using System.Reflection;
using AutoMapper;
using HRMS.API.Authorization;
using HRMS.API.Controllers;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HRMS.API.Tests;

/// <summary>Regression tests for the issues found in the QA audit (see QA_REPORT.md).</summary>
public sealed class QaSecurityRegressionTests
{
    // Endpoints that must work before login. Anything else without auth is a regression.
    private static readonly HashSet<string> PublicActions = new(StringComparer.Ordinal)
    {
        "Auth.Login", "Auth.RefreshToken", "Auth.Register", "Auth.ResendVerification",
        "Auth.ResetPassword", "Auth.ForgotPassword", "Auth.VerifyEmail",
        "PlatformAuth.Login",
        "Domain.ValidateDomain", "Domain.Health",
        "Entitlements.GetPlanComparison",
        "DemoInquiry.SubmitInquiry",
        "QuestionBank.GetJobQuestions", // public career application page
        "SetPassword.ValidateToken", "SetPassword.SetPassword", // invite link flow, protected by the invitation token
        "Uploads.UploadFile",
        "Payment.GetAllSubscriptionPlans", "Payment.GetActiveSubscriptionPlans", "Payment.GetSubscriptionPlanById",
    };

    // Whole controllers that are public by design (career site).
    private static readonly HashSet<string> PublicControllers = new(StringComparer.Ordinal) { "Career" };

    private static IEnumerable<(Type controller, MethodInfo action)> AllActions() =>
        typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(m => (t, m)));

    private static string Name(Type c, MethodInfo m) => $"{c.Name.Replace("Controller", "")}.{m.Name}";

    private static bool RequiresAuth(Type c, MethodInfo m)
    {
        if (m.GetCustomAttributes<AllowAnonymousAttribute>(true).Any() || c.GetCustomAttributes<AllowAnonymousAttribute>(true).Any())
            return false;
        return m.GetCustomAttributes<AuthorizeAttribute>(true).Any() || c.GetCustomAttributes<AuthorizeAttribute>(true).Any();
    }

    [Fact]
    public void EveryEndpoint_RequiresAuthentication_UnlessExplicitlyPublic()
    {
        var unprotected = AllActions()
            .Where(x => !PublicControllers.Contains(x.controller.Name.Replace("Controller", "")))
            .Where(x => !PublicActions.Contains(Name(x.controller, x.action)))
            .Where(x => !RequiresAuth(x.controller, x.action))
            .Select(x => Name(x.controller, x.action))
            .OrderBy(x => x)
            .ToList();

        Assert.True(unprotected.Count == 0, "Endpoints reachable without login: " + string.Join(", ", unprotected));
    }

    [Fact]
    public void PublicAllowList_OnlyNamesRealActions()
    {
        var names = AllActions().Select(x => Name(x.controller, x.action)).ToHashSet();
        Assert.All(PublicActions, a => Assert.Contains(a, names));
    }

    [Fact]
    public void AuthController_HasNoDebugTokenEndpoints()
    {
        var names = typeof(AuthController).GetMethods().Select(m => m.Name).ToList();
        Assert.DoesNotContain("TestHardcodedToken", names);
        Assert.DoesNotContain("TestAuth", names);
    }

    [Fact]
    public void OrganizationController_IsPlatformAdminOnly()
    {
        var attr = typeof(OrganizationController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Platform Admin", attr!.Roles);
    }

    [Fact]
    public void UploadsController_OnlyUploadIsAnonymous()
    {
        Assert.Empty(typeof(UploadsController).GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.NotNull(typeof(UploadsController).GetMethod(nameof(UploadsController.UploadFile))!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(typeof(UploadsController).GetMethod(nameof(UploadsController.DeleteFile))!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(typeof(AuthController), "CreateRoleAsync", "add_new_role")]
    [InlineData(typeof(AuthController), "UpdateRoleAsync", "role_edit")]
    [InlineData(typeof(AuthController), "DeleteRoleAsync", "role_delete")]
    [InlineData(typeof(IpAddressController), "Create", "manage_ip_add")]
    [InlineData(typeof(IpAddressController), "Update", "manage_ip_edit")]
    [InlineData(typeof(IpAddressController), "Delete", "manage_ip_delete")]
    [InlineData(typeof(NewsController), "EditNews", "Edit_News_Button")]
    [InlineData(typeof(NewsController), "DeleteNews", "Delete_News_Button")]
    [InlineData(typeof(NewsController), "UpdateNewsStatus", "Status_News_Toggle")]
    [InlineData(typeof(PerformanceController), "UpdateAppraisalCycle", "APPRAISAL_CYCLE_EDIT_BUTTON")]
    public void SensitiveActions_RequirePermission(Type controller, string action, string permission)
    {
        var method = controller.GetMethod(action)!;
        var attr = method.GetCustomAttribute<PermissionAuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"Permissions:{permission}", attr!.Policy);
    }

    [Theory]
    [InlineData("CreateSubscriptionPlan")]
    [InlineData("UpdateSubscriptionPlan")]
    [InlineData("InactivateSubscriptionPlan")]
    [InlineData("DeleteSubscriptionPlan")]
    [InlineData("GetAllCompanySubscriptions")]
    [InlineData("GetCompanySubscriptionById")]
    [InlineData("UpdateCompanySubscription")]
    [InlineData("DeleteCompanySubscription")]
    public void PlatformWidePaymentActions_ArePlatformAdminOnly(string action)
    {
        var attr = typeof(PaymentController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Platform Admin", attr?.Roles);
    }

    [Fact]
    public void TenantGuard_HasSqlForEveryResource()
    {
        var field = typeof(TenantGuard).GetField("Sql", BindingFlags.NonPublic | BindingFlags.Static)!;
        var map = (IReadOnlyDictionary<TenantResource, string>)field.GetValue(null)!;
        foreach (var r in Enum.GetValues<TenantResource>())
        {
            Assert.True(map.ContainsKey(r), $"No ownership SQL for {r}");
            Assert.Contains("@id", map[r]);
            Assert.Contains("@org", map[r]);
        }
    }

    [Fact]
    public async Task TenantGuard_EmptyIds_AreNeverOwned_WithoutTouchingTheDatabase()
    {
        var guard = new TenantGuard(Mock.Of<HRMS.API.Configuration.IDatabaseConnectionFactory>(MockBehavior.Strict));
        Assert.False(await guard.BelongsToAsync(TenantResource.Expense, Guid.Empty, Guid.NewGuid()));
        Assert.False(await guard.BelongsToAsync(TenantResource.Expense, Guid.NewGuid(), Guid.Empty));
    }

    // ---------- tenant isolation (service level) ----------

    [Fact]
    public async Task Leave_GetById_WithOtherOrganization_ReturnsNull()
    {
        var repo = new Mock<ILeaveRepository>();
        var requestId = Guid.NewGuid();
        repo.Setup(r => r.LeaveRequestBelongsToOrganizationAsync(requestId, It.IsAny<Guid>())).ReturnsAsync(false);
        repo.Setup(r => r.GetLeaveRequestByIdAsync(requestId)).ReturnsAsync(new LeaveRequest { RequestId = requestId });
        var service = new LeaveService(repo.Object, Mock.Of<INotificationRepository>(), Mock.Of<IEmployeeRepository>(),
            Mock.Of<IAttendanceRepository>(), Mock.Of<IHolidayRepository>(), Mock.Of<IEmailService>(), Mock.Of<IMapper>(),
            NullLogger<LeaveService>.Instance);

        var result = await service.GetLeaveRequestByIdAsync(requestId, Guid.NewGuid());

        Assert.Null(result);
        repo.Verify(r => r.GetLeaveRequestByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    private static PerformanceService Performance(Mock<IPerformanceRepository> repo) => new(
        repo.Object, Mock.Of<INotificationService>(), Mock.Of<INotificationRepository>(), Mock.Of<IAttendanceRepository>(),
        Mock.Of<IEmployeeRepository>(), Mock.Of<IMapper>(), NullLogger<PerformanceService>.Instance);

    [Fact]
    public async Task AppraisalCycle_Update_OtherOrganization_IsNotFound()
    {
        var repo = new Mock<IPerformanceRepository>();
        var cycleId = Guid.NewGuid();
        repo.Setup(r => r.GetAppraisalCycleByIdAsync(cycleId))
            .ReturnsAsync(new AppraisalCycleDto { CycleId = cycleId, OrganizationId = Guid.NewGuid(), StartDate = DateTime.UtcNow.AddMonths(1) });

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Performance(repo).UpdateAppraisalCycleAsync(cycleId, new UpdateAppraisalCycleRequest(), Guid.NewGuid()));
        repo.Verify(r => r.UpdateAppraisalCycleAsync(It.IsAny<Guid>(), It.IsAny<UpdateAppraisalCycleRequest>()), Times.Never);
    }

    [Fact]
    public async Task AppraisalCycle_Update_UnknownId_IsNotFoundNotNullReference()
    {
        var repo = new Mock<IPerformanceRepository>();
        repo.Setup(r => r.GetAppraisalCycleByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AppraisalCycleDto?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Performance(repo).UpdateAppraisalCycleAsync(Guid.NewGuid(), new UpdateAppraisalCycleRequest(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Skill_Delete_PassesCallerOrganizationToRepository()
    {
        var repo = new Mock<IPerformanceRepository>();
        var org = Guid.NewGuid();
        var skill = Guid.NewGuid();
        repo.Setup(r => r.DeleteSkillSetAsync(skill, org)).ReturnsAsync(true);

        Assert.True(await Performance(repo).DeleteSkillSetAsync(skill, org));
        repo.Verify(r => r.DeleteSkillSetAsync(skill, org), Times.Once);
    }

    // ---------- payroll input validation ----------

    private static PayRollService Payroll(Mock<IPayRollRepository> repo) =>
        new(repo.Object, Mock.Of<IAttendanceRepository>(), Mock.Of<ISettingsRepository>(), Mock.Of<IPayslipTemplateService>(),
            Mock.Of<IPayslipPdfBuilder>(), NullLogger<PayRollService>.Instance, Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>());

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task SalaryAdvance_NonPositiveAmount_IsRejected(decimal amount)
    {
        var repo = new Mock<IPayRollRepository>();
        var result = await Payroll(repo).CreateSalaryAdvanceAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateSalaryAdvanceDto { Amount = amount });

        Assert.False(result.Success);
        repo.Verify(r => r.CreateSalaryAdvanceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CreateSalaryAdvanceDto>()), Times.Never);
    }

    [Fact]
    public async Task SalaryAdvance_PositiveAmount_IsSaved()
    {
        var repo = new Mock<IPayRollRepository>();
        repo.Setup(r => r.CreateSalaryAdvanceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CreateSalaryAdvanceDto>()))
            .ReturnsAsync(new SalaryAdvanceDto());
        var result = await Payroll(repo).CreateSalaryAdvanceAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateSalaryAdvanceDto { Amount = 1000 });
        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(0, "temporary")]
    [InlineData(100, "forever")]
    [InlineData(100, "")]
    public async Task ProvidentFundWithdrawal_InvalidInput_IsRejected(decimal amount, string type)
    {
        var repo = new Mock<IPayRollRepository>();
        var result = await Payroll(repo).CreateProvidentFundWithdrawRequestAsync(Guid.NewGuid(), Guid.NewGuid(),
            new CreateProvidentFundWithdrawRequestDto { Amount = amount, WithdrawalType = type });

        Assert.False(result.Success);
        repo.Verify(r => r.CreateProvidentFundWithdrawRequestAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CreateProvidentFundWithdrawRequestDto>()), Times.Never);
    }
}
