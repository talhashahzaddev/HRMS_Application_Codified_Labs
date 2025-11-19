using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;

namespace HRMS.API.Services.Interfaces;

public interface IAttendanceService
{

    Task<bool> DeleteShiftAsync(Guid organizationId, Guid shiftId);
    Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request);

    Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeId);
    Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeId);
    Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId);
    Task<bool> ApproveShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto);
    Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto);
    Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId);
    Task<PagedResult<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId, int page, int pageSize, string search);
    Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request);
    Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto rquest);
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
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId);
    Task<bool> BulkApproveAttendanceAsync(List<Guid> attendanceIds);
    Task<bool> BulkRejectAttendanceAsync(List<Guid> attendanceIds, string? reason);
}
