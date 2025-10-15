using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;
using Npgsql;

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
}
