using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;
using Npgsql;

namespace HRMS.API.Repositories;

public class LeaveRepository : BaseRepository, ILeaveRepository
{
    public LeaveRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Guid> GetEmpId(Guid? employeeId )
    {
        try
        {
            var sql = @"
            SELECT em.employeeid
            FROM employee.employees em
            WHERE ( em.userid = @employeeId)
            LIMIT 1";

            var results = await QueryAsync<Guid>(sql, new{employeeId});

            return results.FirstOrDefault();
        }
        catch(Exception ex)
        {
            return Guid.Empty;
        }
    }
    public async Task<LeaveRequest> CreateLeaveRequestAsync(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate, string? reason = null)
    {
        try
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
        catch(Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, string status, Guid approvedBy, string? rejectionReason = null)
    {
        try
        {
            return await ExecuteScalarAsync<bool>(
            "SELECT attendance.fn_update_leave_request_status(@requestId, @status, @approvedBy, @rejectionReason)",
            new { requestId, status, approvedBy, rejectionReason });
        }
        catch(Exception ex)
        {
            return false; 
        }
    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId = null, Guid? organizationId = null, string? status = null, int page = 1, int pageSize = 50)
    {
       try
        {
            var sql = @"
            SELECT lr.requestid, lr.employeeid, lr.leavetypeid, lr.startdate, lr.enddate,
                   lr.daysrequested, lr.reason, lr.status, lr.submittedat, lr.approvedat,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name,
                   e.profilepictureurl,
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
                RequestId = r.requestid ?? Guid.Empty,
                EmployeeId = r.employeeid ?? Guid.Empty,
                EmployeeName = r.employee_name ?? string.Empty,
                LeaveTypeId = r.leavetypeid ?? Guid.Empty,
                LeaveTypeName = r.leave_type_name ?? string.Empty,
                StartDate = r.startdate ?? DateTime.MinValue,  // ✅ Avoids null crash
                EndDate = r.enddate ?? DateTime.MinValue,      // ✅ Avoids null crash
                DaysRequested = r.daysrequested ?? 0,
                Reason = r.reason ?? string.Empty,
                Status = r.status ?? string.Empty,
                SubmittedAt = r.submittedat ?? DateTime.MinValue, // ✅ Nullable support
                ApproverName = r.approver_name ?? string.Empty,
                ProfilePictureUrl = r.profilepictureurl ?? string.Empty,
                ApprovedAt = r.approvedat // This is nullable in DTO
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
        catch(Exception ex)
        {
            return null;
        }
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
    public async Task<bool> CreateLeaveTypeAsync(Guid organizationId, LeaveTypeDto dto)
    {
        var sqlInsert = @"
        INSERT INTO attendance.leavetypes 
            (leavetypeid, organizationid, typename, description, maxdaysperyear, 
             ispaid, carryforwardallowed, color, isactive, createdat)
        VALUES
            (@LeaveTypeId, @OrganizationId, @TypeName, @Description, @MaxDaysPerYear, 
             @IsPaid, @CarryForwardAllowed, @Color, @IsActive, NOW());
    ";

        var sqlInitEntitlements = @"
        SELECT attendance.fn_initialize_leave_entitlements_for_new_leavetype(@LeaveTypeId, @OrganizationId);
    ";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        {
            using var transaction = await connection.BeginTransactionAsync();
            {
                try
                {
                    var rowsAffected = await connection.ExecuteAsync(sqlInsert, new
                    {
                        dto.LeaveTypeId,
                        OrganizationId = organizationId,
                        dto.TypeName,
                        dto.Description,
                        dto.MaxDaysPerYear,
                        dto.IsPaid,
                        dto.CarryForwardAllowed,
                        dto.Color,
                        dto.IsActive
                    }, transaction);

                    if (rowsAffected > 0)
                    {
                        await connection.ExecuteAsync(sqlInitEntitlements, new
                        {
                            dto.LeaveTypeId,
                            OrganizationId = organizationId
                        }, transaction);
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error inserting LeaveType: {ex.Message}");
                    return false;
                }
            }
        }
    }




    public async Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year)
    {
        var sql = @"
            SELECT le.entitlementid, le.employeeid, le.leavetypeid, le.year,
                   le.totaldays, le.useddays, le.remainingdays, le.carryforwarddays,
                   lt.typename as leavetypename
            FROM attendance.leaveentitlements le
            INNER JOIN attendance.leavetypes lt ON le.leavetypeid = lt.leavetypeid
            WHERE le.employeeid = @employeeId AND le.year = @year
            ORDER BY lt.typename";

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
        try
        {
            var sql = @"
            SELECT requestid, employeeid, leavetypeid, startdate, enddate,
                   daysrequested, reason, status, submittedat, approvedby, approvedat
            FROM attendance.leaverequests
            WHERE requestid = @requestId";

            return await QuerySingleOrDefaultAsync<LeaveRequest>(sql, new { requestId });
        }
        catch(Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
    {
       try
        {
            var employeeid = await GetEmpId(employeeId);
            
            var sql = @"
SELECT COUNT(*) > 0
FROM attendance.leaverequests
WHERE employeeid = @employeeid
  AND status IN ('approved', 'pending','rejected')
  AND (@excludeRequestId IS NULL OR requestid != @excludeRequestId)
  AND NOT (enddate < @startDate OR startdate > @endDate)";


            return await ExecuteScalarAsync<bool>(sql, new { employeeid, startDate, endDate, excludeRequestId });
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error checking leave overlap: {ex.Message}");
            throw new Exception("An error occurred while checking leave overlap.");

            return false;
        }
    }

    public async Task<List<LeaveRequest>> GetLeaveRequestsByEmployeeId(Guid employeeId)
    {
        try
        {
            var sql = @"
        SELECT *
        FROM attendance.leaverequests
        WHERE employeeid = @employeeId";

            var results = await QueryAsync<LeaveRequest>(sql, new { employeeId });
            return results.ToList();
        }
        catch (Exception ex)
        {
            return new List<LeaveRequest>();
        }
    }
    public async Task<List<LeaveCalendarDto>> GetLeaveRequestsAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT lr.requestid, 
               lr.employeeid, 
               lr.leavetypeid, 
               lt.typename AS LeaveTypeName,
               lr.startdate, 
               lr.enddate, 
               lr.daysrequested, 
               lr.reason, 
               lr.attachmenturl, 
               lr.status, 
               lr.submittedat, 
               lr.approvedby, 
               lr.approvedat, 
               lr.rejectionreason, 
               lr.createdat, 
               lr.updatedat
        FROM attendance.leaverequests lr
        INNER JOIN attendance.leavetypes lt ON lr.leavetypeid = lt.leavetypeid
        WHERE lr.employeeid = @employeeId
          AND lr.startdate >= @startDate
          AND lr.enddate <= @endDate
        ORDER BY lr.startdate;";

        var results = await QueryAsync<LeaveCalendarDto>(sql, new { employeeId, startDate, endDate });
        return results.ToList();
    }


}
