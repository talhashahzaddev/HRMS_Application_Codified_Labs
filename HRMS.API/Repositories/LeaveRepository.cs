using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;

namespace HRMS.API.Repositories;

public class LeaveRepository : BaseRepository, ILeaveRepository
{
    public LeaveRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<LeaveRequest> CreateLeaveRequestAsync(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate, string? reason = null)
    {
        var requestId = await ExecuteScalarAsync<Guid>(
            "SELECT attendance.fn_create_leave_request(@employeeId, @leaveTypeId, @startDate, @endDate, @reason)",
            new { employeeId, leaveTypeId, startDate, endDate, reason });

        return new LeaveRequest
        {
            RequestId = requestId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason
        };
    }

    public async Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, string status, Guid approvedBy, string? rejectionReason = null)
    {
        return await ExecuteScalarAsync<bool>(
            "SELECT attendance.fn_update_leave_request_status(@requestId, @status, @approvedBy, @rejectionReason)",
            new { requestId, status, approvedBy, rejectionReason });
    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId = null, Guid? organizationId = null, string? status = null, int page = 1, int pageSize = 50)
    {
        var sql = @"
            SELECT lr.requestid, lr.employeeid, lr.leavetypeid, lr.startdate, lr.enddate,
                   lr.daysrequested, lr.reason, lr.status, lr.submittedat, lr.approvedat,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name,
                   lt.typename as leave_type_name,
                   CONCAT(a.firstname, ' ', a.lastname) as approver_name
            FROM attendance.leaverequests lr
            INNER JOIN employee.employees e ON lr.employeeid = e.employeeid
            INNER JOIN attendance.leavetypes lt ON lr.leavetypeid = lt.leavetypeid
            LEFT JOIN employee.employees a ON lr.approvedby = a.employeeid
            WHERE (@employeeId IS NULL OR lr.employeeid = @employeeId)
              AND (@organizationId IS NULL OR e.organizationid = @organizationId)
              AND (@status IS NULL OR lr.status = @status)
            ORDER BY lr.submittedat DESC
            LIMIT @pageSize OFFSET @offset";

        var results = await QueryAsync<dynamic>(sql, new 
        { 
            employeeId, 
            organizationId, 
            status, 
            pageSize, 
            offset = (page - 1) * pageSize 
        });

        var leaveRequests = results.Select(r => new LeaveRequestDto
        {
            RequestId = r.request_id,
            EmployeeId = r.employee_id,
            EmployeeName = r.employee_name,
            LeaveTypeId = r.leave_type_id,
            LeaveTypeName = r.leave_type_name,
            StartDate = r.start_date,
            EndDate = r.end_date,
            DaysRequested = r.days_requested,
            Reason = r.reason,
            Status = r.status,
            SubmittedAt = r.submitted_at,
            ApproverName = r.approver_name,
            ApprovedAt = r.approved_at
        }).ToList();

        // Get total count
        var countSql = @"
            SELECT COUNT(*)
            FROM attendance.leaverequests lr
            INNER JOIN employee.employees e ON lr.employeeid = e.employeeid
            WHERE (@employeeId IS NULL OR lr.employeeid = @employeeId)
              AND (@organizationId IS NULL OR e.organizationid = @organizationId)
              AND (@status IS NULL OR lr.status = @status)";

        var totalCount = await ExecuteScalarAsync<int>(countSql, new { employeeId, organizationId, status });

        return new PagedResult<LeaveRequestDto>
        {
            Data = leaveRequests,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT leavetypeid, typename, description, maxdaysperyear, 
                   ispaid, carryforwardallowed, color, isactive
            FROM attendance.leavetypes 
            WHERE organizationid = @organizationId AND isactive = true
            ORDER BY typename";

        var results = await QueryAsync<LeaveTypeDto>(sql, new { organizationId });
        return results.ToList();
    }

    public async Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year)
    {
        var sql = @"
            SELECT le.entitlement_id, le.employee_id, le.leave_type_id, le.year,
                   le.total_days, le.used_days, le.remaining_days, le.carry_forward_days,
                   lt.type_name as leave_type_name
            FROM attendance.leave_entitlements le
            INNER JOIN attendance.leave_types lt ON le.leave_type_id = lt.leave_type_id
            WHERE le.employee_id = @employeeId AND le.year = @year
            ORDER BY lt.type_name";

        var results = await QueryAsync<LeaveEntitlementDto>(sql, new { employeeId, year });
        return results.ToList();
    }

    public async Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year)
    {
        var sql = @"
            SELECT lt.typename as leave_type_name, lt.color,
                   COALESCE(le.totaldays, 0) as total_days,
                   COALESCE(le.useddays, 0) as used_days,
                   COALESCE(le.remainingdays, 0) as remaining_days
            FROM attendance.leavetypes lt
            LEFT JOIN attendance.leaveentitlements le ON lt.leavetypeid = le.leavetypeid 
                AND le.employeeid = @employeeId AND le.year = @year
            INNER JOIN employee.employees e ON e.employeeid = @employeeId
            WHERE lt.organizationid = e.organizationid AND lt.isactive = true
            ORDER BY lt.typename";

        var results = await QueryAsync<LeaveBalanceDto>(sql, new { employeeId, year });
        return results.ToList();
    }

    public async Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid requestId)
    {
        var sql = @"
            SELECT request_id, employee_id, leave_type_id, start_date, end_date,
                   days_requested, reason, status, submitted_at, approved_by, approved_at
            FROM attendance.leave_requests
            WHERE request_id = @requestId";

        return await QuerySingleOrDefaultAsync<LeaveRequest>(sql, new { requestId });
    }

    public async Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
    {
        var sql = @"
            SELECT COUNT(*) > 0
            FROM attendance.leave_requests
            WHERE employee_id = @employeeId
              AND status IN ('approved', 'pending')
              AND (@excludeRequestId IS NULL OR request_id != @excludeRequestId)
              AND NOT (end_date < @startDate OR start_date > @endDate)";

        return await ExecuteScalarAsync<bool>(sql, new { employeeId, startDate, endDate, excludeRequestId });
    }
}
