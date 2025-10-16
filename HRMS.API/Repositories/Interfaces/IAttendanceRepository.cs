using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<bool> ClockInOutAsync(Guid employeeId, string action, DateTime timestamp, Dictionary<string, object>? location = null);
    Task<Attendance?> GetTodayAttendanceAsync(Guid employeeId);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(Guid? employeeId = null, Guid? organizationId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 100);
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<bool> UpdateAttendanceAsync(Guid attendanceId, Attendance attendance);
    Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date);
    
    // New missing methods
    Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId);
    Task<Attendance> CreateAttendanceAsync(Attendance attendance);
    Task<bool> DeleteAttendanceAsync(Guid attendanceId);
    Task<List<Attendance>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<List<DepartmentAttendanceStatsDto>> GetDepartmentAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<List<DailyAttendanceStatsDto>> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<List<AttendanceDto>> GetAttendanceRecordsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<Guid> GetEmpId(Guid? employeeId);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date);
}
