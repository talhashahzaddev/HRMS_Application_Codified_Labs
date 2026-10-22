using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class SuperAdminRepository : BaseRepository, ISuperAdminRepository
{
    private readonly ILogger<SuperAdminRepository> _logger;

    public SuperAdminRepository(IDatabaseConnectionFactory connectionFactory, ILogger<SuperAdminRepository> logger)
        : base(connectionFactory)
    {
        _logger = logger;
    }

    public async Task<PlatformDashboardDto> GetPlatformDashboardAsync()
    {
        try
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT 
                    -- Total organizations
                    (SELECT COUNT(*) FROM auth.organizations) AS TotalOrganizations,
                    
                    -- Active subscriptions (enddate in future)
                    (SELECT COUNT(*) FROM payment.companysubscriptions 
                     WHERE status = 'active' AND enddate > NOW()) AS ActiveSubscriptions,
                    
                    -- Expired subscriptions
                    (SELECT COUNT(*) FROM payment.companysubscriptions 
                     WHERE enddate <= NOW() OR status = 'expired') AS ExpiredSubscriptions,
                    
                    -- Trial accounts (organizations with subscription_type = 'trial' or no subscription)
                    (SELECT COUNT(*) FROM auth.organizations o 
                     WHERE o.subscriptiontype = 'trial' 
                        OR NOT EXISTS (SELECT 1 FROM payment.companysubscriptions cs WHERE cs.companyid = o.organizationid)) AS TrialAccounts,
                    
                    -- Total active employees across all orgs
                    (SELECT COUNT(*) FROM employee.employees WHERE status = 'active') AS TotalActiveEmployees,
                    
                    -- MRR: sum of monthly prices for active subscriptions
                    (SELECT COALESCE(SUM(
                        CASE 
                            WHEN cs.billingcycle = 'monthly' THEN sp.monthlyprice
                            WHEN cs.billingcycle = 'annual' THEN sp.annualprice / 12
                            ELSE COALESCE(sp.monthlyprice, 0)
                        END
                    ), 0)
                     FROM payment.companysubscriptions cs
                     JOIN payment.subscriptionplans sp ON cs.planid = sp.planid
                     WHERE cs.status = 'active' AND cs.enddate > NOW()) AS MonthlyRecurringRevenue,
                    
                    -- New orgs this month
                    (SELECT COUNT(*) FROM auth.organizations 
                     WHERE createdat >= DATE_TRUNC('month', NOW())) AS NewOrganizationsThisMonth,
                    
                    -- Total inquiries
                    (SELECT COUNT(*) FROM admin.demo_inquiries) AS TotalDemoInquiries,
                    
                    -- Pending inquiries
                    (SELECT COUNT(*) FROM admin.demo_inquiries WHERE status = 'new') AS PendingInquiries
            ";

            var result = await connection.QuerySingleAsync<PlatformDashboardDto>(sql);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform dashboard stats");
            throw;
        }
    }

    public async Task<PagedResult<OrganizationListItemDto>> GetOrganizationsPagedAsync(OrganizationFilterRequest filter)
    {
        try
        {
            using var connection = CreateConnection();
            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                whereClause += @" AND (
                    LOWER(o.name) LIKE @search
                    OR LOWER(COALESCE(o.admin_email, '')) LIKE @search
                    OR LOWER(COALESCE(o.industry, '')) LIKE @search
                )";
                parameters.Add("search", $"%{filter.Search.ToLower()}%");
            }

            if (!string.IsNullOrWhiteSpace(filter.SubscriptionStatus))
            {
                switch (filter.SubscriptionStatus.ToLower())
                {
                    case "active":
                        whereClause += " AND cs.status = 'active' AND cs.enddate > NOW()";
                        break;
                    case "expired":
                        whereClause += " AND (cs.enddate <= NOW() OR cs.status = 'expired')";
                        break;
                    case "trial":
                        whereClause += " AND (o.subscriptiontype = 'trial' OR cs.subscriptionid IS NULL)";
                        break;
                    case "cancelled":
                        whereClause += " AND cs.status = 'cancelled'";
                        break;
                    case "pending":
                        whereClause += " AND cs.status = 'pending'";
                        break;
                }
            }

            if (filter.PlanId.HasValue)
            {
                whereClause += " AND cs.planid = @planId";
                parameters.Add("planId", filter.PlanId.Value);
            }

            // Sort column whitelist
            var allowedSortColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "createdat", "o.createdat" },
                { "created_at", "o.createdat" },
                { "name", "o.name" },
                { "expirydate", "cs.enddate" },
                { "subscriptionstatus", "cs.status" },
                { "subscription_status", "cs.status" },
                { "employeecount", "active_emp_count" },
                { "totalactiveemployees", "active_emp_count" },
                { "total_active_employees", "active_emp_count" }
            };

            var sortColumn = allowedSortColumns.ContainsKey(filter.SortBy ?? "createdat")
                ? allowedSortColumns[filter.SortBy ?? "createdat"]
                : "o.createdat";
            var sortDir = filter.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

            // Count query
            var countSql = $@"
                SELECT COUNT(DISTINCT o.organizationid)
                FROM auth.organizations o
                LEFT JOIN LATERAL (
                    SELECT * FROM payment.companysubscriptions 
                    WHERE companyid = o.organizationid 
                    ORDER BY enddate DESC LIMIT 1
                ) cs ON true
                {whereClause}";

            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Data query
            var offset = (filter.Page - 1) * filter.PageSize;
            parameters.Add("limit", filter.PageSize);
            parameters.Add("offset", offset);

            var dataSql = $@"
                SELECT 
                    o.organizationid AS OrganizationId,
                    o.name AS Name,
                    o.industry AS Industry,
                    o.country AS Country,
                    sp.name AS SubscriptionPlanName,
                    CASE 
                        WHEN cs.subscriptionid IS NULL THEN 'Trial'
                        WHEN cs.enddate <= NOW() OR cs.status = 'expired' THEN 'Expired'
                        WHEN cs.status = 'active' AND cs.enddate > NOW() THEN 'Active'
                        ELSE COALESCE(cs.status, 'Trial')
                    END AS SubscriptionStatus,
                    cs.enddate AS SubscriptionExpiryDate,
                    COALESCE(emp.active_emp_count, 0) AS TotalActiveEmployees,
                    o.createdat AS CreatedAt
                FROM auth.organizations o
                LEFT JOIN LATERAL (
                    SELECT * FROM payment.companysubscriptions 
                    WHERE companyid = o.organizationid 
                    ORDER BY enddate DESC LIMIT 1
                ) cs ON true
                LEFT JOIN payment.subscriptionplans sp ON cs.planid = sp.planid
                LEFT JOIN LATERAL (
                    SELECT COUNT(*) AS active_emp_count
                    FROM employee.employees 
                    WHERE organizationid = o.organizationid AND status = 'active'
                ) emp ON true
                {whereClause}
                ORDER BY {sortColumn} {sortDir} NULLS LAST
                LIMIT @limit OFFSET @offset";

            var data = await connection.QueryAsync<OrganizationListItemDto>(dataSql, parameters);

            return new PagedResult<OrganizationListItemDto>
            {
                Data = data.ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations paged list");
            throw;
        }
    }

    public async Task<OrganizationDetailDto?> GetOrganizationDetailAsync(Guid organizationId)
    {
        try
        {
            using var connection = CreateConnection();

            // Organization info
            var orgSql = @"
                SELECT 
                    o.organizationid AS OrganizationId,
                    o.name AS Name,
                    o.domain AS Domain,
                    o.industry AS Industry,
                    o.country AS Country,
                    o.admin_email AS AdminEmail,
                    o.company_size AS CompanySize,
                    o.currency AS Currency,
                    o.status AS Status,
                    o.createdat AS CreatedAt
                FROM auth.organizations o
                WHERE o.organizationid = @organizationId";

            var org = await connection.QuerySingleOrDefaultAsync<OrganizationDetailDto>(orgSql, new { organizationId });
            if (org == null) return null;

            // Subscription details
            var subSql = @"
                SELECT 
                    cs.subscriptionid AS SubscriptionId,
                    cs.planid AS PlanId,
                    sp.name AS PlanName,
                    cs.billingcycle AS BillingCycle,
                    cs.status AS Status,
                    CASE WHEN cs.billingcycle = 'annual' THEN COALESCE(sp.annualprice, 0)
                         ELSE COALESCE(sp.monthlyprice, 0) END AS Amount,
                    cs.startdate AS StartDate,
                    cs.enddate AS EndDate,
                    CASE WHEN cs.enddate < NOW() THEN true ELSE false END AS IsExpired
                FROM payment.companysubscriptions cs
                JOIN payment.subscriptionplans sp ON cs.planid = sp.planid
                WHERE cs.companyid = @organizationId
                ORDER BY cs.enddate DESC
                LIMIT 1";

            org.Subscription = await connection.QuerySingleOrDefaultAsync<CompanySubscriptionDetailsDto>(subSql, new { organizationId });

            // Employee metrics
            var empSql = @"
                SELECT 
                    COUNT(*) AS TotalEmployees,
                    COUNT(*) FILTER (WHERE status = 'active') AS ActiveEmployees,
                    COUNT(*) FILTER (WHERE status != 'active') AS InactiveEmployees
                FROM employee.employees 
                WHERE organizationid = @organizationId";

            var empMetrics = await connection.QuerySingleOrDefaultAsync<dynamic>(empSql, new { organizationId });
            if (empMetrics != null)
            {
                org.TotalEmployees = (int)(empMetrics.totalemployees ?? 0);
                org.ActiveEmployees = (int)(empMetrics.activeemployees ?? 0);
                org.InactiveEmployees = (int)(empMetrics.inactiveemployees ?? 0);
            }

            // Subscription history
            org.SubscriptionHistory = await GetSubscriptionHistoryAsync(organizationId);

            return org;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization detail for {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<List<SubscriptionHistoryDto>> GetSubscriptionHistoryAsync(Guid companyId)
    {
        var sql = @"
            SELECT 
                sh.history_id AS HistoryId,
                sh.action AS Action,
                old_plan.name AS OldPlanName,
                new_plan.name AS NewPlanName,
                sh.old_end_date AS OldEndDate,
                sh.new_end_date AS NewEndDate,
                pa.full_name AS PerformedByName,
                sh.notes AS Notes,
                sh.created_at AS CreatedAt
            FROM payment.subscription_history sh
            LEFT JOIN payment.subscriptionplans old_plan ON sh.old_plan_id = old_plan.planid
            LEFT JOIN payment.subscriptionplans new_plan ON sh.new_plan_id = new_plan.planid
            LEFT JOIN admin.platform_admins pa ON sh.performed_by = pa.admin_id
            WHERE sh.company_id = @companyId
            ORDER BY sh.created_at DESC";

        var results = await QueryAsync<SubscriptionHistoryDto>(sql, new { companyId });
        return results.ToList();
    }

    public async Task LogSubscriptionHistoryAsync(
        Guid subscriptionId, Guid companyId, string action,
        Guid? oldPlanId, Guid? newPlanId, DateTime? oldEndDate, DateTime? newEndDate,
        Guid? performedBy, string? notes)
    {
        var sql = @"
            INSERT INTO payment.subscription_history 
                (subscription_id, company_id, action, old_plan_id, new_plan_id, old_end_date, new_end_date, performed_by, notes)
            VALUES 
                (@subscriptionId, @companyId, @action, @oldPlanId, @newPlanId, @oldEndDate, @newEndDate, @performedBy, @notes)";

        await ExecuteAsync(sql, new
        {
            subscriptionId,
            companyId,
            action,
            oldPlanId,
            newPlanId,
            oldEndDate,
            newEndDate,
            performedBy,
            notes
        });
    }
}
