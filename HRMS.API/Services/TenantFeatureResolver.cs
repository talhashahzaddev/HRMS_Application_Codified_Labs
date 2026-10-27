using HRMS.API.Models.DTOs;

namespace HRMS.API.Services;

public static class TenantFeatureResolver
{
    public static IReadOnlyDictionary<string, FeatureEntitlementDto> Resolve(
        IEnumerable<FeatureEntitlementDto> planFeatures,
        IEnumerable<FeatureEntitlementDto> tenantOverrides)
    {
        var effective = planFeatures.ToDictionary(
            feature => feature.Key,
            feature => new FeatureEntitlementDto
            {
                Key = feature.Key,
                Enabled = feature.Enabled,
                Limit = feature.Limit
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (var featureOverride in tenantOverrides)
        {
            effective[featureOverride.Key] = new FeatureEntitlementDto
            {
                Key = featureOverride.Key,
                Enabled = featureOverride.Enabled,
                Limit = featureOverride.Limit
            };
        }

        return effective;
    }
}

public static class EntitlementRules
{
    public static string NormalizeSubscriptionStatus(
        string status,
        DateTime? endDate,
        DateTime? trialEndsAt,
        DateTime now)
    {
        if (endDate < now || trialEndsAt < now)
        {
            return "expired";
        }

        return status.ToLowerInvariant();
    }
}
