using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;
using Npgsql;
using System.Data;

namespace HRMS.API.Repositories;

public class AttendanceRepository : BaseRepository, IAttendanceRepository
{
    private readonly ILogger<AttendanceRepository> _logger;

    public AttendanceRepository(IDatabaseConnectionFactory connectionFactory, ILogger<AttendanceRepository> logger) : base(connectionFactory)
    {
        _logger = logger;
    }

    public async Task<bool> ClockInOutAsync(Guid employeeId, string action, DateTime timestamp, Dictionary<string, object>? location = null)
    {
        try
        {
        var result = await ExecuteScalarAsync<bool>(
            "SELECT attendance.fn_clock_in_out(@employeeId, @action, @timestamp, @location::jsonb)",
            new 
            { 
                employeeId, 
                action, 
                timestamp, 
                location = System.Text.Json.JsonSerializer.Serialize(location ?? new())
            });

        return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Check in faild", ex.InnerException);
        }
    }

    public async Task<Attendance?> GetTodayAttendanceAsync(Guid employeeId)
    {
        try
        {
            var sql = @"
                SELECT attendanceid, employeeid, workdate, checkintime, checkouttime,
                       breakstarttime, breakendtime, totalhours, overtimehours, breakhours,
                       status, location, notes, approvedby, approvedat, createdat, updatedat
                FROM attendance.attendance 
                WHERE employeeid = @employeeId AND workdate = CURRENT_DATE";

            return await QuerySingleOrDefaultAsync<Attendance>(sql, new { employeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's attendance for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(
        Guid? employeeId = null, Guid? organizationId = null, DateTime? startDate = null, 
        DateTime? endDate = null, int page = 1, int pageSize = 100)
    {
        try
        {
            var results = await QueryAsync<dynamic>(
                "SELECT * FROM attendance.fn_get_attendance(@employeeId, @organizationId, @startDate, @endDate, @limit, @offset)",
                new 
                {
                    employeeId, 
                    organizationId, 
                    startDate, 
                    endDate, 
                    limit = pageSize, 
                    offset = (page - 1) * pageSize 
                });

            var attendanceList = results.Select(r => new AttendanceDto
            {
                AttendanceId = r.attendanceid,
                EmployeeId = r.employeeid,
                EmployeeName = r.employeename,
                WorkDate = r.workdate,
                CheckInTime = r.checkintime,
                CheckOutTime = r.checkouttime,
                TotalHours = r.totalhours,
                OvertimeHours = r.overtimehours,
                Status = r.status
            }).ToList();

            var totalCount = results.FirstOrDefault()?.total_count ?? 0;

            return new PagedResult<AttendanceDto>
            {
                Data = attendanceList,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance records. EmployeeId: {EmployeeId}, OrganizationId: {OrganizationId}", employeeId, organizationId);
            throw;
        }
    }
    

    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sql = @"
                SELECT 
                    COUNT(*) as totalworkdays,
                    COUNT(CASE WHEN status = 'present' THEN 1 END) as presentdays,
                    COUNT(CASE WHEN status = 'absent' THEN 1 END) as absentdays,
                    COUNT(CASE WHEN status = 'late' THEN 1 END) as latedays,
                    COALESCE(SUM(totalhours), 0) as totalhours,
                    COALESCE(SUM(overtimehours), 0) as overtimehours
                FROM attendance.attendance
                WHERE employeeid = @employeeId";

            var result = await QuerySingleAsync<AttendanceSummaryDto>(sql, new { employeeId, startDate, endDate });

            return new AttendanceSummaryDto
            {
                TotalWorkDays = result.TotalWorkDays,
                PresentDays = result.PresentDays,
                AbsentDays = result.AbsentDays,
                LateDays = result.LateDays,
                TotalHours = result.TotalHours,
                OvertimeHours = result.OvertimeHours,
                AverageHoursPerDay = result.TotalWorkDays > 0 ? result.TotalHours / result.TotalWorkDays : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance summary for employee {EmployeeId} from {StartDate} to {EndDate}", employeeId, startDate, endDate);
            throw;
        }
    }
    

    public async Task<bool> UpdateAttendanceAsync(Guid attendanceId, Attendance attendance)
    {
        try
        {
            var sql = @"
                UPDATE attendance.attendance 
                SET checkintime = @checkInTime, checkouttime = @checkOutTime,
                    totalhours = @totalHours, status = @status, notes = @notes,
                    updatedat = CURRENT_TIMESTAMP
                WHERE attendanceid = @attendanceId";

            var affected = await ExecuteAsync(sql, new
            {
                attendanceId,
                checkInTime = attendance.CheckInTime,
                checkOutTime = attendance.CheckOutTime,
                totalHours = attendance.TotalHours,
                status = attendance.Status,
                notes = attendance.Notes
            });

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance record {AttendanceId}", attendanceId);
            throw;
        }
    }

    public async Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date)
    {
        try
        {
            var sql = @"
                SELECT a.attendanceid, a.employeeid, a.workdate, a.checkintime, 
                       a.checkouttime, a.totalhours, a.status,
                       CONCAT(e.firstname, ' ', e.lastname) as employeename
                FROM attendance.attendance a
                INNER JOIN employee.employees e ON a.employeeid = e.employeeid
                INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
                WHERE ed.departmentid = @departmentId AND a.workdate = @date AND ed.iscurrent = true
                ORDER BY e.firstname, e.lastname";

            var results = await QueryAsync<dynamic>(sql, new { departmentId, date });

            return results.Select(r => new AttendanceDto
            {
                AttendanceId = r.attendance_id,
                EmployeeId = r.employee_id,
                EmployeeName = r.employee_name,
                WorkDate = r.work_date,
                CheckInTime = r.check_in_time,
                CheckOutTime = r.check_out_time,
                TotalHours = r.total_hours,
                Status = r.status
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department attendance for department {DepartmentId} on {Date}", departmentId, date);
            throw;
        }
    }

    // New missing methods implementation
    public async Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId)
    {
        try
        {
            var sql = @"
                SELECT attendanceid, employeeid, workdate, checkintime, checkouttime,
                       totalhours, overtimehours, status, notes, createdat
                FROM attendance.attendance 
                WHERE attendanceid = @attendanceId";

            return await QuerySingleOrDefaultAsync<Attendance>(sql, new { attendanceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance record by ID {AttendanceId}", attendanceId);
            throw;
        }
    }


    //Chnages here

    public async Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request)
    {
        try
        {
            var createdAt = DateTime.Now;
            var sql = @"
            INSERT INTO attendance.shifts
                (organizationid, shiftname, starttime, endtime, breakduration, daysofweek, timezone,createdat)
            VALUES
                (@OrganizationId, @ShiftName, @StartTime, @EndTime, @BreakDuration, @DaysofWeek, @Timezone,@createdAt);
        ";

            var parameters = new
            {
                createdAt = createdAt,
                OrganizationId = organizationId,
                ShiftName = request.ShiftName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                BreakDuration = request.BreakDuration ?? 0,
                DaysofWeek = request.DaysofWeek ?? new int[] { 1, 2, 3, 4, 5 },
                Timezone = request.Timezone ?? "UTC"
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request)
    {
        try
        {
            var sql = @"
            UPDATE employee.employmentdetails
            SET shiftid = @ShiftId
            WHERE employeeid = @EmployeeId
          
        ";

            var parameters = new
            {
                ShiftId = request.ShiftId,
                EmployeeId = request.EmployeeId,
                //OrganizationId = organizationId
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning shift {ShiftId} to employee {EmployeeId} for organization {OrganizationId}",
                request.ShiftId, request.EmployeeId, organizationId);
            throw;
        }

    }

    //Get by shift

    public async Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId)
    {
        try
        {


            var sql = @"
         SELECT 
        e.employeeid,
        e.firstname,
        e.lastname,
        e.email,
        ed.shiftid,
        d.departmentname  
         FROM employee.employees e
         INNER JOIN employee.employmentdetails ed 
         ON e.employeeid = ed.employeeid
         LEFT JOIN employee.departments d
        ON ed.departmentid = d.departmentid   -- join to get department name
          WHERE e.organizationid = @OrganizationId
           AND ed.shiftid = @ShiftId;
            ";

            var parameters = new { OrganizationId = organizationId, ShiftId = shiftId };
            var result = await QueryAsync<EmployeeShiftDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employees for shift {ShiftId} in organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }

    public async Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto)
    {
        try
        {
            var employeeId = await GetEmpId(requestDto.EmployeeId);



            var sql = @"
            INSERT INTO attendance.shiftswaprequests
            (employeeid, currentshiftid, requestedshiftid, reason,organizationid)
            VALUES
            (@EmployeeId, @CurrentShiftId, @RequestedShiftId, @Reason,@organizationId);
        ";

            var parameters = new
            {
                EmployeeId = employeeId,
                CurrentShiftId = requestDto.CurrentShiftId,
                RequestedShiftId = requestDto.RequestedShiftId,
                Reason = requestDto.Reason,
                organizationId = organizationId
            };

            var rows = await ExecuteAsync(sql, parameters);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift swap request for employee {EmployeeId}", requestDto.EmployeeId);
            return false;
        }
    }


    public async Task<bool> RejectShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto)
    {
        try
        {
            var status = dto.IsApproved ? "approved" : "rejected";

            var sql = @"
            UPDATE attendance.shiftswaprequests
            SET status = @Status,
                approvedby = @ApprovedBy,
                approvedat = @ApprovedAt,
                rejectionreason = @RejectionReason,
                updatedat = @UpdatedAt
            WHERE requestid = @RequestId;
        ";

            var parameters = new
            {
                Status = status,
                ApprovedBy = dto.ApprovedBy,
                ApprovedAt = DateTime.UtcNow,
                RejectionReason = dto.RejectionReason,
                UpdatedAt = DateTime.Now,
                RequestId = dto.RequestId
            };

            var rows = await ExecuteAsync(sql, parameters);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving/rejecting shift swap request {RequestId}", dto.RequestId);
            return false;
        }
    }


    //Accept Shift Swap Request

    public async Task<bool> AcceptswapRequestAsync(ApproveShiftSwapRequestDto dto)
    {
        try
        {
            var status = dto.IsApproved ? "approved" : "rejected";

            var sql = @"
            WITH updated_swap AS (
                UPDATE attendance.shiftswaprequests
                SET status = 'approved',
                    approvedby = @ApprovedBy,
                    approvedat = @ApprovedAt,
                    updatedat = NOW()
                WHERE requestid = @RequestId
                RETURNING employeeid, requestedshiftid
            )
            UPDATE employee.employmentdetails ed
            SET shiftid = u.requestedshiftid
            FROM updated_swap u
            WHERE ed.employeeid = u.employeeid;
        ";

            var parameters = new
            {
                Status = status,
                ApprovedBy = dto.ApprovedBy,
                ApprovedAt = DateTime.UtcNow,
                RejectionReason = dto.RejectionReason,
                UpdatedAt = DateTime.Now,
                RequestId = dto.RequestId
            };

            var rows = await ExecuteAsync(sql, parameters);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving/rejecting shift swap request {RequestId}", dto.RequestId);
            return false;
        }
    }




    public async Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
    SELECT 
        ss.requestid,
        ss.employeeid,
        e.firstname || ' ' || e.lastname AS EmployeeName,
        ss.currentshiftid,
        cs.shiftname AS CurrentShiftName,
        ss.requestedshiftid,
        rs.shiftname AS RequestedShiftName,
        ss.reason,
        ss.status
    FROM attendance.shiftswaprequests ss
    INNER JOIN employee.employees e 
        ON ss.employeeid = e.employeeid
    LEFT JOIN attendance.shifts cs
        ON ss.currentshiftid = cs.shiftid
    LEFT JOIN attendance.shifts rs
        ON ss.requestedshiftid = rs.shiftid
    WHERE ss.organizationid = @OrganizationId
      AND ss.status = 'pending';
";

            var parameters = new { OrganizationId = organizationId };
            var result = await QueryAsync<PendingShiftSwapDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending shift swap requests for organization {OrganizationId}", organizationId);
            throw;
        }
    }


    public async Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeIdd)
    {
        try
        {
            var employeeId = await GetEmpId(employeeIdd);

            var sql = @"
       SELECT 
    s.shiftid AS ""ShiftId"",
    s.organizationid AS ""OrganizationId"",
    s.shiftname AS ""ShiftName"",
    s.starttime AS ""StartTime"",
    s.endtime AS ""EndTime"",
    s.breakduration AS ""BreakDuration"",
    s.daysofweek AS ""DaysOfWeek""
FROM employee.employmentdetails ed
INNER JOIN attendance.shifts s
    ON ed.shiftid = s.shiftid
WHERE ed.employeeid = @EmployeeId
  AND ed.iscurrent = TRUE;

    ";

            var parameters = new { EmployeeId = employeeId };
            var result = await QuerySingleOrDefaultAsync<ShiftDto>(sql, parameters);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift for employee {EmployeeId}", employeeIdd);
            throw;
        }
    }


    //Get employee all request

    public async Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeIdd)
    {
        try
        {


            var employeeId = await GetEmpId(employeeIdd);

            var sql = @"
            SELECT 
                ss.requestid,
                ss.employeeid,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                ss.currentshiftid,
                cs.shiftname AS CurrentShiftName,
                ss.requestedshiftid,
                rs.shiftname AS RequestedShiftName,
                ss.status
            FROM attendance.shiftswaprequests ss
            INNER JOIN employee.employees e ON ss.employeeid = e.employeeid
            LEFT JOIN attendance.shifts cs ON ss.currentshiftid = cs.shiftid
            INNER JOIN attendance.shifts rs ON ss.requestedshiftid = rs.shiftid
            WHERE ss.employeeid = @EmployeeId
            ORDER BY ss.submittedat DESC;
        ";

            var parameters = new { EmployeeId = employeeId };
            var result = await QueryAsync<PendingShiftSwapDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift swap requests for employee {EmployeeId}");
            throw;
        }
    }









    public async Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
            SELECT 
                shiftid AS ShiftId,
                organizationid AS OrganizationId,
                shiftname AS ShiftName,
                starttime AS StartTime,
                endtime AS EndTime,
                breakduration AS BreakDuration,
                daysofweek AS DaysOfWeek,
                timezone AS Timezone,
                isactive AS IsActive,
                createdat AS CreatedAt
            FROM attendance.shifts
            WHERE organizationid = @OrganizationId
            ORDER BY createdat DESC;
        ";

            var parameters = new { OrganizationId = organizationId };
            var result = await QueryAsync<ShiftDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shifts for organization {OrganizationId}", organizationId);
            throw;
        }
    }


    public async Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request)
    {
        try
        {
          
            var sql = @"
            UPDATE attendance.shifts
            SET
                shiftname = @ShiftName,
                starttime = @StartTime,
                endtime = @EndTime,
                breakduration = @BreakDuration,
                daysofweek = @DaysofWeek,
                timezone = @Timezone,
               
            WHERE
                organizationid = @OrganizationId AND shiftid = @ShiftId;
        ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ShiftId = shiftId,
                ShiftName = request.ShiftName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                BreakDuration = request.BreakDuration ?? 0,
                DaysofWeek = request.DaysofWeek ?? new int[] { 1, 2, 3, 4, 5 },
                Timezone = request.Timezone ?? "UTC",
               
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift {ShiftId} for organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }

    public async Task<bool> DeleteShiftAsync(Guid organizationId, Guid shiftId)
    {
        try
        {
            var sql = @"
            DELETE FROM attendance.shifts
            WHERE organizationid = @OrganizationId AND shiftid = @ShiftId;
        ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ShiftId = shiftId
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift {ShiftId} for organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }





    public async Task<Attendance> CreateAttendanceAsync(Attendance attendance)
    {
        try
        {
            var sql = @"
                INSERT INTO attendance.attendance 
                (employeeid, workdate, checkintime, checkouttime, status, notes, totalhours, overtimehours, createdat)
                VALUES (@EmployeeId, @WorkDate, @CheckInTime, @CheckOutTime, @Status, @Notes, @TotalHours, @OvertimeHours, @CreatedAt)
                RETURNING attendanceid, employeeid, workdate, checkintime, checkouttime, totalhours, overtimehours, status, notes, createdat";

            var result = await QuerySingleAsync<Attendance>(sql, attendance);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance record for employee {EmployeeId}", attendance.EmployeeId);
            throw;
        }
    }

    public async Task<bool> DeleteAttendanceAsync(Guid attendanceId)
    {
        try
        {
            var sql = "DELETE FROM attendance.attendance WHERE attendance_id = @attendanceId";
            var rowsAffected = await ExecuteAsync(sql, new { attendanceId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance record {AttendanceId}", attendanceId);
            throw;
        }
    }

    public async Task<List<Attendance>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sql = @"
                SELECT attendanceid, employeeid, workdate, checkintime, checkouttime,
                       totalhours, overtimehours, status, notes, createdat
                FROM attendance.attendance 
                WHERE employeeid = @employeeId 
                AND workdate >= @startDate 
                AND workdate <= @endDate
                ORDER BY workdate DESC";

            var results = await QueryAsync<Attendance>(sql, new { employeeId, startDate, endDate });
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance records for employee {EmployeeId} from {StartDate} to {EndDate}", employeeId, startDate, endDate);
            throw;
        }
    }
    public async Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sql = @"
        -- Total Active Employees Count
        WITH EmployeeCounts AS (
            SELECT COUNT(*) as TotalEmployees
            FROM employee.employees e
            WHERE e.organizationid = @organizationId 
            AND e.status = 'active'
        ),
        
        -- Attendance Summary
        AttendanceSummary AS (
            SELECT 
                COUNT(DISTINCT a.employeeid) as TotalEmployeesWithAttendance,
                COUNT(CASE WHEN a.status = 'present' THEN 1 END) as TotalPresentDays,
                COUNT(CASE WHEN a.status = 'absent' THEN 1 END) as TotalAbsentDays,
                COUNT(CASE WHEN a.status = 'late' THEN 1 END) as TotalLateDays,
                COALESCE(SUM(a.totalhours), 0) as TotalHours,
                COALESCE(SUM(a.overtimehours), 0) as OvertimeHours
            FROM attendance.attendance a
            INNER JOIN employee.employees e ON a.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId
            AND a.workdate BETWEEN @startDate AND @endDate
        )
        
        SELECT 
            @startDate as StartDate,
            @endDate as EndDate,
            (SELECT TotalEmployees FROM EmployeeCounts) as TotalEmployees,
            (SELECT TotalPresentDays FROM AttendanceSummary) as TotalPresentDays,
            (SELECT TotalAbsentDays FROM AttendanceSummary) as TotalAbsentDays,
            (SELECT TotalLateDays FROM AttendanceSummary) as TotalLateDays,
            (SELECT TotalHours FROM AttendanceSummary) as TotalHours,
            (SELECT OvertimeHours FROM AttendanceSummary) as OvertimeHours,
            CASE 
                WHEN (SELECT TotalPresentDays FROM AttendanceSummary) > 0 
                THEN ROUND(
                    (SELECT TotalPresentDays FROM AttendanceSummary) * 100.0 / 
                    NULLIF((SELECT TotalPresentDays FROM AttendanceSummary) + (SELECT TotalAbsentDays FROM AttendanceSummary), 0), 
                    2
                )
                ELSE 0 
            END as AverageAttendanceRate";
            using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
           
            var report = await connection.QueryFirstAsync<AttendanceReportDto>(sql, new
            {
                organizationId,
                startDate,
                endDate
            });

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report from database");
            throw;
        }
    }

    public async Task<List<DepartmentAttendanceStatsDto>> GetDepartmentAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT 
            d.departmentid as DepartmentId,
            d.departmentname as DepartmentName,
            COUNT(DISTINCT e.employeeid) as TotalEmployees,
            COUNT(CASE WHEN a.status = 'present' THEN 1 END) as PresentEmployees,
            COUNT(CASE WHEN a.status = 'absent' THEN 1 END) as AbsentEmployees,
            CASE 
                WHEN COUNT(DISTINCT e.employeeid) > 0 
                THEN ROUND(
                    COUNT(CASE WHEN a.status = 'present' THEN 1 END) * 100.0 / 
                    NULLIF(COUNT(DISTINCT e.employeeid), 0), 
                    2
                )
                ELSE 0 
            END as AttendanceRate
        FROM employee.departments d
        LEFT JOIN employee.employmentdetails ed ON d.departmentid = ed.departmentid AND ed.iscurrent = true
        LEFT JOIN employee.employees e ON ed.employeeid = e.employeeid AND e.status = 'active'
        LEFT JOIN attendance.attendance a ON e.employeeid = a.employeeid 
            AND a.workdate BETWEEN @startDate AND @endDate
        WHERE d.organizationid = @organizationId
        AND d.isactive = true
        GROUP BY d.departmentid, d.departmentname
        ORDER BY d.departmentname";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<DepartmentAttendanceStatsDto>(sql, new
        {
            organizationId,
            startDate,
            endDate
        })).ToList();
    }

    public async Task<List<DailyAttendanceStatsDto>> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT 
            a.workdate as Date,
            COUNT(DISTINCT a.employeeid) as TotalEmployees,
            COUNT(CASE WHEN a.status = 'present' THEN 1 END) as PresentEmployees,
            COUNT(CASE WHEN a.status = 'absent' THEN 1 END) as AbsentEmployees,
            COUNT(CASE WHEN a.status = 'late' THEN 1 END) as LateEmployees,
            CASE 
                WHEN COUNT(DISTINCT a.employeeid) > 0 
                THEN ROUND(
                    COUNT(CASE WHEN a.status = 'present' THEN 1 END) * 100.0 / 
                    COUNT(DISTINCT a.employeeid), 
                    2
                )
                ELSE 0 
            END as AttendancePercentage
        FROM attendance.attendance a
        INNER JOIN employee.employees e ON a.employeeid = e.employeeid
        WHERE e.organizationid = @organizationId
        AND a.workdate BETWEEN @startDate AND @endDate
        GROUP BY a.workdate
        ORDER BY a.workdate";
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<DailyAttendanceStatsDto>(sql, new
        {
            organizationId,
            startDate,
            endDate
        })).ToList();
    }

    public async Task<List<AttendanceDto>> GetAttendanceRecordsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT 
            a.attendanceid as AttendanceId,
            a.employeeid as EmployeeId,
            e.firstname || ' ' || e.lastname as EmployeeName,
            a.workdate as WorkDate,
            a.workdate as Date,
            a.checkintime as CheckInTime,
            a.checkouttime as CheckOutTime,
            a.totalhours as TotalHours,
            a.overtimehours as OvertimeHours,
            a.status as Status,
            a.notes as Notes
        FROM attendance.attendance a
        INNER JOIN employee.employees e ON a.employeeid = e.employeeid
        WHERE e.organizationid = @organizationId
        AND a.workdate BETWEEN @startDate AND @endDate
        ORDER BY a.workdate DESC, e.firstname, e.lastname";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<AttendanceDto>(sql, new
        {
            organizationId,
            startDate,
            endDate
        })).ToList();
    }

    public async Task<Guid> GetEmpId(Guid? employeeId)
    {
        try
        {
            var sql = @"
            SELECT em.employeeid
            FROM employee.employees em
            WHERE ( em.userid = @employeeId)
            LIMIT 1";

            var results = await QueryAsync<Guid>(sql, new { employeeId });

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            return Guid.Empty;
        }
    }
    public async Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date)
    {
        try
        {
            const string sql = @"
WITH emp AS (
    SELECT e.employeeid
    FROM employee.employees e
    WHERE e.organizationid = @organizationId
),
PresentEmployees AS (
    SELECT DISTINCT a.employeeid
    FROM attendance.attendance a
    INNER JOIN emp ON emp.employeeid = a.employeeid
    WHERE a.workdate = @date
      AND LOWER(TRIM(a.status)) = 'present'
),
OnLeaveEmployees AS (
    SELECT DISTINCT lr.employeeid
    FROM attendance.leaverequests lr
    INNER JOIN emp ON emp.employeeid = lr.employeeid
    WHERE LOWER(TRIM(lr.status)) = 'approved'
      AND @date BETWEEN lr.startdate AND lr.enddate
)
SELECT
    @date AS Date,
    COALESCE((SELECT COUNT(*) FROM emp), 0) AS TotalEmployees,
    COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0) AS PresentEmployees,
    COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0) AS OnLeaveEmployees,
    GREATEST(
        COALESCE((SELECT COUNT(*) FROM emp), 0)
        - COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0)
        - COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0),
        0
    ) AS AbsentEmployees,
    ROUND(
        (COALESCE((SELECT COUNT(*)::numeric FROM PresentEmployees), 0)
         / NULLIF(COALESCE((SELECT COUNT(*)::numeric FROM emp),0), 0)
        ) * 100,
    2) AS AttendancePercentage;
";

            var parameters = new DynamicParameters();
            parameters.Add("organizationId", organizationId, DbType.Guid);
            parameters.Add("date", date.Date, DbType.Date);

            return await QuerySingleOrDefaultAsync<DailyAttendanceStatsDto>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting daily attendance stats for organization {OrganizationId} on {Date}",
                organizationId, date);
            throw;
        }
    }
}
