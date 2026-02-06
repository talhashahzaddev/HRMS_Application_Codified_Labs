using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;

namespace HRMS.API.Services.Interfaces;

public interface IAttendanceService
{
    //Task<List<AttendanceSessionWithInfoDto>> EmployeeAllAttendanceAsync(Guid employeeId,DateTime startDate,DateTime endDate,int pageNumber,int pageSize);
    Task<PagedResult<AttendanceSessionWithInfoDto>> EmployeeAllAttendanceAsync(
    Guid employeeId,
    DateTime startDate,
    DateTime endDate,
    int pageNumber,
    int pageSize);

    Task<bool> DeleteShiftAsync(Guid organizationId, Guid shiftId);
    Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request);
    Task<List<AttendanceSessionDto?>> EmployeeTodaySessions(Guid employeeId, DateTime? date);
    Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeId);
    Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeId);
    Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId,Guid currentEmpid);
    Task<bool> ApproveShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto,Guid organizationId);
    Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto);
    Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId);
    Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId ,Guid CurrentEmployee);
    Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request,Guid currentEmpId);
    Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto rquest);
    Task<ClockInOutResponse> ClockInAsync(Guid employeeId, ClockInOutRequest request, string ipAddress);
    Task<ClockInOutResponse> ClockOutAsync(Guid employeeId, ClockInOutRequest request, string ipAddress);
    Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(AttendanceSearchViewModel searchModel, Guid organizationId, Guid currentEmployeeId);
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
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid employeeId,Guid organizationId, DateTime date);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid currentEmployeeId, Guid? departmentId, string? status, Guid? employeeId, int pageNumber = 1, int pageSize = 10);
    Task<List<EmployeeSimpleDto>> GetdepartmentEmployeesAsync(Guid currentEmployee, Guid? departmentId,Guid organizationId);
    Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId, Guid currentEmployeeId, Guid? departmentId, string? status);
    Task<bool> BulkApproveAttendanceAsync(List<Guid> attendanceIds);
    Task<bool> BulkRejectAttendanceAsync(List<Guid> attendanceIds, string? reason);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
}
