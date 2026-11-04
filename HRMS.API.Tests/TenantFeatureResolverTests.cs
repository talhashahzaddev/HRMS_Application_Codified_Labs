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

public sealed class TenantFeatureResolverExtraTests
{
    private static FeatureEntitlementDto F(string key, bool enabled, int? limit = null) =>
        new() { Key = key, Enabled = enabled, Limit = limit };

    [Fact]
    public void Resolve_OverrideKeyIsCaseInsensitive()
    {
        var features = TenantFeatureResolver.Resolve([F("Payroll", true)], [F("payroll", false)]);
        Assert.Single(features);
        Assert.False(features["PAYROLL"].Enabled);
    }

    [Fact]
    public void Resolve_DoesNotMutateInputPlan()
    {
        var plan = F("payroll", true, 10);
        TenantFeatureResolver.Resolve([plan], [F("payroll", false, 1)]);
        Assert.True(plan.Enabled);
        Assert.Equal(10, plan.Limit);
    }

    [Fact]
    public void NormalizeStatus_ExpiresWhenEndDatePassed()
    {
        var now = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("expired", EntitlementRules.NormalizeSubscriptionStatus("Active", now.AddDays(-1), null, now));
    }

    [Fact]
    public void NormalizeStatus_ActiveSubscriptionIsLowercased()
    {
        var now = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("active", EntitlementRules.NormalizeSubscriptionStatus("ACTIVE", now.AddDays(30), null, now));
    }
}
