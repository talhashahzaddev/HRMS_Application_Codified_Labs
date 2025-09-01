using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;

namespace HRMS.API.Services.Interfaces;

public interface IAttendanceService
{
    Task<bool> ClockInAsync(Guid employeeId, ClockInOutRequest request);
    Task<bool> ClockOutAsync(Guid employeeId, ClockInOutRequest request);
    Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(AttendanceSearchViewModel searchModel, Guid organizationId);
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<bool> UpdateAttendanceAsync(Guid attendanceId, CreateAttendanceRequest request);
    Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date);
    
    // New missing methods
    Task<TimeTrackingSessionDto?> GetCurrentSessionAsync(Guid employeeId);
    Task<AttendanceDto?> GetAttendanceByIdAsync(Guid attendanceId);
    Task<AttendanceDto> CreateManualAttendanceAsync(ManualAttendanceRequest request);
    Task<AttendanceDto> UpdateAttendanceAsync(Guid attendanceId, ManualAttendanceRequest request);
    Task<bool> DeleteAttendanceAsync(Guid attendanceId);
    Task<List<AttendanceDto>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<List<AttendanceCalendarDto>> GetAttendanceCalendarAsync(Guid employeeId, int year, int month);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid? employeeId, Guid? departmentId);
    Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId);
    Task<bool> BulkApproveAttendanceAsync(List<Guid> attendanceIds);
    Task<bool> BulkRejectAttendanceAsync(List<Guid> attendanceIds, string? reason);
}
