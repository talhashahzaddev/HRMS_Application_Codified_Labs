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
        catch (Exception ex)
        {
            return null;
        }
    }


    public async Task<bool> SubstractLeaveDayAsync(
    Guid leaveTypeId,
    Guid employeeId,
    int totalLeaveDays)
    {
        var year = DateTime.UtcNow.Year;

        const string sql = @"
        UPDATE attendance.leaveentitlements
        SET 
            useddays = useddays + @TotalLeaveDays,
            remainingdays = remainingdays - @TotalLeaveDays,
            updatedat = CURRENT_TIMESTAMP
        WHERE employeeid = @EmployeeId
          AND leavetypeid = @LeaveTypeId
          AND year = @Year
          AND remainingdays >= @TotalLeaveDays
        RETURNING entitlementid;
    ";

        var result = await ExecuteScalarAsync<Guid?>(
            sql,
            new
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Year = year,
                TotalLeaveDays = totalLeaveDays
            });

        if (result == null)
            throw new InvalidOperationException("Insufficient leave balance or entitlement not found");

        return true;
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

    public async Task<PushEmailLeaveRequest> GetEmployeeDetails(Guid requestId)
    {
        try
        {
            return await QuerySingleOrDefaultAsync<PushEmailLeaveRequest>(
             @"SELECT e.email,
                     e.firstname,
                     e.lastname,
                     CONCAT(e.firstname, ' ', e.lastname) AS EmployeeName,
                     lt.typename,
                     lr.startdate,
                     lr.enddate
              FROM employee.employees e
              JOIN attendance.leaverequests lr 
                  ON lr.employeeid = e.employeeid
              JOIN attendance.leavetypes lt
                  ON lt.leavetypeid = lr.leavetypeid
              WHERE lr.requestid = @RequestId",
             new { RequestId = requestId });
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId, Guid organizationId, string? status = null, int page = 1, int pageSize = 50)
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
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        INNER JOIN attendance.leavetypes lt ON lr.leavetypeid = lt.leavetypeid
        LEFT JOIN employee.employees a ON lr.approvedby = a.employeeid
        WHERE ed.reportingmanagerid = @employeeId   -- ✅ Only employees under this manager
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
                StartDate = r.startdate ?? DateTime.MinValue,
                EndDate = r.enddate ?? DateTime.MinValue,
                DaysRequested = r.daysrequested ?? 0,
                Reason = r.reason ?? string.Empty,
                Status = r.status ?? string.Empty,
                SubmittedAt = r.submittedat ?? DateTime.MinValue,
                ApproverName = r.approver_name ?? string.Empty,
                ProfilePictureUrl = r.profilepictureurl ?? string.Empty,
                ApprovedAt = r.approvedat
            }).ToList();

            // Total count with manager-filter
            var countSql = @"
        SELECT COUNT(*)
        FROM attendance.leaverequests lr
        INNER JOIN employee.employees e ON lr.employeeid = e.employeeid
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        WHERE ed.reportingmanagerid = @employeeId
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
        catch
        {
            return null;
        }
    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsByOrganizationAsync(Guid organizationId, string? status = null, int page = 1, int pageSize = 50)
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
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        INNER JOIN attendance.leavetypes lt ON lr.leavetypeid = lt.leavetypeid
        LEFT JOIN employee.employees a ON lr.approvedby = a.employeeid
        WHERE e.organizationid = @organizationId
          AND (@status IS NULL OR lr.status = @status)
        ORDER BY lr.submittedat DESC
        LIMIT @pageSize OFFSET @offset";

            var results = await QueryAsync<dynamic>(sql, new
            {
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
                StartDate = r.startdate ?? DateTime.MinValue,
                EndDate = r.enddate ?? DateTime.MinValue,
                DaysRequested = r.daysrequested ?? 0,
                Reason = r.reason ?? string.Empty,
                Status = r.status ?? string.Empty,
                SubmittedAt = r.submittedat ?? DateTime.MinValue,
                ApproverName = r.approver_name ?? string.Empty,
                ProfilePictureUrl = r.profilepictureurl ?? string.Empty,
                ApprovedAt = r.approvedat
            }).ToList();

            // Total count by organization
            var countSql = @"
        SELECT COUNT(*)
        FROM attendance.leaverequests lr
        INNER JOIN employee.employees e ON lr.employeeid = e.employeeid
        WHERE e.organizationid = @organizationId
          AND (@status IS NULL OR lr.status = @status)";

            var totalCount = await ExecuteScalarAsync<int>(countSql, new { organizationId, status });

            return new PagedResult<LeaveRequestDto>
            {
                Data = leaveRequests,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT leavetypeid, typename, description, maxdaysperyear, 
                   ispaid,ishalfpaid, carryforwardallowed, maxcarryforwarddays, color, isactive
            FROM attendance.leavetypes 
            WHERE organizationid = @organizationId 
            ORDER BY typename";

        var results = await QueryAsync<LeaveTypeDto>(sql, new { organizationId });
        return results.ToList();
    }
    public async Task<List<LeaveTypeDto>> GetLeaveTypesforRequestAsync(Guid organizationId)
    {
        var sql = @"
            SELECT leavetypeid, typename, description, maxdaysperyear, 
                   ispaid, carryforwardallowed, maxcarryforwarddays, color, isactive
            FROM attendance.leavetypes 
            WHERE organizationid = @organizationId AND isactive = true
            ORDER BY typename";

        var results = await QueryAsync<LeaveTypeDto>(sql, new { organizationId });
        return results.ToList();
    }
    public async Task<List<LeaveTypeDto>> GetLeaveTypeIdByNameAsync(string LeaveType , Guid organizationId)
    {
        var sql = @"
            SELECT leavetypeid
            FROM attendance.leavetypes 
            WHERE organizationid = @organizationId AND typename = @LeaveType AND isactive = true
            ORDER BY typename";

        var results = await QueryAsync<LeaveTypeDto>(sql, new { organizationId,LeaveType });
        return results.ToList();
    }
    public async Task<string> GetLeaveTypeNameByIdAsync(Guid LeaveTypeid, Guid organizationId)
    {
        var sql = @"
            SELECT typename
            FROM attendance.leavetypes 
            WHERE organizationid = @organizationId AND leavetypeid = @LeaveTypeid AND isactive = true
            ORDER BY typename";

        var results = await QuerySingleOrDefaultAsync<string>(sql, new { organizationId, LeaveTypeid });
        return results;
    }
    public async Task<bool> CreateLeaveTypeAsync(Guid organizationId, LeaveTypeDto dto)
    {
        var sqlInsert = @"
        INSERT INTO attendance.leavetypes 
            (leavetypeid, organizationid, typename, description, maxdaysperyear, 
             ispaid,ishalfpaid,carryforwardallowed, maxcarryforwarddays, color, isactive, createdat)
        VALUES
            (@LeaveTypeId, @OrganizationId, @TypeName, @Description, @MaxDaysPerYear, 
             @IsPaid,@IsHalfPaid, @CarryForwardAllowed, @maxcarryforwarddays, @Color, @IsActive, NOW());
    ";

        var sqlInitEntitlements = @"
        SELECT attendance.fn_initialize_leave_entitlements_for_new_leavetype(@LeaveTypeId, @OrganizationId);
    ";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

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
                        dto.IsHalfPaid,
                        dto.CarryForwardAllowed,
                        dto.MaxCarryForwardDays,
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

    public async Task<bool> EditLeaveTypeAsync(Guid organizationId, LeaveTypeDto dto)
    {
        var sqlUpdate = @"
UPDATE attendance.leavetypes
SET
    typename = @TypeName,
    description = @Description,
    maxdaysperyear = @MaxDaysPerYear,
    ispaid = @IsPaid,
    carryforwardallowed = @CarryForwardAllowed,
    maxcarryforwarddays = @MaxCarryForwardDays,
    color = @Color,
    isactive = @IsActive,
    createdat = createdat 
WHERE leavetypeid = @LeaveTypeId
  AND organizationid = @OrganizationId;
";

        // Optional but recommended: create entitlement rows for employees who joined after this leave type was created
        var sqlEnsureEntitlements = @"
SELECT attendance.fn_initialize_leave_entitlements_for_new_leavetype(@LeaveTypeId, @OrganizationId);
";

        // This will update balances correctly based on latest leave type settings
        var sqlSyncEntitlements = @"
SELECT attendance.fn_sync_leave_entitlements_for_leavetype(@LeaveTypeId, @OrganizationId);
";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();


        using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var rowsAffected = await connection.ExecuteAsync(sqlUpdate, new
            {
                dto.LeaveTypeId,
                OrganizationId = organizationId,
                dto.TypeName,
                dto.Description,
                dto.MaxDaysPerYear,
                dto.IsPaid,
                dto.CarryForwardAllowed,
                dto.MaxCarryForwardDays,
                dto.Color,
                dto.IsActive
            }, transaction);

            // If no row updated: either wrong id or not in this org
            if (rowsAffected <= 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // Keep system consistent
            await connection.ExecuteAsync(sqlEnsureEntitlements, new
            {
                dto.LeaveTypeId,
                OrganizationId = organizationId
            }, transaction);

            await connection.ExecuteAsync(sqlSyncEntitlements, new
            {
                dto.LeaveTypeId,
                OrganizationId = organizationId
            }, transaction);

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error updating LeaveType: {ex.Message}");
            return false;
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
                   COALESCE(le.remainingdays, 0) as remaining_days,
                   COALESCE(le.carryforwarddays, 0) as carryforwarddays                
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
    //EdiT Elementals for edit leave request

    public async Task<LeaveEntitlementDto> EditingLeaveElementals(Guid empId, Guid leaveTypeid, decimal Days)
    {
        try
        {
            int Year = DateTime.UtcNow.Year;


            var selectSql = @"
                SELECT 
                    entitlementid AS EntitlementId,
                    totaldays AS TotalDays,
                    useddays AS UsedDays,
                    remainingdays AS RemainingDays,
                    leavetypeid AS LeaveTypeId,
                    employeeid AS EmployeeId,
                    year AS Year,
                    carryforwarddays AS CarryForwardDays
                FROM attendance.leaveentitlements
                WHERE employeeid=@empId AND leavetypeid=@leaveTypeid AND year=@year;
                ";

            var result = await QuerySingleOrDefaultAsync<LeaveEntitlementDto>(
                selectSql, new { empId, leaveTypeid, Year });

            if (result == null) return null;

            var updateSql = @"
                UPDATE attendance.leaveentitlements
                SET remainingdays = remainingdays - @Days,
                useddays=useddays+@Days
                WHERE entitlementid = @EntitlementId;
                ";

            await ExecuteAsync(updateSql, new { Days, result.EntitlementId });

            //result.RemainingDays += days;
            return result;


        }
        catch (Exception ex)
        {
            return null;

        }
    }



    public async Task<LeaveEntitlementDto> EditingLeaveElementalsFordaysLess(Guid empId, Guid leaveTypeid, decimal Days)
    {
        try
        {
            int Year = DateTime.UtcNow.Year;


            var selectSql = @"
                    SELECT 
                        entitlementid AS EntitlementId,
                        totaldays AS TotalDays,
                        useddays AS UsedDays,
                        remainingdays AS RemainingDays,
                        leavetypeid AS LeaveTypeId,
                        employeeid AS EmployeeId,
                        year AS Year,
                        carryforwarddays AS CarryForwardDays
                    FROM attendance.leaveentitlements
                    WHERE employeeid=@empId AND leavetypeid=@leaveTypeid AND year=@year;
                    ";

            var result = await QuerySingleOrDefaultAsync<LeaveEntitlementDto>(
                selectSql, new { empId, leaveTypeid, Year });

            if (result == null) return null;

            var updateSql = @"
            UPDATE attendance.leaveentitlements
            SET remainingdays = remainingdays + @Days,
            useddays=useddays-@Days
            WHERE entitlementid = @EntitlementId;
            ";

            await ExecuteAsync(updateSql, new { Days, result.EntitlementId });

            //result.RemainingDays += days;
            return result;


        }
        catch (Exception ex)
        {
            return null;

        }
    }

    public async Task<bool> UpdateLeaveRequestAsync(Guid requestId, Guid leaveTypeId, DateTime startDate, DateTime endDate, decimal daysRequested, string? reason = null)
    {
        try
        {
            var sql = @"
                UPDATE attendance.leaverequests
                SET leavetypeid = @leaveTypeId,
                    startdate = @startDate,
                    enddate = @endDate,
                    daysrequested = @daysRequested,
                    reason = @reason,
                    updatedat = CURRENT_TIMESTAMP
                WHERE requestid = @requestId
                  AND status = 'pending'";

            var rowsAffected = await ExecuteAsync(sql, new
            {
                requestId,
                leaveTypeId,
                startDate,
                endDate,
                daysRequested,
                reason
            });

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> CancelLeaveRequestAsync(Guid requestId)
    {
        try
        {
            var sql = @"
                UPDATE attendance.leaverequests
                SET status = 'cancelled',
                    updatedat = CURRENT_TIMESTAMP
                WHERE requestid = @requestId
                  AND status = 'pending'";

            var rowsAffected = await ExecuteAsync(sql, new { requestId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
    {
        try
        {
            //var employeeid = await GetEmpId(employeeId);

            // Normalize dates to date-only (remove time component) for proper comparison
            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date;

            // Debug logging
            //Console.WriteLine($"Checking overlap for EmployeeId: {employeeId}, StartDate: {normalizedStartDate:yyyy-MM-dd}, EndDate: {normalizedEndDate:yyyy-MM-dd}, ExcludeRequestId: {excludeRequestId}");

            var sql = @"
SELECT COUNT(*) > 0
FROM attendance.leaverequests
WHERE employeeid = @employeeid
  AND status IN ('approved', 'pending')
  AND (@excludeRequestId IS NULL OR requestid != @excludeRequestId)
  AND NOT (enddate < @normalizedStartDate OR startdate > @normalizedEndDate)";

            var result = await ExecuteScalarAsync<bool>(sql, new {
                employeeId,
                normalizedStartDate,
                normalizedEndDate,
                excludeRequestId
            });

            //Console.WriteLine($"Overlap check result: {result} for EmployeeId: {employeeId}");
            return result;
        }
        catch(Exception ex)
        {
            //    Console.WriteLine($"Error checking leave overlap: {ex.Message}");
            //    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception("An error occurred while checking leave overlap.");
        }
    }

    public async Task<List<LeaveRequest>> GetLeaveRequestsByEmployeeId(Guid employeeId)
    {
        try
        {
            var sql = @"
        SELECT lr.*, lt.typename
        FROM attendance.leaverequests lr
        JOIN attendance.leavetypes lt 
            ON lt.leavetypeid = lr.leavetypeid
        WHERE lr.employeeid = @employeeId";

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

    public async Task<List<LeaveCalendarDto>> GetApprovedLeaveRequestsAsync(Guid employeeId, DateTime startDate, DateTime endDate)
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
          AND lr.status = 'approved'
          AND ((lr.startdate >= @startDate AND lr.startdate <= @endDate)
               OR (lr.enddate >= @startDate AND lr.enddate <= @endDate)
               OR (lr.startdate <= @startDate AND lr.enddate >= @endDate))
        ORDER BY lr.startdate;";

        var results = await QueryAsync<LeaveCalendarDto>(sql, new { employeeId, startDate, endDate });
        return results.ToList();
    }

    public async Task<PagedResult<EmployeeLeaveBalanceDto>> GetTeamRemainingLeavesAsync(
      Guid managerEmployeeId,
      Guid organizationId,
      int year,
      string? employeeName = null,
      Guid? leaveTypeId = null,
      int page = 1,
      int pageSize = 50,
      string? sortBy = null,
      string? sortOrder = "asc")
    {
        var parameters = new DynamicParameters();
        parameters.Add("managerEmployeeId", managerEmployeeId);
        parameters.Add("organizationId", organizationId);
        parameters.Add("year", year);

        var employeeFilter = string.IsNullOrWhiteSpace(employeeName)
            ? ""
            : "AND LOWER(e.firstname || ' ' || e.lastname) LIKE LOWER(@employeeName)";

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            parameters.Add("employeeName", $"%{employeeName}%");
        }

        var leaveTypeFilter = leaveTypeId.HasValue ? "AND lt.leavetypeid = @leaveTypeId" : "";
        if (leaveTypeId.HasValue)
        {
            parameters.Add("leaveTypeId", leaveTypeId.Value);
        }

        // Build ORDER BY clause for employee sorting
        var employeeOrderBy = "ORDER BY e.firstname || ' ' || e.lastname";
        if (!string.IsNullOrWhiteSpace(sortBy) && sortBy.ToLower() == "employeename")
        {
            employeeOrderBy = $"ORDER BY e.firstname || ' ' || e.lastname {(sortOrder?.ToLower() == "desc" ? "DESC" : "ASC")}";
        }

        // Count distinct employees
        var countSql = $@"
         SELECT COUNT(DISTINCT e.employeeid)
         FROM employee.employees e
         INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
         WHERE ed.reportingmanagerid = @managerEmployeeId
           AND ed.iscurrent = true
           AND e.organizationid = @organizationId
           AND e.status = 'active'
           {employeeFilter}";

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);

        // Get paginated employees
        var offset = (page - 1) * pageSize;
        parameters.Add("offset", offset);
        parameters.Add("pageSize", pageSize);

        var sql = $@"
         WITH TeamEmployees AS (
             SELECT DISTINCT e.employeeid, 
                    e.firstname || ' ' || e.lastname AS employeename
             FROM employee.employees e
             INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
             WHERE ed.reportingmanagerid = @managerEmployeeId
               AND ed.iscurrent = true
               AND e.organizationid = @organizationId
               AND e.status = 'active'
               {employeeFilter}
             {employeeOrderBy}
             LIMIT @pageSize OFFSET @offset
         ),
         LeaveTypes AS (
             SELECT leavetypeid, 
                    typename
             FROM attendance.leavetypes
             WHERE organizationid = @organizationId
               AND isactive = true
               {leaveTypeFilter}
         ),
         LeaveEntitlements AS (
             SELECT le.employeeid,
                    le.leavetypeid,
                    le.totaldays,
                    le.useddays,
                    le.remainingdays,
                    le.carryforwarddays
             FROM attendance.leaveentitlements le
             INNER JOIN TeamEmployees te ON le.employeeid = te.employeeid
             WHERE le.year = @year
         ),
         UsedLeaves AS (
             SELECT lr.employeeid,
                    lr.leavetypeid,
                    COALESCE(SUM(lr.daysrequested), 0) AS useddays
             FROM attendance.leaverequests lr
             INNER JOIN TeamEmployees te ON lr.employeeid = te.employeeid
             WHERE lr.status = 'approved'
               AND EXTRACT(YEAR FROM lr.startdate) = @year
             GROUP BY lr.employeeid, lr.leavetypeid
         )
         SELECT te.employeeid AS EmployeeId,
                te.employeename AS EmployeeName,
                lt.leavetypeid AS LeaveTypeId,
                lt.typename AS LeaveTypeName,
                COALESCE(le.totaldays, 0) AS TotalDays,
                COALESCE(le.useddays, 0) + COALESCE(ul.useddays, 0) AS UsedDays,
                COALESCE(le.remainingdays, 0) AS RemainingDays
         FROM TeamEmployees te
         CROSS JOIN LeaveTypes lt
         LEFT JOIN LeaveEntitlements le ON te.employeeid = le.employeeid 
                                       AND lt.leavetypeid = le.leavetypeid
         LEFT JOIN UsedLeaves ul ON te.employeeid = ul.employeeid 
                                AND lt.leavetypeid = ul.leavetypeid
         ORDER BY te.employeename, lt.typename";

        var results = await QueryAsync<EmployeeLeaveBalanceDto>(sql, parameters);

        return new PagedResult<EmployeeLeaveBalanceDto>
        {
            Data = results.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<EmployeeLeaveBalanceDto>> GetTeamRemainingLeavesByOrganizationAsync(
      Guid organizationId,
      int year,
      string? employeeName = null,
      Guid? leaveTypeId = null,
      int page = 1,
      int pageSize = 50,
      string? sortBy = null,
      string? sortOrder = "asc")
    {
        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("year", year);

        var employeeFilter = string.IsNullOrWhiteSpace(employeeName)
            ? ""
            : "AND LOWER(e.firstname || ' ' || e.lastname) LIKE LOWER(@employeeName)";

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            parameters.Add("employeeName", $"%{employeeName}%");
        }

        var leaveTypeFilter = leaveTypeId.HasValue ? "AND lt.leavetypeid = @leaveTypeId" : "";
        if (leaveTypeId.HasValue)
        {
            parameters.Add("leaveTypeId", leaveTypeId.Value);
        }

        // Build ORDER BY clause for employee sorting
        var employeeOrderBy = "ORDER BY e.firstname || ' ' || e.lastname";
        if (!string.IsNullOrWhiteSpace(sortBy) && sortBy.ToLower() == "employeename")
        {
            employeeOrderBy = $"ORDER BY e.firstname || ' ' || e.lastname {(sortOrder?.ToLower() == "desc" ? "DESC" : "ASC")}";
        }

        // Count distinct employees
        var countSql = $@"
         SELECT COUNT(DISTINCT e.employeeid)
         FROM employee.employees e
         INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
         WHERE ed.iscurrent = true
           AND e.organizationid = @organizationId
           AND e.status = 'active'
           {employeeFilter}";

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);

        // Get paginated employees
        var offset = (page - 1) * pageSize;
        parameters.Add("offset", offset);
        parameters.Add("pageSize", pageSize);

        var sql = $@"
         WITH AllEmployees AS (
             SELECT DISTINCT e.employeeid, 
                    e.firstname || ' ' || e.lastname AS employeename
             FROM employee.employees e
             INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
             WHERE ed.iscurrent = true
               AND e.organizationid = @organizationId
               AND e.status = 'active'
               {employeeFilter}
             {employeeOrderBy}
             LIMIT @pageSize OFFSET @offset
         ),
         LeaveTypes AS (
             SELECT leavetypeid, 
                    typename
             FROM attendance.leavetypes
             WHERE organizationid = @organizationId
               AND isactive = true
               {leaveTypeFilter}
         ),
         LeaveEntitlements AS (
             SELECT le.employeeid,
                    le.leavetypeid,
                    le.totaldays,
                    le.useddays,
                    le.remainingdays,
                    le.carryforwarddays
             FROM attendance.leaveentitlements le
             INNER JOIN AllEmployees ae ON le.employeeid = ae.employeeid
             WHERE le.year = @year
         ),
         UsedLeaves AS (
             SELECT lr.employeeid,
                    lr.leavetypeid,
                    COALESCE(SUM(lr.daysrequested), 0) AS useddays
             FROM attendance.leaverequests lr
             INNER JOIN AllEmployees ae ON lr.employeeid = ae.employeeid
             WHERE lr.status = 'approved'
               AND EXTRACT(YEAR FROM lr.startdate) = @year
             GROUP BY lr.employeeid, lr.leavetypeid
         )
         SELECT ae.employeeid AS EmployeeId,
                ae.employeename AS EmployeeName,
                lt.leavetypeid AS LeaveTypeId,
                lt.typename AS LeaveTypeName,
                COALESCE(le.totaldays, 0) AS TotalDays,
                COALESCE(le.useddays, 0) + COALESCE(ul.useddays, 0) AS UsedDays,
                COALESCE(le.remainingdays, 0) AS RemainingDays
         FROM AllEmployees ae
         CROSS JOIN LeaveTypes lt
         LEFT JOIN LeaveEntitlements le ON ae.employeeid = le.employeeid 
                                       AND lt.leavetypeid = le.leavetypeid
         LEFT JOIN UsedLeaves ul ON ae.employeeid = ul.employeeid 
                                AND lt.leavetypeid = ul.leavetypeid
         ORDER BY ae.employeename, lt.typename";

        var results = await QueryAsync<EmployeeLeaveBalanceDto>(sql, parameters);

        return new PagedResult<EmployeeLeaveBalanceDto>
        {
            Data = results.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<string?> GetUserRoleNameAsync(Guid userId)
    {
        var sql = @"
            SELECT r.rolename
            FROM auth.users u
            INNER JOIN auth.roles r ON u.roleid = r.roleid
            WHERE u.userid = @userId";

        var result = await QuerySingleOrDefaultAsync<string>(sql, new { userId });
        return result;
    }


}
