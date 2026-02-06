﻿using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<bool> CheckAlreadyClockInAsync(Guid employeeId);
    Task<List<EmployeeShiftDto>> GetManagerEmployeesByShiftAsync(Guid organizationId, Guid shiftId, Guid currentEmp);
    Task<ShiftSwapRequestDto?> getshifrequestDetailsAsync(Guid requestId);
    Task<ShiftDto?> GetshiftInfoAsync(Guid shiftId);
    Task<PagedResult<AttendanceSessionWithInfoDto>> GetEmployeeSessionsWithInfoAsync(Guid employeeId,DateTime startDate,DateTime endDate,int pageNumber = 1,int pageSize = 10);
    Task<List<PendingShiftSwapDto>> GetManagerPendingShiftsAsync(Guid organizationId, Guid currentEmployeeId);
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
    Task<ShiftSwapRequestCreatedDto> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto);
    Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId);
    Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId);
    Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request);
    Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request);

    Task<bool> ClockInOutAsync(Guid employeeId, string action, DateTime timestamp, Dictionary<string, object>? location = null, string? notes = null);
    Task<Attendance?> GetTodayAttendanceAsync(Guid employeeId);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(Guid? employeeId = null, Guid? organizationId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 100, Guid? departmentId=null, string? Status=null);
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<bool> UpdateAttendanceAsync(Guid attendanceId, Attendance attendance);
    Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date);
    
    Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId);
    Task<Attendance> CreateAttendanceAsync(Attendance attendance);
    Task<bool> DeleteAttendanceAsync(Guid attendanceId);
    Task<List<Attendance>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<List<DepartmentAttendanceStatsDto>> GetDepartmentAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate,Guid? departmentId);
    Task<List<DailyAttendanceStatsDto>> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<PagedResult<AttendanceDto>> GetAttendanceRecordsAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid? departmentId, string? status, Guid? employeeId, int pageNumber = 1, int pageSize = 10);
    Task<Guid> GetEmpId(Guid? employeeId);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
    Task<bool> IsHRManagerAsync(Guid employeeId, Guid organizationId);
    Task<bool> IsAdminOrHRAsync(Guid employeeId, Guid organizationId);
    Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsForManagerAsync(Guid employeeId, Guid organizationId, DateTime date);
    Task<List<Guid>> GetTeamEmployeeIdsAsync(Guid managerEmployeeId, Guid organizationId);

    // Manual Attendance
    Task<bool> CreateMonthlySnapshotAsync(Guid organizationId, Guid generatedBy, MonthlyTimesheetCreateDto dto);
    Task<IEnumerable<PendingAttendanceRequest>> GetAllPendingRequestsAsync(Guid organizationId, Guid? managerId = null);
    Task<Guid> SubmitUpdateRequestAsync(Guid employeeId, Guid organizationId, AttendanceUpdateRequestDto dto);
    Task<bool> ProcessUpdateRequestAsync(Guid requestId, Guid actionedBy, bool isApproved, string? rejectionReason);
    Task<AttendanceUpdateRequestDto?> GetAttendanceUpdateRequestDetailsAsync(Guid requestId);
    //    Task<IEnumerable<dynamic>> GetPendingRequestsForManagerAsync(Guid managerId, Guid organizationId);
    Task<IEnumerable<Attendance>> GetManualAttendanceRecordsAsync(Guid organizationId, Guid? managerId, Guid? employeeId, DateTime startDate, DateTime endDate);
    Task<bool> UpdateManualAttendanceAsync(Attendance attendance, AttendanceAudit audit);
    Task<bool> HasPermissionToEditAsync(Guid editorId, Guid targetEmployeeId, Guid organizationId);
    Task<int> BatchFinalizeMonthAsync(Guid timesheetId, Guid organizationId, Guid actionedBy);
    Task<IEnumerable<FinalizedTimesheetRecordDto>> GetFinalizedRecordsAsync(Guid timesheetId, Guid? employeeId = null);
    Task<IEnumerable<MonthlyTimesheetSummary>> GetMonthlySnapshotsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate);
    Task<IEnumerable<AttendanceDto>> GetConsolidatedAttendanceAsync(
          Guid organizationId, DateTime startDate, DateTime endDate, Guid? employeeId, Guid? managerId = null);
    Task<MonthlyTimesheetSummary?> GetSnapshotByIdAsync(Guid timesheetId);
    Task<int> SubmitBatchForApprovalAsync(Guid employeeId, Guid organizationId, Guid timesheetId, DateTime startDate, DateTime endDate);
    Task<int> ApproveAllBatchAsync(Guid timesheetId, Guid? targetEmployeeId, Guid managerId, Guid organizationId);
    Task<OrgSubmissionProgressDto> GetOrgSubmissionProgressAsync(Guid organizationId, int month, int year);
}
