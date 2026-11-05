using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
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
    Task<IReadOnlyList<AttendanceSessionDto>> EmployeeTodaySessions(Guid employeeId, DateTime? date);
    Task<IReadOnlyList<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeId);
    Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeId);
    Task<IReadOnlyList<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId, Guid currentEmpid);
    Task<bool> ApproveShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto, Guid organizationId);
    Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto);
    Task<IReadOnlyList<ShiftDto>> GetAllShiftsAsync(Guid organizationId);
    Task<IReadOnlyList<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId, Guid CurrentEmployee);
    Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request, Guid currentEmpId);
    Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto rquest);
    Task<ClockInOutResponse> ClockInAsync(Guid employeeId, ClockInOutRequest request, string ipAddress);
    Task<ClockInOutResponse> ClockOutAsync(Guid employeeId, ClockInOutRequest request, string ipAddress);
    Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(AttendanceSearchViewModel searchModel, Guid organizationId, Guid currentEmployeeId);
    Task<PagedResult<AttendanceDto>> GetTeamAttendanceAsync(AttendanceSearchViewModel searchModel,Guid organizationId,Guid currentEmployeeId);
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<bool> UpdateAttendanceAsync(Guid attendanceId, CreateAttendanceRequest request);
    Task<IReadOnlyList<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date);

    // New missing methods
    Task<TimeTrackingSessionDto?> GetCurrentSessionAsync(Guid employeeId);
    Task<AttendanceDto?> GetAttendanceByIdAsync(Guid attendanceId);
    Task<AttendanceDto> CreateManualAttendanceAsync(ManualAttendanceRequest request, Guid organizationId);
    Task<AttendanceDto> UpdateAttendanceAsync(Guid attendanceId, ManualAttendanceRequest request);
    Task<bool> DeleteAttendanceAsync(Guid attendanceId);
    Task<IReadOnlyList<AttendanceDto>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<AttendanceCalendarDto>> GetAttendanceCalendarAsync(Guid employeeId, int year, int month);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid employeeId, Guid organizationId, DateTime date);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid currentEmployeeId, Guid? departmentId, string? status, Guid? employeeId, int pageNumber = 1, int pageSize = 10);
    Task<IReadOnlyList<EmployeeSimpleDto>> GetdepartmentEmployeesAsync(Guid currentEmployee, Guid? departmentId, Guid organizationId);
    Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId, Guid currentEmployeeId, Guid? departmentId, string? status);
    Task<bool> BulkApproveAttendanceAsync(List<Guid> attendanceIds);
    Task<bool> BulkRejectAttendanceAsync(List<Guid> attendanceIds, string? reason);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
    Task<ServiceResponse<IReadOnlyList<EmployeeTimesheetDto>>> GetTimesheetDetailsByIdAsync(Guid timesheetId, Guid organizationId, Guid currentEmployeeId);
    Task<ServiceResponse<bool>> ProcessAttendanceUpdateRequestAsync(ProcessAttendanceRequestDto dto, Guid actionedBy, Guid organizationId);
    Task<ServiceResponse<int>> FinalizeTimesheetBatchAsync(Guid timesheetId, Guid organizationId, Guid managerEmployeeId, Guid? employeeId = null);
    Task<ServiceResponse<IReadOnlyList<FinalizedTimesheetRecordDto>>> GetApprovedTimesheetAsync(Guid timesheetId, Guid currentUserEmployeeId, string role);
    Task<ServiceResponse<bool>> CreateTimesheetSnapshotAsync(Guid organizationId, Guid currentUserEmployeeId, MonthlyTimesheetCreateDto dto);
    Task<ServiceResponse<Guid>> RequestAttendanceUpdateAsync(Guid employeeId, Guid organizationId, AttendanceUpdateRequestDto dto);
    Task<ServiceResponse<IReadOnlyList<Attendance>>> GetManualAttendanceAsync(ManualAttendanceSearchDto searchDto, Guid currentUserEmployeeId, string currentUserRole, Guid organizationId);
    Task<ServiceResponse<bool>> UpdateManualAttendanceAsync(ManualAttendanceUpdateDto updateDto, Guid currentUserEmployeeId, string currentUserRole, Guid organizationId, string ipAddress);
    Task<ServiceResponse<IReadOnlyList<PendingAttendanceRequest>>> GetPendingRequestsAsync(Guid organizationId, Guid currentEmployeeId);
    Task<ServiceResponse<IReadOnlyList<MonthlyTimesheetSummary>>> GetMonthlyTimesheetSnapshotsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate, Guid currentEmployeeId);
    Task<ServiceResponse<int>> SubmitTimesheetBatchAsync(Guid timesheetId, Guid employeeId, Guid organizationId);
    Task<ServiceResponse<int>> ApproveAllPendingAsync(Guid timesheetId, Guid? targetEmployeeId, Guid managerId, Guid orgId);
    Task<ServiceResponse<IReadOnlyList<EmployeeSubmissionPackage>>> GetGroupedPendingRequestsAsync(Guid organizationId, Guid currentEmployeeId);
    Task<ServiceResponse<IReadOnlyList<EmployeeReviewPackage>>> GetManagerReviewDashboardAsync(Guid timesheetId, Guid organizationId, Guid currentUserId, Guid? targetEmployeeId = null);
    Task<OrgSubmissionProgressDto> GetOrgSubmissionProgressAsync(
       Guid organizationId, int month, int year);
    Task<bool> AdminOverrideAttendanceAsync(ManualAttendanceUpdateDto dto, Guid managerId, Guid orgId);
    Task<ShiftSummary> GetShiftSummaryAsync(Guid organizationId, Guid currentEmployeeId);
    Task<bool> CreateEmployeeOvertimeRequestAsync(Guid organizationId, Guid employeeId, CreateEmployeeOvertimeDto request);
    Task<bool> UpdateEmployeeOvertimeStatusAsync(Guid organizationId, Guid requestId, Guid managerId, string status);
    Task<bool> CreateManagerOvertimeAsync(Guid organizationId, Guid managerId, ManagerOvertimeRequestDto request);
    Task<IReadOnlyList<OvertimeRequestDto>> GetEmployeeOvertimeRequestsAsync(Guid organizationId, Guid employeeId);
    Task<IReadOnlyList<EmployeeOverTimeDto>> EmplyeOverTimeRequesForManagerAsync(Guid organizationId, Guid managerId);
    Task<bool> RespondToAssignedOvertimeRequestAsync(Guid requestId, Guid employeeId, string response);
    Task<IReadOnlyList<EmployeeOverTimeDto>> GetAllEmployeeCreatedOverTimeRequest(Guid organizationId, Guid managerId);
    Task<IReadOnlyList<OvertimeRequestDto>> GetAllEmployeeOvertimeAssignedRequests(Guid organizationId, Guid employeeId);

    // Payroll bridge
    Task<ServiceResponse<IReadOnlyList<TimesheetPeriodLinkDto>>> GetFinalizedTimesheetLinksAsync(Guid organizationId);
    Task<ServiceResponse<TimesheetPayrollSummaryDto?>> GetTimesheetPayrollSummaryAsync(Guid timesheetId);
    Task<ServiceResponse<bool>> LockTimesheetAsync(Guid timesheetId, Guid lockedBy, Guid? payrollPeriodId);
    Task<OvertimeRequestDto?> GetEmployeeTodayOvertimeRequestsAsync(Guid organizationId, Guid employeeId);

    // CR6: Admin/HR check for authorization
    Task<bool> IsAdminOrHRAsync(Guid employeeId, Guid organizationId);
}
