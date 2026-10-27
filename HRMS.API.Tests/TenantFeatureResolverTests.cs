using HRMS.API.Models.DTOs;
using HRMS.API.Services;
using Xunit;

namespace HRMS.API.Tests;

public sealed class TenantFeatureResolverTests
{
    [Fact]
    public void Resolve_ReturnsBasePlanFeatures()
    {
        var features = TenantFeatureResolver.Resolve(
            [Feature("dashboard", true)],
            []);

        Assert.True(features["dashboard"].Enabled);
    }

    [Fact]
    public void Resolve_AllowsOverrideToAddFeatureMissingFromBasePlan()
    {
        var features = TenantFeatureResolver.Resolve(
            [Feature("dashboard", true)],
            [Feature("geo_fencing", true)]);

        Assert.True(features["geo_fencing"].Enabled);
    }

    [Fact]
    public void Resolve_OverrideCanDisableBaseFeature()
    {
        var features = TenantFeatureResolver.Resolve(
            [Feature("payroll", true)],
            [Feature("payroll", false)]);

        Assert.False(features["payroll"].Enabled);
    }

    [Fact]
    public void Resolve_OverrideReplacesFeatureLimit()
    {
        var features = TenantFeatureResolver.Resolve(
            [Feature("ai_assistant", true, 50)],
            [Feature("ai_assistant", true, 500)]);

        Assert.Equal(500, features["ai_assistant"].Limit);
    }

    [Fact]
    public void NormalizeSubscriptionStatus_ExpiresWhenTrialHasEnded()
    {
        var now = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var status = EntitlementRules.NormalizeSubscriptionStatus(
            "trial",
            endDate: null,
            trialEndsAt: now.AddSeconds(-1),
            now);

        Assert.Equal("expired", status);
    }

    private static FeatureEntitlementDto Feature(string key, bool enabled, int? limit = null) =>
        new() { Key = key, Enabled = enabled, Limit = limit };
}
