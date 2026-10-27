using System.Data;
using System.Text.Json;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services;

namespace HRMS.API.Repositories;

public sealed class EntitlementRepository : BaseRepository, IEntitlementRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public EntitlementRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<EntitlementTenantState?> GetTenantStateAsync(Guid tenantId)
    {
        const string sql = """
            SELECT
                p.planid AS PlanId,
                p.code AS PlanCode,
                CASE
                    WHEN s.enddate IS NOT NULL AND s.enddate < now() THEN 'expired'
                    WHEN s.trial_ends_at IS NOT NULL AND s.trial_ends_at < now() THEN 'expired'
                    ELSE COALESCE(s.status, 'active')
                END AS SubscriptionStatus,
                s.enddate AS EndDate,
                s.trial_ends_at AS TrialEndsAt,
                s.startdate AS StartDate,
                COALESCE(s.billingcycle, 'monthly') AS BillingCycle,
                COALESCE(s.custom_max_users, p.maxusers) AS MaxUsers,
                COALESCE(s.custom_price, p.monthlyprice) AS PriceMonthly,
                COALESCE(s.custom_currency, p.currency) AS Currency,
                p.user_count_type AS UserCountType
            FROM payment.subscriptionplans p
            LEFT JOIN LATERAL (
                SELECT sub.planid, sub.status, sub.enddate, sub.trial_ends_at,
                       sub.startdate, sub.billingcycle,
                       sub.custom_max_users, sub.custom_price, sub.custom_currency
                FROM payment.companysubscriptions sub
                WHERE sub.companyid = @tenantId
                ORDER BY
                    CASE lower(COALESCE(sub.status, '')) WHEN 'active' THEN 0 WHEN 'trial' THEN 1 ELSE 2 END,
                    sub.enddate DESC NULLS FIRST
                LIMIT 1
            ) s ON true
            WHERE p.planid = COALESCE(
                s.planid,
                (SELECT starter.planid FROM payment.subscriptionplans starter WHERE starter.code = 'starter' LIMIT 1)
            )
            LIMIT 1;
            """;

        return await QuerySingleOrDefaultAsync<EntitlementTenantState>(sql, new { tenantId });
    }

    public async Task<IReadOnlyList<Guid>> GetTenantsByPlanAsync(Guid planId)
    {
        const string sql = """
            SELECT DISTINCT companyid
            FROM payment.companysubscriptions
            WHERE planid = @planId AND companyid IS NOT NULL;
            """;
        var tenantIds = await QueryAsync<Guid>(sql, new { planId });
        return tenantIds.ToArray();
    }

    public async Task<IReadOnlyList<FeatureEntitlementDto>> GetEffectiveFeaturesAsync(Guid tenantId, Guid? planId)
    {
        const string planSql = """
            SELECT
                f.key AS Key,
                COALESCE(pf.enabled, false) AS Enabled,
                pf.limit_value AS Limit
            FROM entitlement.features f
            LEFT JOIN entitlement.plan_features pf
                ON pf.feature_id = f.feature_id AND pf.plan_id = @planId
            WHERE f.is_active = true
            ORDER BY f.module_key, f.key;
            """;
        const string overrideSql = """
            SELECT f.key AS Key, o.enabled AS Enabled, o.limit_value AS Limit
            FROM entitlement.tenant_feature_overrides o
            JOIN entitlement.features f ON f.feature_id = o.feature_id
            WHERE o.tenant_id = @tenantId AND f.is_active = true;
            """;

        using var connection = CreateConnection();
        var planFeatures = await connection.QueryAsync<FeatureEntitlementDto>(planSql, new { planId });
        var overrides = await connection.QueryAsync<FeatureEntitlementDto>(overrideSql, new { tenantId });
        return TenantFeatureResolver.Resolve(planFeatures, overrides).Values.ToArray();
    }

    public Task<int> GetUserCountAsync(Guid tenantId, string userCountType)
    {
        const string sql = """
            SELECT count(*)::integer
            FROM employee.employees
            WHERE organizationid = @tenantId
              AND (@userCountType <> 'active_only' OR lower(COALESCE(status, '')) = 'active');
            """;

        return ExecuteScalarAsync<int>(sql, new { tenantId, userCountType });
    }

    public Task<int> GetAiQueryCountAsync(Guid tenantId)
    {
        const string sql = """
            SELECT COALESCE((
                SELECT query_count
                FROM entitlement.ai_query_usage
                WHERE tenant_id = @tenantId
                  AND usage_month = date_trunc('month', now())::date
            ), 0)::integer;
            """;
        return ExecuteScalarAsync<int>(sql, new { tenantId });
    }

    public async Task<bool> TryConsumeAiQueryAsync(Guid tenantId, int limit)
    {
        const string sql = """
            INSERT INTO entitlement.ai_query_usage (tenant_id, usage_month, query_count)
            SELECT @tenantId, date_trunc('month', now())::date, 1
            WHERE @limit > 0
            ON CONFLICT (tenant_id, usage_month) DO UPDATE
            SET query_count = entitlement.ai_query_usage.query_count + 1
            WHERE entitlement.ai_query_usage.query_count < @limit
            RETURNING query_count;
            """;

        using var connection = CreateConnection();
        var consumed = await connection.QuerySingleOrDefaultAsync<int?>(
            sql,
            new { tenantId, limit });
        return consumed.HasValue;
    }

    public async Task<IReadOnlyList<EntitlementFeatureDto>> GetFeatureCatalogAsync()
    {
        const string sql = """
            SELECT
                feature_id AS FeatureId,
                key AS Key,
                name AS Name,
                module_key AS ModuleKey,
                description AS Description,
                type AS Type,
                is_active AS IsActive
            FROM entitlement.features
            WHERE is_active = true
            ORDER BY module_key, name;
            """;

        var features = await QueryAsync<EntitlementFeatureDto>(sql);
        return features.ToArray();
    }

    public async Task<IReadOnlyList<EntitlementPlanDto>> GetPlansAsync(bool includeArchived)
    {
        const string sql = """
            SELECT
                planid AS PlanId,
                name AS Name,
                code AS Code,
                monthlyprice AS PriceMonthly,
                annualprice AS PriceAnnual,
                currency AS Currency,
                maxusers AS MaxUsers,
                user_count_type AS UserCountType,
                iscustom AS IsCustom,
                is_public AS IsPublic,
                is_recommended AS IsRecommended,
                status AS Status,
                version AS Version
            FROM payment.subscriptionplans
            WHERE @includeArchived OR status = 'active'
            ORDER BY monthlyprice NULLS LAST, name;
            """;

        using var connection = CreateConnection();
        var plans = (await connection.QueryAsync<EntitlementPlanDto>(sql, new { includeArchived })).ToList();
        foreach (var plan in plans)
        {
            plan.Features = await GetPlanFeaturesAsync(connection, plan.PlanId);
        }

        return plans;
    }

    public async Task<EntitlementPlanDto?> GetPlanAsync(Guid planId)
    {
        const string sql = """
            SELECT
                planid AS PlanId,
                name AS Name,
                code AS Code,
                monthlyprice AS PriceMonthly,
                annualprice AS PriceAnnual,
                currency AS Currency,
                maxusers AS MaxUsers,
                user_count_type AS UserCountType,
                iscustom AS IsCustom,
                is_public AS IsPublic,
                is_recommended AS IsRecommended,
                status AS Status,
                version AS Version
            FROM payment.subscriptionplans
            WHERE planid = @planId;
            """;

        using var connection = CreateConnection();
        var plan = await connection.QuerySingleOrDefaultAsync<EntitlementPlanDto>(sql, new { planId });
        if (plan != null)
        {
            plan.Features = await GetPlanFeaturesAsync(connection, planId);
        }

        return plan;
    }

    public async Task<Guid> CreatePlanAsync(CreatePlanEntitlementsRequest request, Guid? actor)
    {
        const string sql = """
            INSERT INTO payment.subscriptionplans
                (name, code, monthlyprice, annualprice, maxusers, features, iscustom, isactive,
                 currency, user_count_type, is_public, is_recommended, status)
            VALUES
                (@Name, @Code, @PriceMonthly, @PriceAnnual, @MaxUsers, '{}'::jsonb, @IsCustom, true,
                 @Currency, @UserCountType, @IsPublic, @IsRecommended, 'active')
            RETURNING planid;
            """;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var planId = await connection.ExecuteScalarAsync<Guid>(sql, new
            {
                request.Name,
                request.Code,
                request.PriceMonthly,
                request.PriceAnnual,
                request.MaxUsers,
                request.IsCustom,
                request.Currency,
                request.UserCountType,
                request.IsPublic,
                request.IsRecommended
            }, transaction);
            await SavePlanFeaturesAsync(connection, transaction, planId, request.Features);
            await WriteAuditAsync(connection, transaction, null, actor, "plan.created", "plan", planId, null, request);
            transaction.Commit();
            return planId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdatePlanAsync(Guid planId, UpdatePlanEntitlementsRequest request, Guid? actor)
    {
        var before = await GetPlanAsync(planId);
        if (before == null)
        {
            return false;
        }

        const string sql = """
            UPDATE payment.subscriptionplans
            SET name = @Name,
                monthlyprice = @PriceMonthly,
                annualprice = @PriceAnnual,
                currency = @Currency,
                maxusers = @MaxUsers,
                user_count_type = @UserCountType,
                is_public = @IsPublic,
                is_recommended = @IsRecommended,
                version = version + 1
            WHERE planid = @PlanId AND version = @Version AND status = 'active';
            """;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var updated = await connection.ExecuteAsync(sql, new
            {
                PlanId = planId,
                request.Name,
                request.PriceMonthly,
                request.PriceAnnual,
                request.Currency,
                request.MaxUsers,
                request.UserCountType,
                request.IsPublic,
                request.IsRecommended,
                request.Version
            }, transaction);
            if (updated != 1)
            {
                transaction.Rollback();
                return false;
            }

            await SavePlanFeaturesAsync(connection, transaction, planId, request.Features);
            await WriteAuditAsync(connection, transaction, null, actor, "plan.updated", "plan", planId, before, request);
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DuplicatePlanAsync(Guid planId, string name, string code, Guid? actor)
    {
        var source = await GetPlanAsync(planId);
        if (source == null)
        {
            return false;
        }

        var request = new CreatePlanEntitlementsRequest
        {
            Name = name,
            Code = code,
            PriceMonthly = source.PriceMonthly,
            PriceAnnual = source.PriceAnnual,
            Currency = source.Currency,
            MaxUsers = source.MaxUsers,
            UserCountType = source.UserCountType,
            IsCustom = source.IsCustom,
            IsPublic = source.IsPublic,
            IsRecommended = false,
            Features = source.Features
        };
        await CreatePlanAsync(request, actor);
        return true;
    }

    public async Task<bool> ArchivePlanAsync(Guid planId, Guid? actor)
    {
        var before = await GetPlanAsync(planId);
        if (before == null)
        {
            return false;
        }

        const string sql = """
            UPDATE payment.subscriptionplans
            SET status = 'archived', isactive = false, version = version + 1
            WHERE planid = @planId AND status = 'active';
            """;
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var updated = await connection.ExecuteAsync(sql, new { planId }, transaction);
            if (updated == 1)
            {
                await WriteAuditAsync(connection, transaction, null, actor, "plan.archived", "plan", planId, before, new { status = "archived" });
            }

            transaction.Commit();
            return updated == 1;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ReactivatePlanAsync(Guid planId, Guid? actor)
    {
        var before = await GetPlanAsync(planId);
        if (before == null || !string.Equals(before.Status, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        const string sql = """
            UPDATE payment.subscriptionplans p
            SET status = 'active',
                isactive = true,
                is_public = COALESCE(
                    (
                        SELECT (a.before_state::jsonb ->> 'isPublic')::boolean
                        FROM entitlement.audit_logs a
                        WHERE a.entity = 'plan'
                          AND a.entity_id = p.planid
                          AND a.action = 'plan.archived'
                          AND a.before_state IS NOT NULL
                        ORDER BY a.created_at DESC
                        LIMIT 1
                    ),
                    NOT p.iscustom
                ),
                version = version + 1
            WHERE p.planid = @planId AND p.status = 'archived'
            RETURNING is_public;
            """;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var restoredIsPublic = await connection.QuerySingleOrDefaultAsync<bool?>(
                sql,
                new { planId },
                transaction);
            if (restoredIsPublic.HasValue)
            {
                await WriteAuditAsync(
                    connection,
                    transaction,
                    null,
                    actor,
                    "plan.reactivated",
                    "plan",
                    planId,
                    before,
                    new { status = "active", isPublic = restoredIsPublic.Value });
            }

            transaction.Commit();
            return restoredIsPublic.HasValue;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> SaveTenantOverridesAsync(Guid tenantId, TenantOverridesRequest request, Guid? actor)
    {
        var before = await GetTenantOverridesAsync(tenantId);
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM entitlement.tenant_feature_overrides WHERE tenant_id = @tenantId;",
                new { tenantId },
                transaction);

            const string insertSql = """
                INSERT INTO entitlement.tenant_feature_overrides
                    (tenant_id, feature_id, enabled, limit_value, reason, created_by)
                SELECT @tenantId, feature_id, @Enabled, @Limit, @Reason, @Actor
                FROM entitlement.features
                WHERE key = @FeatureKey AND is_active = true;
                """;

            foreach (var feature in request.Features)
            {
                var inserted = await connection.ExecuteAsync(insertSql, new
                {
                    tenantId,
                    feature.Enabled,
                    Limit = feature.Limit,
                    Reason = string.IsNullOrWhiteSpace(feature.Reason) ? request.Reason : feature.Reason,
                    Actor = actor,
                    feature.FeatureKey
                }, transaction);

                if (inserted != 1)
                {
                    throw new ArgumentException($"Unknown or inactive feature key: {feature.FeatureKey}");
                }
            }

            await WriteAuditAsync(
                connection,
                transaction,
                tenantId,
                actor,
                "tenant.overrides.updated",
                "tenant_feature_overrides",
                tenantId,
                before,
                request);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> AssignTenantPlanAsync(Guid tenantId, TenantPlanAssignmentRequest request, Guid? actor)
    {
        const string planSql = """
            SELECT planid AS PlanId, code AS Code, status AS Status
            FROM payment.subscriptionplans
            WHERE planid = @PlanId;
            """;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var plan = await connection.QuerySingleOrDefaultAsync<dynamic>(
                planSql,
                new { request.PlanId },
                transaction);
            if (plan == null || !string.Equals(
                    ((string?)plan.Status)?.Trim(),
                    "active",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The selected plan does not exist or is archived.");
            }

            const string currentSql = """
                SELECT subscriptionid AS SubscriptionId, planid AS PlanId, status AS Status,
                       billingcycle AS BillingCycle, startdate AS StartDate, enddate AS EndDate,
                       trial_ends_at AS TrialEndsAt
                FROM payment.companysubscriptions
                WHERE companyid = @tenantId
                ORDER BY
                    CASE lower(COALESCE(status, '')) WHEN 'active' THEN 0 WHEN 'trial' THEN 1 ELSE 2 END,
                    enddate DESC NULLS FIRST
                LIMIT 1
                FOR UPDATE;
                """;

            var before = await connection.QuerySingleOrDefaultAsync<dynamic>(
                currentSql,
                new { tenantId },
                transaction);
            var startDate = request.StartDate ?? DateTime.UtcNow;
            var endDate = request.EndDate;
            if (endDate == null && !string.Equals(request.Status, "trial", StringComparison.OrdinalIgnoreCase))
            {
                endDate = string.Equals(request.BillingCycle, "annual", StringComparison.OrdinalIgnoreCase)
                    ? startDate.AddYears(1)
                    : startDate.AddMonths(1);
            }

            int affected;
            Guid subscriptionId;
            if (before == null)
            {
                const string insertSubscriptionSql = """
                    INSERT INTO payment.companysubscriptions
                        (companyid, planid, status, billingcycle, startdate, enddate, trial_ends_at,
                         custom_price, custom_max_users, custom_currency)
                    VALUES (@tenantId, @PlanId, @Status, @BillingCycle, @StartDate, @EndDate, @TrialEndsAt,
                         @CustomPrice, @CustomMaxUsers, @CustomCurrency)
                    RETURNING subscriptionid;
                    """;
                subscriptionId = await connection.ExecuteScalarAsync<Guid>(insertSubscriptionSql, new
                {
                    tenantId,
                    request.PlanId,
                    request.Status,
                    request.BillingCycle,
                    StartDate = startDate,
                    EndDate = endDate,
                    request.TrialEndsAt,
                    request.CustomPrice,
                    request.CustomMaxUsers,
                    request.CustomCurrency
                }, transaction);
                affected = 1;
            }
            else
            {
                subscriptionId = (Guid)before.SubscriptionId;
                const string updateSubscriptionSql = """
                    UPDATE payment.companysubscriptions
                    SET planid = @PlanId, status = @Status, billingcycle = @BillingCycle,
                        startdate = @StartDate, enddate = @EndDate, trial_ends_at = @TrialEndsAt,
                        custom_price = @CustomPrice, custom_max_users = @CustomMaxUsers,
                        custom_currency = @CustomCurrency
                    WHERE subscriptionid = @SubscriptionId;
                    """;
                affected = await connection.ExecuteAsync(updateSubscriptionSql, new
                {
                    SubscriptionId = subscriptionId,
                    request.PlanId,
                    request.Status,
                    request.BillingCycle,
                    StartDate = startDate,
                    EndDate = endDate,
                    request.TrialEndsAt,
                    request.CustomPrice,
                    request.CustomMaxUsers,
                    request.CustomCurrency
                }, transaction);
            }

            if (affected == 1)
            {
                await WriteAuditAsync(
                    connection,
                    transaction,
                    tenantId,
                    actor,
                    "tenant.subscription.assigned",
                    "companysubscription",
                    subscriptionId,
                    before,
                    request);
            }

            transaction.Commit();
            return affected == 1;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> SaveTenantCustomPlanAsync(
        Guid tenantId,
        TenantCustomPlanRequest request,
        Guid? actor)
    {
        var beforeOverrides = await GetTenantOverridesAsync(tenantId);
        const string planSql = """
            SELECT code AS Code, status AS Status
            FROM payment.subscriptionplans
            WHERE planid = @PlanId;
            """;
        const string currentSql = """
            SELECT subscriptionid AS SubscriptionId, planid AS PlanId, status AS Status,
                   billingcycle AS BillingCycle, startdate AS StartDate, enddate AS EndDate,
                   trial_ends_at AS TrialEndsAt, custom_price AS CustomPrice,
                   custom_max_users AS CustomMaxUsers, custom_currency AS CustomCurrency
            FROM payment.companysubscriptions
            WHERE companyid = @tenantId
            ORDER BY
                CASE lower(COALESCE(status, '')) WHEN 'active' THEN 0 WHEN 'trial' THEN 1 ELSE 2 END,
                enddate DESC NULLS FIRST
            LIMIT 1
            FOR UPDATE;
            """;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var plan = await connection.QuerySingleOrDefaultAsync<dynamic>(
                planSql,
                new { request.PlanId },
                transaction);
            if (plan == null || !string.Equals(
                    ((string?)plan.Status)?.Trim(),
                    "active",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The selected plan does not exist or is archived.");
            }

            var beforeSubscription = await connection.QuerySingleOrDefaultAsync<dynamic>(
                currentSql,
                new { tenantId },
                transaction);

            await connection.ExecuteAsync(
                "DELETE FROM entitlement.tenant_feature_overrides WHERE tenant_id = @tenantId;",
                new { tenantId },
                transaction);

            const string insertOverrideSql = """
                INSERT INTO entitlement.tenant_feature_overrides
                    (tenant_id, feature_id, enabled, limit_value, reason, created_by)
                SELECT @tenantId, feature_id, @Enabled, @Limit, @Reason, @Actor
                FROM entitlement.features
                WHERE key = @FeatureKey AND is_active = true;
                """;
            foreach (var feature in request.Features)
            {
                var inserted = await connection.ExecuteAsync(insertOverrideSql, new
                {
                    tenantId,
                    feature.Enabled,
                    feature.Limit,
                    Reason = string.IsNullOrWhiteSpace(feature.Reason) ? request.Reason : feature.Reason,
                    Actor = actor,
                    feature.FeatureKey
                }, transaction);
                if (inserted != 1)
                {
                    throw new ArgumentException($"Unknown or inactive feature key: {feature.FeatureKey}");
                }
            }

            var existingStartDate = beforeSubscription == null
                ? null
                : (DateTime?)beforeSubscription.StartDate;
            var existingEndDate = beforeSubscription == null
                ? null
                : (DateTime?)beforeSubscription.EndDate;
            var startDate = request.StartDate ?? existingStartDate ?? DateTime.UtcNow;
            var endDate = request.EndDate ?? existingEndDate;
            if (beforeSubscription == null &&
                endDate == null &&
                !string.Equals(request.Status, "trial", StringComparison.OrdinalIgnoreCase))
            {
                endDate = string.Equals(request.BillingCycle, "annual", StringComparison.OrdinalIgnoreCase)
                    ? startDate.AddYears(1)
                    : startDate.AddMonths(1);
            }

            Guid subscriptionId;
            if (beforeSubscription == null)
            {
                const string insertSql = """
                    INSERT INTO payment.companysubscriptions
                        (companyid, planid, status, billingcycle, startdate, enddate, trial_ends_at,
                         custom_price, custom_max_users, custom_currency)
                    VALUES (@tenantId, @PlanId, @Status, @BillingCycle, @StartDate, @EndDate, @TrialEndsAt,
                         @CustomPrice, @CustomMaxUsers, @CustomCurrency)
                    RETURNING subscriptionid;
                    """;
                subscriptionId = await connection.ExecuteScalarAsync<Guid>(insertSql, new
                {
                    tenantId,
                    request.PlanId,
                    request.Status,
                    request.BillingCycle,
                    StartDate = startDate,
                    EndDate = endDate,
                    request.TrialEndsAt,
                    request.CustomPrice,
                    request.CustomMaxUsers,
                    request.CustomCurrency
                }, transaction);
            }
            else
            {
                subscriptionId = (Guid)beforeSubscription.SubscriptionId;
                const string updateSql = """
                    UPDATE payment.companysubscriptions
                    SET planid = @PlanId, status = @Status, billingcycle = @BillingCycle,
                        startdate = @StartDate, enddate = @EndDate, trial_ends_at = @TrialEndsAt,
                        custom_price = @CustomPrice, custom_max_users = @CustomMaxUsers,
                        custom_currency = @CustomCurrency
                    WHERE subscriptionid = @SubscriptionId;
                    """;
                await connection.ExecuteAsync(updateSql, new
                {
                    SubscriptionId = subscriptionId,
                    request.PlanId,
                    request.Status,
                    request.BillingCycle,
                    StartDate = startDate,
                    EndDate = endDate,
                    request.TrialEndsAt,
                    request.CustomPrice,
                    request.CustomMaxUsers,
                    request.CustomCurrency
                }, transaction);
            }

            await WriteAuditAsync(
                connection,
                transaction,
                tenantId,
                actor,
                "tenant.overrides.updated",
                "tenant_feature_overrides",
                tenantId,
                beforeOverrides,
                request.Features);
            await WriteAuditAsync(
                connection,
                transaction,
                tenantId,
                actor,
                "tenant.subscription.customized",
                "companysubscription",
                subscriptionId,
                beforeSubscription,
                request);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IReadOnlyList<EntitlementAuditEntryDto>> GetAuditLogsAsync(
        Guid? tenantId,
        DateTime? from,
        DateTime? to,
        string? action)
    {
        const string sql = """
            SELECT audit_id AS AuditId, tenant_id AS TenantId, actor AS Actor,
                   action AS Action, entity AS Entity, entity_id AS EntityId,
                   before_state::text AS BeforeState, after_state::text AS AfterState,
                   created_at AS CreatedAt
            FROM entitlement.audit_logs
            WHERE (@tenantId IS NULL OR tenant_id = @tenantId)
              AND (@from IS NULL OR created_at >= @from)
              AND (@to IS NULL OR created_at < @to)
              AND (@action IS NULL OR action = @action)
            ORDER BY created_at DESC
            LIMIT 500;
            """;
        var rows = await QueryAsync<EntitlementAuditEntryDto>(sql, new { tenantId, from, to, action });
        return rows.ToArray();
    }

    private static async Task<Dictionary<string, FeatureEntitlementDto>> GetPlanFeaturesAsync(
        IDbConnection connection,
        Guid planId)
    {
        const string sql = """
            SELECT f.key AS Key, pf.enabled AS Enabled, pf.limit_value AS Limit
            FROM entitlement.plan_features pf
            JOIN entitlement.features f ON f.feature_id = pf.feature_id
            WHERE pf.plan_id = @planId AND f.is_active = true;
            """;
        var features = await connection.QueryAsync<FeatureEntitlementDto>(sql, new { planId });
        return features.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SavePlanFeaturesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid planId,
        IDictionary<string, FeatureEntitlementDto> features)
    {
        await connection.ExecuteAsync(
            "DELETE FROM entitlement.plan_features WHERE plan_id = @planId;",
            new { planId },
            transaction);

        const string sql = """
            INSERT INTO entitlement.plan_features (plan_id, feature_id, enabled, limit_value)
            SELECT @planId, feature_id, @Enabled, @Limit
            FROM entitlement.features
            WHERE key = @Key AND is_active = true;
            """;

        foreach (var (key, feature) in features)
        {
            var inserted = await connection.ExecuteAsync(sql, new
            {
                planId,
                feature.Enabled,
                feature.Limit,
                Key = key
            }, transaction);
            if (inserted != 1)
            {
                throw new ArgumentException($"Unknown or inactive feature key: {key}");
            }
        }
    }

    private async Task<object> GetTenantOverridesAsync(Guid tenantId)
    {
        const string sql = """
            SELECT f.key AS FeatureKey, o.enabled AS Enabled, o.limit_value AS Limit,
                   o.reason AS Reason, o.created_by AS CreatedBy
            FROM entitlement.tenant_feature_overrides o
            JOIN entitlement.features f ON f.feature_id = o.feature_id
            WHERE o.tenant_id = @tenantId
            ORDER BY f.key;
            """;
        return await QueryAsync<dynamic>(sql, new { tenantId });
    }

    private static Task WriteAuditAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid? tenantId,
        Guid? actor,
        string action,
        string entity,
        Guid? entityId,
        object? before,
        object? after)
    {
        const string sql = """
            INSERT INTO entitlement.audit_logs
                (tenant_id, actor, action, entity, entity_id, before_state, after_state)
            VALUES
                (@tenantId, @actor, @action, @entity, @entityId,
                 CAST(@beforeState AS jsonb), CAST(@afterState AS jsonb));
            """;
        return connection.ExecuteAsync(sql, new
        {
            tenantId,
            actor,
            action,
            entity,
            entityId,
            beforeState = before == null ? null : JsonSerializer.Serialize(before, JsonOptions),
            afterState = after == null ? null : JsonSerializer.Serialize(after, JsonOptions)
        }, transaction);
    }
}
