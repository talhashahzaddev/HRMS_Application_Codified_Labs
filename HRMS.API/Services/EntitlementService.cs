using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HRMS.API.Services;

public sealed class EntitlementService : IEntitlementService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
    private readonly IEntitlementRepository _repository;
    private readonly IMemoryCache _cache;

    public EntitlementService(IEntitlementRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<TenantEntitlementsDto> GetTenantEntitlementsAsync(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A valid tenant ID is required.", nameof(tenantId));
        }

        return await _cache.GetOrCreateAsync(CacheKey(tenantId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var state = await _repository.GetTenantStateAsync(tenantId);
            if (state == null)
            {
                throw new InvalidOperationException($"No subscription plan is available for tenant {tenantId}.");
            }

            var status = EntitlementRules.NormalizeSubscriptionStatus(
                state.SubscriptionStatus,
                state.EndDate,
                state.TrialEndsAt,
                DateTime.UtcNow);
            var features = await _repository.GetEffectiveFeaturesAsync(tenantId, state.PlanId);
            var userCount = await _repository.GetUserCountAsync(tenantId, state.UserCountType);
            var aiQueriesUsed = await _repository.GetAiQueryCountAsync(tenantId);
            var aiLimit = features.FirstOrDefault(feature =>
                string.Equals(feature.Key, "ai_assistant", StringComparison.OrdinalIgnoreCase))?.Limit;
            return new TenantEntitlementsDto
            {
                PlanCode = state.PlanCode,
                PlanId = state.PlanId,
                SubscriptionStatus = status,
                MaxUsers = state.MaxUsers,
                PriceMonthly = state.PriceMonthly,
                Currency = state.Currency,
                StartDate = state.StartDate,
                EndDate = state.EndDate,
                TrialEndsAt = state.TrialEndsAt,
                BillingCycle = state.BillingCycle,
                UserCountType = state.UserCountType,
                UserCount = userCount,
                IsReadOnly = status is "expired" or "cancelled",
                AiQueriesUsedThisMonth = aiQueriesUsed,
                AiQueriesRemaining = aiLimit.HasValue ? Math.Max(0, aiLimit.Value - aiQueriesUsed) : null,
                Features = features.ToDictionary(
                    feature => feature.Key,
                    feature => feature,
                    StringComparer.OrdinalIgnoreCase)
            };
        }) ?? throw new InvalidOperationException("Failed to load tenant entitlements.");
    }

    public async Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureKey)
    {
        var entitlements = await GetTenantEntitlementsAsync(tenantId);
        return entitlements.Features.TryGetValue(featureKey, out var feature) && feature.Enabled;
    }

    public async Task<bool> TryConsumeAiQueryAsync(Guid tenantId, int? limit)
    {
        if (!limit.HasValue)
        {
            return true;
        }

        return await _repository.TryConsumeAiQueryAsync(tenantId, limit.Value);
    }

    public Task<int> GetUserCountAsync(Guid tenantId, string userCountType) =>
        _repository.GetUserCountAsync(tenantId, userCountType);

    public void InvalidateTenantCache(Guid tenantId) =>
        _cache.Remove(CacheKey(tenantId));

    public Task<IReadOnlyList<EntitlementFeatureDto>> GetFeatureCatalogAsync() =>
        _repository.GetFeatureCatalogAsync();

    public Task<IReadOnlyList<EntitlementPlanDto>> GetPlansAsync(bool includeArchived) =>
        _repository.GetPlansAsync(includeArchived);

    public Task<EntitlementPlanDto?> GetPlanAsync(Guid planId) =>
        _repository.GetPlanAsync(planId);

    public async Task<Guid> CreatePlanAsync(CreatePlanEntitlementsRequest request, Guid? actor)
    {
        await ValidatePlanAsync(request, request.Code);
        return await _repository.CreatePlanAsync(request, actor);
    }

    public async Task<bool> UpdatePlanAsync(Guid planId, UpdatePlanEntitlementsRequest request, Guid? actor)
    {
        await ValidatePlanAsync(request, null);
        var updated = await _repository.UpdatePlanAsync(planId, request, actor);
        if (updated)
        {
            foreach (var tenantId in await GetTenantsOnPlanAsync(planId))
            {
                InvalidateTenantCache(tenantId);
            }
        }

        return updated;
    }

    public async Task<bool> DuplicatePlanAsync(Guid planId, string name, string code, Guid? actor)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A plan name and code are required.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        if (normalizedCode.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
        {
            throw new ArgumentException("Plan codes may contain only lowercase letters, digits, and underscores.");
        }

        return await _repository.DuplicatePlanAsync(planId, name.Trim(), normalizedCode, actor);
    }

    public async Task<bool> ArchivePlanAsync(Guid planId, Guid? actor)
    {
        var archived = await _repository.ArchivePlanAsync(planId, actor);
        if (archived)
        {
            foreach (var tenantId in await GetTenantsOnPlanAsync(planId))
            {
                InvalidateTenantCache(tenantId);
            }
        }

        return archived;
    }

    public async Task<bool> ReactivatePlanAsync(Guid planId, Guid? actor)
    {
        var reactivated = await _repository.ReactivatePlanAsync(planId, actor);
        if (reactivated)
        {
            foreach (var tenantId in await GetTenantsOnPlanAsync(planId))
            {
                InvalidateTenantCache(tenantId);
            }
        }

        return reactivated;
    }

    public async Task<bool> SaveTenantOverridesAsync(Guid tenantId, TenantOverridesRequest request, Guid? actor)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("A reason is required for tenant feature overrides.");
        }

        var catalog = await _repository.GetFeatureCatalogAsync();
        var featureTypes = catalog.ToDictionary(feature => feature.Key, feature => feature.Type, StringComparer.OrdinalIgnoreCase);
        foreach (var feature in request.Features)
        {
            if (!featureTypes.TryGetValue(feature.FeatureKey, out var type))
            {
                throw new ArgumentException($"Unknown or inactive feature key: {feature.FeatureKey}");
            }

            if (feature.Limit is < 0)
            {
                throw new ArgumentException($"Feature limit cannot be negative: {feature.FeatureKey}");
            }

            if (type == "boolean" && feature.Limit.HasValue)
            {
                throw new ArgumentException($"Boolean feature cannot have a limit: {feature.FeatureKey}");
            }

            if (type == "limit" && feature.Enabled && feature.Limit is null)
            {
                throw new ArgumentException($"A limit is required when enabling feature: {feature.FeatureKey}");
            }
        }

        var saved = await _repository.SaveTenantOverridesAsync(tenantId, request, actor);
        if (saved)
        {
            InvalidateTenantCache(tenantId);
        }

        return saved;
    }

    public async Task<bool> AssignTenantPlanAsync(Guid tenantId, TenantPlanAssignmentRequest request, Guid? actor)
    {
        if (!new[] { "trial", "active", "past_due", "cancelled", "expired" }
            .Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid subscription status.");
        }

        if (!new[] { "monthly", "annual" }.Contains(request.BillingCycle, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Billing cycle must be monthly or annual.");
        }

        if (string.Equals(request.Status, "trial", StringComparison.OrdinalIgnoreCase) &&
            request.TrialEndsAt is null)
        {
            throw new ArgumentException("A trial end date is required for trial subscriptions.");
        }

        if (request.CustomPrice is < 0 || request.CustomMaxUsers is < 1)
        {
            throw new ArgumentException("Custom price cannot be negative and a custom user limit must be positive.");
        }

        if (request.CustomCurrency.Length != 3)
        {
            throw new ArgumentException("Custom currency must be a three-letter ISO code.");
        }

        var plan = await _repository.GetPlanAsync(request.PlanId);
        if (plan == null)
        {
            throw new ArgumentException($"Plan ID '{request.PlanId}' was not found. Reload the plan list and try again.");
        }

        if (!string.Equals(plan.Status?.Trim(), "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Plan '{plan.Code}' is not active (status: '{plan.Status}'). Select an active plan.");
        }

        var maximumUsers = request.CustomMaxUsers ?? plan.MaxUsers;
        if (maximumUsers.HasValue)
        {
            var userCount = await _repository.GetUserCountAsync(tenantId, plan.UserCountType);
            if (userCount > maximumUsers.Value)
            {
                throw new InvalidOperationException(
                    $"This tenant has {userCount} users and exceeds the selected plan limit of {maximumUsers.Value}.");
            }
        }

        var updated = await _repository.AssignTenantPlanAsync(tenantId, request, actor);
        if (updated)
        {
            InvalidateTenantCache(tenantId);
        }

        return updated;
    }

    public async Task<bool> SaveTenantCustomPlanAsync(
        Guid tenantId,
        TenantCustomPlanRequest request,
        Guid? actor)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("A reason is required for tenant feature overrides.");
        }

        ValidateTenantPlanRequest(request);
        var catalog = await _repository.GetFeatureCatalogAsync();
        var featureTypes = catalog.ToDictionary(feature => feature.Key, feature => feature.Type, StringComparer.OrdinalIgnoreCase);
        foreach (var feature in request.Features)
        {
            if (!featureTypes.TryGetValue(feature.FeatureKey, out var type))
            {
                throw new ArgumentException($"Unknown or inactive feature key: {feature.FeatureKey}");
            }

            if (feature.Limit is < 0 || (type == "boolean" && feature.Limit.HasValue))
            {
                throw new ArgumentException($"Invalid limit for feature: {feature.FeatureKey}");
            }

            if (type == "limit" && feature.Enabled && feature.Limit is null)
            {
                throw new ArgumentException($"A limit is required when enabling feature: {feature.FeatureKey}");
            }
        }

        var plan = await _repository.GetPlanAsync(request.PlanId);
        if (plan == null)
        {
            throw new ArgumentException($"Plan ID '{request.PlanId}' was not found. Reload the plan list and try again.");
        }

        if (!string.Equals(plan.Status?.Trim(), "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Plan '{plan.Code}' is not active (status: '{plan.Status}'). Select an active plan.");
        }

        var maximumUsers = request.CustomMaxUsers ?? plan.MaxUsers;
        if (maximumUsers.HasValue)
        {
            var userCount = await _repository.GetUserCountAsync(tenantId, plan.UserCountType);
            if (userCount > maximumUsers.Value)
            {
                throw new InvalidOperationException(
                    $"This tenant has {userCount} users and exceeds the selected plan limit of {maximumUsers.Value}.");
            }
        }

        var saved = await _repository.SaveTenantCustomPlanAsync(tenantId, request, actor);
        if (saved)
        {
            InvalidateTenantCache(tenantId);
        }

        return saved;
    }

    public Task<IReadOnlyList<EntitlementAuditEntryDto>> GetAuditLogsAsync(
        Guid? tenantId,
        DateTime? from,
        DateTime? to,
        string? action)
    {
        if (from.HasValue && to.HasValue && to.Value < from.Value)
        {
            throw new ArgumentException("The audit end date must be after the start date.");
        }

        return _repository.GetAuditLogsAsync(tenantId, from, to, action);
    }

    private async Task ValidatePlanAsync(UpdatePlanEntitlementsRequest request, string? code)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Plan name is required.");
        }

        if (request.MaxUsers is < 1)
        {
            throw new ArgumentException("Maximum users must be greater than zero when specified.");
        }

        if (request.PriceMonthly is < 0 || request.PriceAnnual is < 0)
        {
            throw new ArgumentException("Plan prices cannot be negative.");
        }

        if (request.Currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a three-letter ISO code.");
        }

        if (request.UserCountType is not ("active_only" or "active_and_inactive"))
        {
            throw new ArgumentException("Invalid user count type.");
        }

        if (code != null)
        {
            var normalizedCode = code.Trim().ToLowerInvariant();
            if (normalizedCode.Length == 0 ||
                normalizedCode.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            {
                throw new ArgumentException("Plan codes may contain only lowercase letters, digits, and underscores.");
            }
        }

        var catalog = await _repository.GetFeatureCatalogAsync();
        var featureTypes = catalog.ToDictionary(feature => feature.Key, feature => feature.Type, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, feature) in request.Features)
        {
            if (!featureTypes.TryGetValue(key, out var type))
            {
                throw new ArgumentException($"Unknown or inactive feature key: {key}");
            }

            if (feature.Limit is < 0 || (type == "boolean" && feature.Limit.HasValue))
            {
                throw new ArgumentException($"Invalid limit for feature: {key}");
            }

            if (type == "limit" && feature.Enabled && !feature.Limit.HasValue)
            {
                throw new ArgumentException($"A limit is required when enabling feature: {key}");
            }
        }
    }

    private Task<IReadOnlyList<Guid>> GetTenantsOnPlanAsync(Guid planId) =>
        _repository.GetTenantsByPlanAsync(planId);

    private static string CacheKey(Guid tenantId) => $"tenant-entitlements:{tenantId:D}";

    private static void ValidateTenantPlanRequest(TenantPlanAssignmentRequest request)
    {
        if (!new[] { "trial", "active", "past_due", "cancelled", "expired" }
            .Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid subscription status.");
        }

        if (string.Equals(request.Status, "trial", StringComparison.OrdinalIgnoreCase) &&
            request.TrialEndsAt is null)
        {
            throw new ArgumentException("A trial end date is required for trial subscriptions.");
        }

        if (!new[] { "monthly", "annual" }.Contains(request.BillingCycle, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Billing cycle must be monthly or annual.");
        }

        if (request.CustomPrice is < 0 || request.CustomMaxUsers is < 1)
        {
            throw new ArgumentException("Custom price cannot be negative and a custom user limit must be positive.");
        }

        if (request.CustomCurrency.Length != 3)
        {
            throw new ArgumentException("Custom currency must be a three-letter ISO code.");
        }
    }
}
