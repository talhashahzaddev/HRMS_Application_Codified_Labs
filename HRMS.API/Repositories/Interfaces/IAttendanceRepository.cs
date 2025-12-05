using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<PagedResult<AttendanceSessionWithInfoDto>> GetEmployeeSessionsWithInfoAsync(
    Guid employeeId,
    DateTime startDate,
    DateTime endDate,
    int pageNumber = 1,
    int pageSize = 10);
    Task<List<PendingShiftSwapDto>> GetManagerPendingShiftsAsync(Guid organizationId, Guid currentEmployeeId);
    //Task<List<AttendanceSessionWithInfoDto>> GetEmployeeSessionsWithInfoAsync(Guid employeeId,DateTime startDate,DateTime endDate,int pageNumber = 1,int pageSize = 10);
    Task<List<EmployeeSimpleDto>> GetEmployeeForAdminAsync(Guid organizationId, Guid? departmentId);
    Task<List<EmployeeSimpleDto>> GetManagerEmployeeAsync(Guid managerId,Guid organizationId, Guid? departmentId);
    Task<Attendance?> UpdateOvertimeHoursAsync(Guid employeeId);
    Task<Attendance?> UpdateTodayAttendanceLateStatusAsync(Guid employeeId);
    Task<Attendance?> UpdateTodayAttendanceFromSessionsAsync(Guid employeeId);
    Task<Guid?> GetAttendanceIdAsync(Guid employeeId, DateTime? date);
    Task<List<AttendanceSessionDto>> GetTodaySessionAsync(Guid? attendanceId);
    Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request);
    Task<bool> DeleteShiftAsync(Guid organizationId, Guid shiftId);
    Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeId);
    Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeId);
    Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId);
    Task<bool> RejectShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto);
    Task<bool> AcceptswapRequestAsync(ApproveShiftSwapRequestDto dto);
    Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto);
    Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId);
    Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId);
    Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request);
    Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request);

    Task<bool> ClockInOutAsync(Guid employeeId, string action, DateTime timestamp, Dictionary<string, object>? location = null);
    Task<Attendance?> GetTodayAttendanceAsync(Guid employeeId);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(Guid? employeeId = null, Guid? organizationId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 100, Guid? departmentId=null, string Status=null);
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<bool> UpdateAttendanceAsync(Guid attendanceId, Attendance attendance);
    Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date);
    
    // New missing methods
    Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId);
    Task<Attendance> CreateAttendanceAsync(Attendance attendance);
    Task<bool> DeleteAttendanceAsync(Guid attendanceId);
    Task<List<Attendance>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<List<DepartmentAttendanceStatsDto>> GetDepartmentAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate,Guid? departmentId);
    Task<List<DailyAttendanceStatsDto>> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<List<AttendanceDto>> GetAttendanceRecordsAsync(Guid organizationId, DateTime startDate, DateTime endDate,Guid? departmentId,string? status,Guid? employeeId);
    Task<Guid> GetEmpId(Guid? employeeId);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
    Task<bool> IsHRManagerAsync(Guid employeeId, Guid organizationId);
    Task<bool> IsAdminOrHRAsync(Guid employeeId, Guid organizationId);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsForManagerAsync(Guid employeeId, Guid organizationId, DateTime date);
    Task<List<Guid>> GetTeamEmployeeIdsAsync(Guid managerEmployeeId, Guid organizationId);
}
