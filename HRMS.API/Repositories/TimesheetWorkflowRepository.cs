using System.Data;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public interface ITimesheetWorkflowRepository
{
    Task<IEnumerable<TimesheetApprovalWorkflowDto>> GetWorkflowsAsync(Guid orgId);
    Task<TimesheetApprovalWorkflowDto?> GetWorkflowAsync(Guid workflowId);
    Task<Guid> CreateWorkflowAsync(Guid orgId, TimesheetApprovalWorkflowDto dto);
    Task<bool> UpdateWorkflowAsync(TimesheetApprovalWorkflowDto dto);
    Task<bool> DeleteWorkflowAsync(Guid workflowId);
    Task<IEnumerable<TimesheetApprovalProgressDto>> GetMyApprovalQueueAsync(Guid approverId, Guid orgId);
    Task<bool> ApproveStepAsync(Guid progressId);
    Task<bool> RejectStepAsync(Guid progressId, string reason);
    Task InitializeApprovalProgressAsync(Guid timesheetId, Guid employeeId, Guid orgId);
    Task<bool> IsEmployeeTimesheetApprovedAsync(Guid timesheetId, Guid employeeId);
    Task<Guid?> CreateReopenRequestAsync(Guid timesheetId, Guid employeeId, string reason);
    Task<IEnumerable<TimesheetReopenRequestDto>> GetPendingReopenRequestsAsync(Guid orgId);
    Task<bool> ApproveReopenRequestAsync(Guid reopenRequestId, Guid reviewerId);
    Task<bool> RejectReopenRequestAsync(Guid reopenRequestId, Guid reviewerId, string notes);
}

public class TimesheetWorkflowRepository : BaseRepository, ITimesheetWorkflowRepository
{
    public TimesheetWorkflowRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IEnumerable<TimesheetApprovalWorkflowDto>> GetWorkflowsAsync(Guid orgId)
    {
        var workflows = (await QueryAsync<TimesheetApprovalWorkflowDto>(@"
            SELECT workflow_id AS workflowid, organization_id AS organizationid,
                   workflow_name AS workflowname, is_default AS isdefault, is_active AS isactive
            FROM attendance.timesheet_approval_workflows
            WHERE organization_id = @orgId ORDER BY is_default DESC, workflowname",
            new { orgId })).ToList();

        foreach (var wf in workflows)
        {
            wf.Steps = (await QueryAsync<TimesheetApprovalStepDto>(@"
                SELECT step_id AS stepid, step_order AS steporder,
                       approver_role AS approverrole, approver_source AS approversource,
                       escalation_hours AS escalationhours, is_optional AS isoptional
                FROM attendance.timesheet_approval_steps
                WHERE workflow_id = @wfId ORDER BY step_order",
                new { wfId = wf.WorkflowId })).ToList();
        }
        return workflows;
    }

    public async Task<TimesheetApprovalWorkflowDto?> GetWorkflowAsync(Guid workflowId) { /* simplified */ return null; }
    public async Task<Guid> CreateWorkflowAsync(Guid orgId, TimesheetApprovalWorkflowDto dto)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(@"INSERT INTO attendance.timesheet_approval_workflows (workflow_id, organization_id, workflow_name, is_default) VALUES (@id, @orgId, @name, @def)",
            new { id, orgId, name = dto.WorkflowName, def = dto.IsDefault });
        foreach (var step in dto.Steps)
            await ExecuteAsync(@"INSERT INTO attendance.timesheet_approval_steps (workflow_id, step_order, approver_role, approver_source, escalation_hours, is_optional) VALUES (@wfId, @order, @role, @source, @esc, @opt)",
                new { wfId = id, order = step.StepOrder, role = step.ApproverRole, source = step.ApproverSource, esc = step.EscalationHours, opt = step.IsOptional });
        return id;
    }

    public async Task<bool> UpdateWorkflowAsync(TimesheetApprovalWorkflowDto dto) { return false; }
    public async Task<bool> DeleteWorkflowAsync(Guid workflowId) { return false; }

    public async Task<IEnumerable<TimesheetApprovalProgressDto>> GetMyApprovalQueueAsync(Guid approverId, Guid orgId)
    {
        return await QueryAsync<TimesheetApprovalProgressDto>(@"
            SELECT p.progress_id AS progressid, p.timesheet_id AS timesheetid,
                   p.employee_id AS employeeid, e.firstname || ' ' || e.lastname AS employeename,
                   s.step_order AS steporder, s.approver_role AS approverrole,
                   p.status AS status, p.created_at AS createdat
            FROM attendance.timesheet_approval_progress p
            JOIN attendance.timesheet_approval_steps s ON s.step_id = p.step_id
            JOIN employee.employees e ON e.employeeid = p.employee_id
            JOIN attendance.monthly_timesheets mt ON mt.timesheet_id = p.timesheet_id
            WHERE p.approver_id = @approverId AND p.status = 'Pending' AND mt.organization_id = @orgId
            ORDER BY p.created_at DESC",
            new { approverId, orgId });
    }

    public async Task<bool> ApproveStepAsync(Guid progressId)
    {
        var rows = await ExecuteAsync(@"
            UPDATE attendance.timesheet_approval_progress
            SET status = 'Approved', approved_at = now()
            WHERE progress_id = @id AND status = 'Pending'", new { id = progressId });
        return rows > 0;
    }

    public async Task<bool> RejectStepAsync(Guid progressId, string reason)
    {
        var rows = await ExecuteAsync(@"
            UPDATE attendance.timesheet_approval_progress
            SET status = 'Rejected', rejection_reason = @reason
            WHERE progress_id = @id AND status = 'Pending'", new { id = progressId, reason });
        return rows > 0;
    }

    public async Task InitializeApprovalProgressAsync(Guid timesheetId, Guid employeeId, Guid orgId)
    {
        var wf = await QuerySingleOrDefaultAsync<TimesheetApprovalWorkflowDto>(@"
            SELECT workflow_id AS workflowid FROM attendance.timesheet_approval_workflows
            WHERE organization_id = @orgId AND is_default = true AND is_active = true",
            new { orgId });
        if (wf == null) return;

        var steps = (await QueryAsync<TimesheetApprovalStepDto>(@"
            SELECT step_id AS stepid, approver_role AS approverrole, approver_source AS approversource
            FROM attendance.timesheet_approval_steps WHERE workflow_id = @wfId ORDER BY step_order",
            new { wfId = wf.WorkflowId })).ToList();

        foreach (var step in steps)
        {
            Guid? approverId = null;
            if (step.ApproverSource == "ReportingManager")
                approverId = await ExecuteScalarAsync<Guid?>(@"
                    SELECT ed.reportingmanagerid FROM employee.employmentdetails ed WHERE ed.employeeid = @empId AND ed.iscurrent = true",
                    new { empId = employeeId });
            if (approverId == null) continue;

            await ExecuteAsync(@"
                INSERT INTO attendance.timesheet_approval_progress (timesheet_id, employee_id, step_id, approver_id)
                VALUES (@tsId, @empId, @stepId, @approverId)
                ON CONFLICT (timesheet_id, employee_id, step_id) DO NOTHING",
                new { tsId = timesheetId, empId = employeeId, stepId = step.StepId, approverId });
        }
    }

    public async Task<bool> IsEmployeeTimesheetApprovedAsync(Guid timesheetId, Guid employeeId)
    {
        return await ExecuteScalarAsync<bool>(@"
            SELECT NOT EXISTS (
                SELECT 1 FROM attendance.timesheet_approval_progress
                WHERE timesheet_id = @tsId AND employee_id = @empId AND status != 'Approved'
            )",
            new { tsId = timesheetId, empId = employeeId });
    }

    public async Task<Guid?> CreateReopenRequestAsync(Guid timesheetId, Guid employeeId, string reason)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(@"
            INSERT INTO attendance.timesheet_reopen_requests (reopen_request_id, timesheet_id, requested_by, reason)
            VALUES (@id, @tsId, @empId, @reason)",
            new { id, tsId = timesheetId, empId = employeeId, reason });
        return id;
    }

    public async Task<IEnumerable<TimesheetReopenRequestDto>> GetPendingReopenRequestsAsync(Guid orgId)
    {
        return await QueryAsync<TimesheetReopenRequestDto>(@"
            SELECT rr.reopen_request_id AS reopenrequestid, rr.timesheet_id AS timesheetid,
                   mt.timesheet_name AS timesheetname, rr.requested_by AS requestedby,
                   e.firstname || ' ' || e.lastname AS requestedbyname,
                   rr.requested_at AS requestedat, rr.reason AS reason, rr.status AS status
            FROM attendance.timesheet_reopen_requests rr
            JOIN attendance.monthly_timesheets mt ON mt.timesheet_id = rr.timesheet_id
            JOIN employee.employees e ON e.employeeid = rr.requested_by
            WHERE mt.organization_id = @orgId AND rr.status = 'Pending'
            ORDER BY rr.requested_at DESC",
            new { orgId });
    }

    public async Task<bool> ApproveReopenRequestAsync(Guid reopenRequestId, Guid reviewerId)
    {
        var rows = await ExecuteAsync(@"
            UPDATE attendance.timesheet_reopen_requests
            SET status = 'Approved', reviewed_by = @reviewerId, reviewed_at = now()
            WHERE reopen_request_id = @id AND status = 'Pending'", new { id = reopenRequestId, reviewerId });
        if (rows > 0)
        {
            var tsId = await ExecuteScalarAsync<Guid>(@"
                SELECT timesheet_id FROM attendance.timesheet_reopen_requests WHERE reopen_request_id = @id",
                new { id = reopenRequestId });
            await ExecuteAsync(@"UPDATE attendance.monthly_timesheets SET status = 'InProgress' WHERE timesheet_id = @tsId",
                new { tsId });
        }
        return rows > 0;
    }

    public async Task<bool> RejectReopenRequestAsync(Guid reopenRequestId, Guid reviewerId, string notes)
    {
        var rows = await ExecuteAsync(@"
            UPDATE attendance.timesheet_reopen_requests
            SET status = 'Rejected', reviewed_by = @reviewerId, reviewed_at = now(), review_notes = @notes
            WHERE reopen_request_id = @id AND status = 'Pending'",
            new { id = reopenRequestId, reviewerId, notes });
        return rows > 0;
    }
}
