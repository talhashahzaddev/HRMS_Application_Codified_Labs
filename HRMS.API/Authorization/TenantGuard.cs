using System.Security.Claims;
using HRMS.API.Configuration;
using HRMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Authorization;

/// <summary>Records whose id may arrive in a URL and must be checked against the caller's organisation.</summary>
public enum TenantResource
{
    Employee, Department, Position,
    Attendance, Shift, GeoFence,
    ExpenseCategory, Expense, RecurringExpense,
    JobStage, JobOpening, JobApplication, ApplicationStage,
    SkillSet, Kra, Goal, Appraisal, AppraisalCycle,
}

public interface ITenantGuard
{
    Task<bool> BelongsToAsync(TenantResource resource, Guid id, Guid organizationId);
}

/// <summary>
/// One place that answers "does this id belong to this organisation?". Ids are global UUIDs, so any action that
/// takes an id from the request must check ownership before reading or changing the row.
/// </summary>
public sealed class TenantGuard : BaseRepository, ITenantGuard
{
    // Fixed SQL per resource (never built from input); @id and @org are the only parameters.
    private static readonly IReadOnlyDictionary<TenantResource, string> Sql = new Dictionary<TenantResource, string>
    {
        [TenantResource.Employee] = "SELECT 1 FROM employee.employees WHERE employeeid = @id AND organizationid = @org",
        [TenantResource.Department] = "SELECT 1 FROM employee.departments WHERE departmentid = @id AND organizationid = @org",
        [TenantResource.Position] = "SELECT 1 FROM employee.positions WHERE positionid = @id AND organizationid = @org",
        [TenantResource.Attendance] = @"SELECT 1 FROM attendance.attendance a JOIN employee.employees e ON e.employeeid = a.employeeid
                                         WHERE a.attendanceid = @id AND e.organizationid = @org",
        [TenantResource.Shift] = "SELECT 1 FROM attendance.shifts WHERE shiftid = @id AND organizationid = @org",
        [TenantResource.GeoFence] = "SELECT 1 FROM attendance.geo_fences WHERE geofenceid = @id AND organizationid = @org",
        [TenantResource.ExpenseCategory] = "SELECT 1 FROM expense.expensecategories WHERE categoryid = @id AND organizationid = @org",
        [TenantResource.Expense] = "SELECT 1 FROM expense.expenses WHERE expenseid = @id AND organizationid = @org",
        [TenantResource.RecurringExpense] = "SELECT 1 FROM expense.recurringexpenses WHERE recurringexpenseid = @id AND organizationid = @org",
        [TenantResource.JobStage] = "SELECT 1 FROM jobs.stagemaster WHERE stageid = @id AND organizationid = @org",
        [TenantResource.JobOpening] = "SELECT 1 FROM jobs.jobopenings WHERE jobid = @id AND organizationid = @org",
        [TenantResource.JobApplication] = "SELECT 1 FROM jobs.jobapplications WHERE jobapplyid = @id AND organizationid = @org",
        [TenantResource.ApplicationStage] = @"SELECT 1 FROM jobs.applicationstages s JOIN jobs.jobapplications a ON a.jobapplyid = s.jobapplyid
                                               WHERE s.applicationstageid = @id AND a.organizationid = @org",
        [TenantResource.SkillSet] = "SELECT 1 FROM performance.skillsets WHERE skillid = @id AND organizationid = @org",
        [TenantResource.AppraisalCycle] = "SELECT 1 FROM performance.appraisal_cycle WHERE cycleid = @id AND organizationid = @org",
        [TenantResource.Kra] = "SELECT 1 FROM performance.kra WHERE kraid = @id AND organizationid = @org",
        [TenantResource.Goal] = "SELECT 1 FROM performance.goals WHERE goalid = @id AND organizationid = @org",
        [TenantResource.Appraisal] = @"SELECT 1 FROM performance.employeeappraisals ap JOIN employee.employees e ON e.employeeid = ap.employeeid
                                        WHERE ap.appraisalid = @id AND e.organizationid = @org",
    };

    public TenantGuard(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<bool> BelongsToAsync(TenantResource resource, Guid id, Guid organizationId)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty) return false;
        return await ExecuteScalarAsync<bool>($"SELECT EXISTS ({Sql[resource]})", new { id, org = organizationId });
    }
}

public static class TenantGuardExtensions
{
    /// <summary>True when <paramref name="id"/> is owned by the organisation in the caller's token.</summary>
    public static async Task<bool> OwnsAsync(this ControllerBase controller, TenantResource resource, Guid id)
    {
        if (!Guid.TryParse(controller.User.FindFirstValue("organizationId"), out var org)) return false;
        var guard = controller.HttpContext.RequestServices.GetRequiredService<ITenantGuard>();
        return await guard.BelongsToAsync(resource, id, org);
    }

    public static NotFoundObjectResult NotFoundInTenant(this ControllerBase controller, string what) =>
        controller.NotFound(new { success = false, data = (object?)null, message = $"{what} not found" });
}
