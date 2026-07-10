using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories.Interfaces;

public interface ITimesheetRepository
{
    Task<TimesheetConfigDto?> GetConfigAsync(Guid orgId);
    Task<TimesheetConfigDto> UpsertConfigAsync(Guid orgId, TimesheetConfigDto dto);

    Task<IEnumerable<TimesheetPeriodDto>> GetPeriodsAsync(Guid orgId, DateTime? from, DateTime? to);
    Task<TimesheetPeriodDto?> GetPeriodAsync(Guid timesheetId);
    Task<Guid> CreatePeriodAsync(Guid orgId, CreateTimesheetPeriodDto dto, Guid createdBy);
    Task<bool> TransitionStatusAsync(Guid timesheetId, string newStatus, Guid actorId, string? reason);
    Task<int> CountPendingCorrectionsAsync(Guid timesheetId);

    Task<IEnumerable<EmployeeTimesheetDto>> GetEmployeeDaysAsync(Guid timesheetId, Guid orgId, Guid? employeeId);

    Task<IEnumerable<PendingCorrectionDto>> GetPendingCorrectionsAsync(Guid timesheetId, Guid orgId);
    Task<Guid?> CreateCorrectionAsync(Guid timesheetId, Guid employeeId, Guid orgId, TimesheetCorrectionDto dto);
    Task<TimesheetCorrectionDto?> GetCorrectionAsync(Guid correctionId);
    Task<CorrectionActionResult> ApproveCorrectionAsync(Guid correctionId, Guid managerId);
    Task<CorrectionActionResult> RejectCorrectionAsync(Guid correctionId, Guid managerId, string reason);
    Task<BulkApproveResult> ApproveBulkCorrectionsAsync(IEnumerable<Guid> correctionIds, Guid managerId);

    Task<bool> ApplyOverrideAsync(Guid timesheetId, Guid managerId, Guid orgId, TimesheetOverrideDto dto);

    Task<int> FinalizeBatchAsync(Guid timesheetId, Guid orgId, Guid actorId, Guid? employeeId);

    Task<IEnumerable<TimesheetPeriodLinkDto>> GetFinalizedTimesheetLinksAsync(Guid orgId);
    Task<TimesheetPayrollSummaryDto?> GetPayrollSummaryAsync(Guid timesheetId);
    Task<bool> LockForPayrollAsync(Guid timesheetId, Guid lockedBy, Guid? payrollPeriodId);

    Task<IEnumerable<TimesheetAuditEntryDto>> GetAuditLogAsync(Guid timesheetId);
    Task InsertAuditEntryAsync(Guid timesheetId, string entityType, Guid entityId,
        string action, Guid actorId, string? actorRole, string? oldValues, string? newValues, string? reason);

    // ── Projects ──
    Task<List<TimesheetProjectDto>> GetProjectsAsync(Guid organizationId);
    Task<Guid> CreateProjectAsync(TimesheetProjectDto dto);
    Task<List<TimesheetTaskDto>> GetProjectTasksAsync(Guid projectId);
    Task<TimesheetTaskDto> CreateTaskAsync(Guid projectId, TimesheetTaskDto dto);

    // ── Time Allocations ──
    Task<List<TimeAllocationDto>> GetTimeAllocationsAsync(Guid timesheetId, Guid employeeId);
    Task SaveTimeAllocationsAsync(SaveTimeAllocationDto dto);

    // ── Comp Time ──
    Task<CompTimeBalanceDto> GetCompTimeBalanceAsync(Guid employeeId, Guid organizationId, int year);

    // ── Dashboard ──
    Task<TimesheetDashboardDto> GetDashboardAsync(Guid organizationId, Guid? periodId);

    // ── Delegation ──
    Task<TimesheetDelegationDto?> GetActiveDelegationAsync(Guid approverId);
    Task<Guid> CreateDelegationAsync(CreateTimesheetDelegationDto dto, Guid delegatedFrom);

    // ── Rate Cards ──
    Task<List<EmployeeRateCardDto>> GetRateCardsAsync(Guid organizationId);

    // ── Bulk Operations ──
    Task BulkCopyFromPreviousAsync(BulkCopyRequestDto dto);
    Task AutoFillFromScheduleAsync(BulkAutoFillRequestDto dto);

    // ── Guards ──
    Task<bool> IsRecordOverriddenAsync(Guid? attendanceId, Guid employeeId, DateTime workDate);
    Task<bool> IsAttendanceOwnedByEmployeeAsync(Guid attendanceId, Guid employeeId);
    Task<bool> DoesEmployeeHaveAttendanceOnDateAsync(Guid employeeId, DateTime workDate);
    Task<IEnumerable<Guid>> GetEmployeeIdsInTimesheetAsync(Guid timesheetId);
    Task<bool> IsActorTimesheetApproverAsync(Guid timesheetId, Guid actorId);
}