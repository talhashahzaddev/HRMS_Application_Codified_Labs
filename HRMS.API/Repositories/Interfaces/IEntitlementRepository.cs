using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IEntitlementRepository
{
    Task<EntitlementTenantState?> GetTenantStateAsync(Guid tenantId);
    Task<IReadOnlyList<Guid>> GetTenantsByPlanAsync(Guid planId);
    Task<IReadOnlyList<FeatureEntitlementDto>> GetEffectiveFeaturesAsync(Guid tenantId, Guid? planId);
    Task<int> GetUserCountAsync(Guid tenantId, string userCountType);
    Task<int> GetAiQueryCountAsync(Guid tenantId);
    Task<bool> TryConsumeAiQueryAsync(Guid tenantId, int limit);
    Task<IReadOnlyList<EntitlementFeatureDto>> GetFeatureCatalogAsync();
    Task<IReadOnlyList<EntitlementPlanDto>> GetPlansAsync(bool includeArchived);
    Task<EntitlementPlanDto?> GetPlanAsync(Guid planId);
    Task<Guid> CreatePlanAsync(CreatePlanEntitlementsRequest request, Guid? actor);
    Task<bool> UpdatePlanAsync(Guid planId, UpdatePlanEntitlementsRequest request, Guid? actor);
    Task<bool> DuplicatePlanAsync(Guid planId, string name, string code, Guid? actor);
    Task<bool> ArchivePlanAsync(Guid planId, Guid? actor);
    Task<bool> ReactivatePlanAsync(Guid planId, Guid? actor);
    Task<bool> SaveTenantOverridesAsync(Guid tenantId, TenantOverridesRequest request, Guid? actor);
    Task<bool> AssignTenantPlanAsync(Guid tenantId, TenantPlanAssignmentRequest request, Guid? actor);
    Task<bool> SaveTenantCustomPlanAsync(Guid tenantId, TenantCustomPlanRequest request, Guid? actor);
    Task<IReadOnlyList<EntitlementAuditEntryDto>> GetAuditLogsAsync(Guid? tenantId, DateTime? from, DateTime? to, string? action);
}

public sealed class EntitlementTenantState
{
    public Guid? PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string SubscriptionStatus { get; set; } = "active";
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? StartDate { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public int? MaxUsers { get; set; }
    public decimal? PriceMonthly { get; set; }
    public string Currency { get; set; } = "BHD";
    public string UserCountType { get; set; } = "active_only";
}
