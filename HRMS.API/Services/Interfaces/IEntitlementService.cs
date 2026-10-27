using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IEntitlementService
{
    Task<TenantEntitlementsDto> GetTenantEntitlementsAsync(Guid tenantId);
    Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureKey);
    Task<bool> TryConsumeAiQueryAsync(Guid tenantId, int? limit);
    Task<int> GetUserCountAsync(Guid tenantId, string userCountType);
    void InvalidateTenantCache(Guid tenantId);
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
