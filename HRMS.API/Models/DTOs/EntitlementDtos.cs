namespace HRMS.API.Models.DTOs;

public sealed class TenantEntitlementsDto
{
    public Guid? PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string SubscriptionStatus { get; set; } = "active";
    public int? MaxUsers { get; set; }
    public decimal? PriceMonthly { get; set; }
    public string Currency { get; set; } = "BHD";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public string UserCountType { get; set; } = "active_only";
    public int UserCount { get; set; }
    public bool IsReadOnly { get; set; }
    public int AiQueriesUsedThisMonth { get; set; }
    public int? AiQueriesRemaining { get; set; }
    public Dictionary<string, FeatureEntitlementDto> Features { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class FeatureEntitlementDto
{
    public string Key { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int? Limit { get; set; }
}

public sealed class EntitlementFeatureDto
{
    public Guid FeatureId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "boolean";
    public bool IsActive { get; set; }
}

public sealed class EntitlementPlanDto
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal? PriceMonthly { get; set; }
    public decimal? PriceAnnual { get; set; }
    public string Currency { get; set; } = "BHD";
    public int? MaxUsers { get; set; }
    public string UserCountType { get; set; } = "active_only";
    public bool IsCustom { get; set; }
    public bool IsPublic { get; set; }
    public bool IsRecommended { get; set; }
    public string Status { get; set; } = "active";
    public int Version { get; set; }
    public Dictionary<string, FeatureEntitlementDto> Features { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class UpdatePlanEntitlementsRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? PriceMonthly { get; set; }
    public decimal? PriceAnnual { get; set; }
    public string Currency { get; set; } = "BHD";
    public int? MaxUsers { get; set; }
    public string UserCountType { get; set; } = "active_only";
    public bool IsPublic { get; set; }
    public bool IsRecommended { get; set; }
    public int Version { get; set; }
    public Dictionary<string, FeatureEntitlementDto> Features { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CreatePlanEntitlementsRequest : UpdatePlanEntitlementsRequest
{
    public string Code { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}

public sealed class TenantFeatureOverrideDto
{
    public string FeatureKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int? Limit { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class TenantOverridesRequest
{
    public string Reason { get; set; } = string.Empty;
    public List<TenantFeatureOverrideDto> Features { get; set; } = new();
}

public sealed class TenantCustomPlanRequest : TenantPlanAssignmentRequest
{
    public string Reason { get; set; } = string.Empty;
    public List<TenantFeatureOverrideDto> Features { get; set; } = new();
}

public class TenantPlanAssignmentRequest
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "active";
    public string BillingCycle { get; set; } = "monthly";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public decimal? CustomPrice { get; set; }
    public int? CustomMaxUsers { get; set; }
    public string CustomCurrency { get; set; } = "BHD";
}

public sealed class EntitlementAuditEntryDto
{
    public Guid AuditId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? Actor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public DateTime CreatedAt { get; set; }
}
