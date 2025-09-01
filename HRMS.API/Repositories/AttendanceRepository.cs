using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;

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
                SELECT attendance_id, employee_id, work_date, check_in_time, check_out_time,
                       break_start_time, break_end_time, total_hours, overtime_hours, break_hours,
                       status, location, notes, approved_by, approved_at, created_at, updated_at
                FROM attendance.attendance 
                WHERE employee_id = @employeeId AND work_date = CURRENT_DATE";

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
                AttendanceId = r.attendance_id,
                EmployeeId = r.employee_id,
                EmployeeName = r.employee_name,
                WorkDate = r.work_date,
                CheckInTime = r.check_in_time,
                CheckOutTime = r.check_out_time,
                TotalHours = r.total_hours,
                OvertimeHours = r.overtime_hours,
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
                    COALESCE(SUM(total_hours), 0) as total_hours,
                    COALESCE(SUM(overtime_hours), 0) as overtimehours
                FROM attendance.attendance
                WHERE employee_id = @employeeId";

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
                SET check_in_time = @checkInTime, check_out_time = @checkOutTime,
                    total_hours = @totalHours, status = @status, notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE attendance_id = @attendanceId";

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
                SELECT a.attendance_id, a.employee_id, a.work_date, a.check_in_time, 
                       a.check_out_time, a.total_hours, a.status,
                       CONCAT(e.first_name, ' ', e.last_name) as employee_name
                FROM attendance.attendance a
                INNER JOIN employee.employees e ON a.employee_id = e.employee_id
                INNER JOIN employee.employment_details ed ON e.employee_id = ed.employee_id
                WHERE ed.department_id = @departmentId AND a.work_date = @date AND ed.is_current = true
                ORDER BY e.first_name, e.last_name";

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
                SELECT attendance_id, employee_id, work_date, check_in_time, check_out_time,
                       total_hours, overtime_hours, status, notes, created_at
                FROM attendance.attendance 
                WHERE attendance_id = @attendanceId";

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
                (employee_id, work_date, check_in_time, check_out_time, status, notes, total_hours, overtime_hours, created_at)
                VALUES (@EmployeeId, @WorkDate, @CheckInTime, @CheckOutTime, @Status, @Notes, @TotalHours, @OvertimeHours, @CreatedAt)
                RETURNING attendance_id, employee_id, work_date, check_in_time, check_out_time, total_hours, overtime_hours, status, notes, created_at";

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
                SELECT attendance_id, employee_id, work_date, check_in_time, check_out_time,
                       total_hours, overtime_hours, status, notes, created_at
                FROM attendance.attendance 
                WHERE employee_id = @employeeId 
                AND work_date >= @startDate 
                AND work_date <= @endDate
                ORDER BY work_date DESC";

            var results = await QueryAsync<Attendance>(sql, new { employeeId, startDate, endDate });
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance records for employee {EmployeeId} from {StartDate} to {EndDate}", employeeId, startDate, endDate);
            throw;
        }
    }
}
