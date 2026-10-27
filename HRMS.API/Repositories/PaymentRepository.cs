using System.Data;
using System.Text.Json;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class PaymentRepository : BaseRepository, IPaymentRepository
{
    public PaymentRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<SubscriptionPlanDto?> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        var sql = @"
            SELECT 
                planid,
                name,
                monthlyprice,
                annualprice,
                maxusers,
                currency,
                features,
                iscustom,
                isactive
            FROM payment.subscriptionplans p
            WHERE planid = @planId";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { planId });
        
        if (result == null)
            return null;

        return MapToSubscriptionPlanDto(result);
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetAllSubscriptionPlansAsync()
    {
        var sql = @"
            SELECT 
                planid,
                name,
                monthlyprice,
                annualprice,
                maxusers,
                currency,
                features,
                iscustom,
                isactive
            FROM payment.subscriptionplans
            ORDER BY name";

        var results = await QueryAsync<dynamic>(sql);
        return results.Select(MapToSubscriptionPlanDto);
    }
    public async Task<IEnumerable<SubscriptionPlanDto>> GetActiveSubscriptionPlansAsync()
    {
        var sql = @"
        SELECT planid, name, monthlyprice, annualprice, maxusers, currency,
               COALESCE((
                   SELECT jsonb_agg(f.name ORDER BY f.module_key, f.name)
                   FROM entitlement.plan_features pf
                   JOIN entitlement.features f ON f.feature_id = pf.feature_id
                   WHERE pf.plan_id = p.planid AND pf.enabled = true AND f.is_active = true
               ), '[]'::jsonb) AS features,
               iscustom, isactive
        FROM payment.subscriptionplans p
        WHERE p.isactive = true AND p.status = 'active' AND p.is_public = true
        ORDER BY p.monthlyprice ASC";
        var results = await QueryAsync<dynamic>(sql);
        return results.Select(MapToSubscriptionPlanDto);
    }
    public async Task<Guid> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request)
    {
        var sql = @"
            INSERT INTO payment.subscriptionplans 
                (name, monthlyprice, annualprice, maxusers, features, iscustom, isactive)
            VALUES 
                (@name, @monthlyPrice, @annualPrice, @maxUsers, @features::jsonb, @isCustom, @isActive)
            RETURNING planid";

        var featuresJson = request.Features != null ? JsonSerializer.Serialize(request.Features) : null;

        var planId = await ExecuteScalarAsync<Guid>(sql, new
        {
            name = request.Name,
            monthlyPrice = request.MonthlyPrice,
            annualPrice = request.AnnualPrice,
            maxUsers = request.MaxUsers,
            features = featuresJson,
            isCustom = request.IsCustom,
            isActive = request.IsActive
        });

        return planId;
    }

    public async Task<bool> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request)
    {
        var sql = @"
            UPDATE payment.subscriptionplans
            SET 
                name = @name,
                monthlyprice = @monthlyPrice,
                annualprice = @annualPrice,
                maxusers = @maxUsers,
                features = @features::jsonb,
                iscustom = @isCustom,
                isactive = @isActive
            WHERE planid = @planId";

        var featuresJson = request.Features != null ? JsonSerializer.Serialize(request.Features) : null;

        var affectedRows = await ExecuteAsync(sql, new
        {
            planId,
            name = request.Name,
            monthlyPrice = request.MonthlyPrice,
            annualPrice = request.AnnualPrice,
            maxUsers = request.MaxUsers,
            features = featuresJson,
            isCustom = request.IsCustom,
            isActive = request.IsActive
        });

        return affectedRows > 0;
    }

    public async Task<bool> InactivateSubscriptionPlanAsync(Guid planId)
    {
        var sql = @"
            UPDATE payment.subscriptionplans
            SET isactive = false
            WHERE planid = @planId";

        var affectedRows = await ExecuteAsync(sql, new { planId });
        return affectedRows > 0;
    }

    public async Task<bool> DeleteSubscriptionPlanAsync(Guid planId)
    {
        var sql = @"
            DELETE FROM payment.subscriptionplans
            WHERE planid = @planId";

        var affectedRows = await ExecuteAsync(sql, new { planId });
        return affectedRows > 0;
    }

    public async Task<CompanySubscriptionDto?> GetCompanySubscriptionByIdAsync(Guid subscriptionId)
    {
        var sql = @"
            SELECT 
                subscriptionid,
                companyid,
                planid,
                status,
                billingcycle,
                startdate,
                enddate
            FROM payment.companysubscriptions
            WHERE subscriptionid = @subscriptionId";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { subscriptionId });
        
        if (result == null)
            return null;

        return MapToCompanySubscriptionDto(result);
    }

    public async Task<IEnumerable<CompanySubscriptionDto>> GetCompanySubscriptionsByCompanyIdAsync(Guid companyId)
    {
        var sql = @"
            SELECT 
                subscriptionid,
                companyid,
                planid,
                status,
                billingcycle,
                startdate,
                enddate
            FROM payment.companysubscriptions
            WHERE companyid = @companyId";

        var results = await QueryAsync<dynamic>(sql, new { companyId });
        return results.Select(MapToCompanySubscriptionDto);
    }

    public async Task<IEnumerable<CompanySubscriptionDto>> GetAllCompanySubscriptionsAsync()
    {
        var sql = @"
            SELECT 
                subscriptionid,
                companyid,
                planid,
                status,
                billingcycle,
                startdate,
                enddate
            FROM payment.companysubscriptions";

        var results = await QueryAsync<dynamic>(sql);
        return results.Select(MapToCompanySubscriptionDto);
    }

    public async Task<CompanySubscriptionDetailsDto?> GetCompanySubscriptionDetailsByCompanyIdAsync(Guid companyId)
    {
        var sql = @"
            SELECT 
                cs.planid,
                sp.name as planname,
                cs.billingcycle,
                cs.startdate,
                cs.enddate,
                CASE 
                    WHEN cs.enddate < CURRENT_TIMESTAMP THEN true 
                    ELSE false 
                END as isexpired
            FROM payment.companysubscriptions cs
            JOIN payment.subscriptionplans sp ON cs.planid = sp.planid
            WHERE cs.companyid = @companyId
            ORDER BY cs.enddate DESC
            LIMIT 1";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { companyId });

        if (result == null)
            return null;

        return new CompanySubscriptionDetailsDto
        {
            PlanId = result.planid,
            PlanName = result.planname,
            BillingCycle = result.billingcycle,
            StartDate = result.startdate,
            EndDate = result.enddate,
            IsExpired = result.isexpired
        };
    }

    public async Task<Guid> CreateCompanySubscriptionAsync(CreateCompanySubscriptionRequest request)
    {
        // First, check if a subscription already exists for this company
        var checkSql = "SELECT subscriptionid FROM payment.companysubscriptions WHERE companyid = @companyId LIMIT 1";
        var existingSubscriptionId = await QuerySingleOrDefaultAsync<Guid?>(checkSql, new { companyId = request.CompanyId });

        if (existingSubscriptionId.HasValue)
        {
            // UPDATE existing subscription
            var updateSql = @"
                UPDATE payment.companysubscriptions 
                SET planid = @planId, 
                    status = @status, 
                    billingcycle = @billingCycle, 
                    startdate = @startDate, 
                    enddate = @endDate
                WHERE subscriptionid = @subscriptionId";

            await ExecuteAsync(updateSql, new
            {
                subscriptionId = existingSubscriptionId.Value,
                planId = request.PlanId,
                status = request.Status,
                billingCycle = request.BillingCycle,
                startDate = request.StartDate,
                endDate = request.EndDate
            });

            return existingSubscriptionId.Value;
        }
        else
        {
            // INSERT new subscription
            var insertSql = @"
                INSERT INTO payment.companysubscriptions 
                    (companyid, planid, status, billingcycle, startdate, enddate)
                VALUES 
                    (@companyId, @planId, @status, @billingCycle, @startDate, @endDate)
                RETURNING subscriptionid";

            var subscriptionId = await ExecuteScalarAsync<Guid>(insertSql, new
            {
                companyId = request.CompanyId,
                planId = request.PlanId,
                status = request.Status,
                billingCycle = request.BillingCycle,
                startDate = request.StartDate,
                endDate = request.EndDate
            });

            return subscriptionId;
        }
    }

    public async Task<bool> UpdateCompanySubscriptionAsync(Guid subscriptionId, UpdateCompanySubscriptionRequest request)
    {
        var sql = @"
            UPDATE payment.companysubscriptions
            SET 
                planid = @planId,
                status = @status,
                billingcycle = @billingCycle,
                startdate = @startDate,
                enddate = @endDate
            WHERE subscriptionid = @subscriptionId";

        var affectedRows = await ExecuteAsync(sql, new
        {
            subscriptionId,
            planId = request.PlanId,
            status = request.Status,
            billingCycle = request.BillingCycle,
            startDate = request.StartDate,
            endDate = request.EndDate
        });

        return affectedRows > 0;
    }

    public async Task<bool> DeleteCompanySubscriptionAsync(Guid subscriptionId)
    {
        var sql = @"
            DELETE FROM payment.companysubscriptions
            WHERE subscriptionid = @subscriptionId";

        var affectedRows = await ExecuteAsync(sql, new { subscriptionId });
        return affectedRows > 0;
    }

    private CompanySubscriptionDto MapToCompanySubscriptionDto(dynamic row)
    {
        return new CompanySubscriptionDto
        {
            SubscriptionId = row.subscriptionid,
            CompanyId = row.companyid,
            PlanId = row.planid,
            Status = row.status,
            BillingCycle = row.billingcycle,
            StartDate = row.startdate,
            EndDate = row.enddate
        };
    }

    private SubscriptionPlanDto MapToSubscriptionPlanDto(dynamic row)
    {
        return new SubscriptionPlanDto
        {
            PlanId = row.planid,
            Name = row.name ?? string.Empty,
            MonthlyPrice = row.monthlyprice,
            AnnualPrice = row.annualprice,
            MaxUsers = row.maxusers,
            Currency = row.currency ?? "BHD",
            Features = ParsePlanFeatures(row.features),
            IsCustom = row.iscustom ?? false,
            IsActive = row.isactive ?? true
        };
    }

    private static Dictionary<string, object>? ParsePlanFeatures(object? value)
    {
        if (value == null || Convert.IsDBNull(value))
        {
            return null;
        }

        using var document = JsonDocument.Parse(value.ToString() ?? "null");
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(root.GetRawText());
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray()
                .Select((feature, index) => new KeyValuePair<string, object>(
                    $"Feature{index + 1}",
                    feature.ValueKind == JsonValueKind.String
                        ? feature.GetString() ?? string.Empty
                        : feature.GetRawText()))
                .ToDictionary(item => item.Key, item => item.Value);
        }

        throw new JsonException("Subscription plan features must be a JSON object or array.");
    }
}
