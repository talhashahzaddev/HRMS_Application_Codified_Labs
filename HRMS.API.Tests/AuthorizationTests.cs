using System.Security.Claims;
using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HRMS.API.Tests;

public sealed class AuthorizationTests
{
    private static readonly Guid RoleId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();

    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private static ClaimsPrincipal User(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));

    private void RolePermissions(Guid roleId, params (string key, bool granted)[] permissions) =>
        _authRepo.Setup(r => r.GetRolePermissionsWithMenuDetailsAsync(roleId))
            .ReturnsAsync(permissions.Select(p => new PermissionDetail { ActionKey = p.key, HasPermission = p.granted }).ToList());

    private async Task<bool> Authorize(ClaimsPrincipal user, params string[] required)
    {
        var handler = new PermissionAuthorizationHandler(
            NullLogger<PermissionAuthorizationHandler>.Instance, _authRepo.Object, _cache);
        var requirement = new PermissionRequirement(required);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    // ---------- PermissionAuthorizeAttribute / PolicyProvider ----------

    [Fact]
    public void Attribute_BuildsPolicyName()
    {
        var attr = new PermissionAuthorizeAttribute("add_employee", "edit_employee");
        Assert.Equal("Permissions:add_employee,edit_employee", attr.Policy);
    }

    [Fact]
    public async Task PolicyProvider_ParsesPermissionsFromPolicyName()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("Permissions:add_employee, edit_employee,,");

        var requirement = Assert.Single(policy!.Requirements.OfType<PermissionRequirement>());
        Assert.Equal(new[] { "add_employee", "edit_employee" }, requirement.Permissions);
        Assert.Contains(policy.Requirements, r => r is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task PolicyProvider_UnknownPolicy_FallsBackToDefault()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        Assert.Null(await provider.GetPolicyAsync("SomethingElse"));
    }

    // ---------- PermissionAuthorizationHandler ----------

    [Theory]
    [InlineData("Super Admin")]
    [InlineData("super admin")]
    [InlineData("  Super Admin ")]
    public async Task Handler_SuperAdmin_AlwaysAllowed(string role)
    {
        Assert.True(await Authorize(User(new Claim(ClaimTypes.Role, role)), "delete_everything"));
        _authRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handler_RoleWithAllPermissions_IsAllowed()
    {
        RolePermissions(RoleId, ("view_payroll", true), ("edit_payroll", true));
        var user = User(new Claim(CustomClaimTypes.RoleId, RoleId.ToString()));

        Assert.True(await Authorize(user, "view_payroll", "EDIT_PAYROLL"));
    }

    [Fact]
    public async Task Handler_MissingOnePermission_IsDenied()
    {
        RolePermissions(RoleId, ("view_payroll", true));
        var user = User(new Claim(CustomClaimTypes.RoleId, RoleId.ToString()));

        Assert.False(await Authorize(user, "view_payroll", "edit_payroll"));
    }

    [Fact]
    public async Task Handler_PermissionPresentButNotGranted_IsDenied()
    {
        RolePermissions(RoleId, ("edit_payroll", false));
        var user = User(new Claim(CustomClaimTypes.RoleId, RoleId.ToString()));

        Assert.False(await Authorize(user, "edit_payroll"));
    }

    [Fact]
    public async Task Handler_NoRoleInfo_IsDenied()
    {
        Assert.False(await Authorize(User(), "view_payroll"));
    }

    [Fact]
    public async Task Handler_ResolvesRoleByNameWhenRoleIdClaimMissing()
    {
        _authRepo.Setup(r => r.GetRoleByNameforTokenAsync("HR Manager", OrgId)).ReturnsAsync(new Role { RoleId = RoleId });
        RolePermissions(RoleId, ("view_payroll", true));
        var user = User(new Claim(ClaimTypes.Role, "HR Manager"), new Claim(CustomClaimTypes.OrganizationId, OrgId.ToString()));

        Assert.True(await Authorize(user, "view_payroll"));
    }

    [Fact]
    public async Task Handler_UnknownRoleName_IsDeniedNotCrashed()
    {
        _authRepo.Setup(r => r.GetRoleByNameforTokenAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync((Role)null!);
        var user = User(new Claim(ClaimTypes.Role, "Ghost"), new Claim(CustomClaimTypes.OrganizationId, OrgId.ToString()));

        Assert.False(await Authorize(user, "view_payroll"));
    }

    [Fact]
    public async Task Handler_CachesPermissionsPerRole()
    {
        RolePermissions(RoleId, ("view_payroll", true));
        var user = User(new Claim(CustomClaimTypes.RoleId, RoleId.ToString()));

        await Authorize(user, "view_payroll");
        await Authorize(user, "view_payroll");

        _authRepo.Verify(r => r.GetRolePermissionsWithMenuDetailsAsync(RoleId), Times.Once);
    }

    // ---------- ClaimsPrincipalExtensions ----------

    [Fact]
    public void Extensions_TokenPermissionClaims()
    {
        var user = User(new Claim(CustomClaimTypes.Permission, "a"), new Claim(CustomClaimTypes.Permission, "b"));

        Assert.True(user.HasPermission("a"));
        Assert.False(user.HasPermission("c"));
        Assert.True(user.HasAnyPermission("c", "b"));
        Assert.False(user.HasAllPermissions("a", "c"));
        Assert.Equal(new[] { "a", "b" }, user.GetPermissions());
    }

    [Fact]
    public void Extensions_FallsBackToRolePermissionsFromStore()
    {
        RolePermissions(RoleId, ("view_payroll", true), ("edit_payroll", false));
        var user = User(new Claim(CustomClaimTypes.RoleId, RoleId.ToString()));

        Assert.True(user.HasPermission("VIEW_PAYROLL", _authRepo.Object, _cache));
        Assert.False(user.HasPermission("edit_payroll", _authRepo.Object, _cache));
        Assert.False(user.HasPermission("", _authRepo.Object, _cache));
        Assert.Equal(new[] { "view_payroll" }, user.GetPermissions(_authRepo.Object, _cache));
    }
}
