using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ITimesheetService
{
    // Config
    Task<ServiceResponse<TimesheetConfigDto>> GetConfigAsync(Guid orgId);
    Task<ServiceResponse<TimesheetConfigDto>> UpdateConfigAsync(Guid orgId, TimesheetConfigDto dto, Guid actorId);

    // Periods
    Task<ServiceResponse<List<TimesheetPeriodDto>>> GetPeriodsAsync(Guid orgId, DateTime? from, DateTime? to);
    Task<ServiceResponse<TimesheetPeriodDto>> GetPeriodAsync(Guid timesheetId);
    Task<ServiceResponse<Guid>> CreatePeriodAsync(Guid orgId, CreateTimesheetPeriodDto dto, Guid createdBy);
    Task<ServiceResponse<bool>> TransitionStatusAsync(Guid timesheetId, string newStatus, Guid actorId, string? reason);
    Task<ServiceResponse<TimesheetLockBlockersDto>> GetBlockersAsync(Guid timesheetId);

    // Employee Days
    Task<ServiceResponse<List<EmployeeTimesheetDto>>> GetEmployeeDaysAsync(Guid timesheetId, Guid orgId, Guid? employeeId);

    // Corrections
    Task<ServiceResponse<List<PendingCorrectionDto>>> GetPendingCorrectionsAsync(Guid timesheetId, Guid orgId);
    Task<ServiceResponse<Guid>> SubmitCorrectionAsync(Guid timesheetId, Guid employeeId, Guid orgId, TimesheetCorrectionDto dto);
    Task<ServiceResponse<bool>> ApproveCorrectionAsync(Guid correctionId, Guid managerId);
    Task<ServiceResponse<bool>> RejectCorrectionAsync(Guid correctionId, Guid managerId, string reason);
    Task<ServiceResponse<BulkApproveResult>> ApproveBulkCorrectionsAsync(TimesheetBulkApproveDto dto, Guid managerId);

    // Overrides
    Task<ServiceResponse<bool>> ApplyOverrideAsync(Guid timesheetId, Guid managerId, Guid orgId, TimesheetOverrideDto dto);

    // Finalization
    Task<ServiceResponse<int>> FinalizeBatchAsync(Guid timesheetId, Guid orgId, Guid actorId, Guid? employeeId);

    // Payroll Bridge
    Task<ServiceResponse<List<TimesheetPeriodLinkDto>>> GetFinalizedForPayrollAsync(Guid orgId);
    Task<ServiceResponse<TimesheetPayrollSummaryDto>> GetPayrollSummaryAsync(Guid timesheetId);
    Task<ServiceResponse<bool>> LockForPayrollAsync(Guid timesheetId, Guid lockedBy, Guid? payrollPeriodId);

    // Audit
    Task<ServiceResponse<List<TimesheetAuditEntryDto>>> GetAuditLogAsync(Guid timesheetId);

    // ── Projects ──
    Task<ServiceResponse<List<TimesheetProjectDto>>> GetProjectsAsync(Guid organizationId);
    Task<ServiceResponse<Guid>> CreateProjectAsync(TimesheetProjectDto dto);
    Task<ServiceResponse<bool>> UpdateProjectAsync(Guid projectId, TimesheetProjectDto dto);
    Task<ServiceResponse<List<TimesheetTaskDto>>> GetProjectTasksAsync(Guid projectId);
    Task<ServiceResponse<TimesheetTaskDto>> CreateTaskAsync(Guid projectId, TimesheetTaskDto dto);

    // ── Time Allocations ──
    Task<ServiceResponse<List<TimeAllocationDto>>> GetTimeAllocationsAsync(Guid timesheetId, Guid employeeId);
    Task<ServiceResponse<bool>> SaveTimeAllocationsAsync(SaveTimeAllocationDto dto);

    // ── Comp Time ──
    Task<ServiceResponse<CompTimeBalanceDto>> GetCompTimeBalanceAsync(Guid employeeId, Guid organizationId, int year);

    // ── Dashboard ──
    Task<ServiceResponse<TimesheetDashboardDto>> GetDashboardAsync(Guid organizationId, Guid? periodId);

    // ── Delegation ──
    Task<ServiceResponse<TimesheetDelegationDto?>> GetActiveDelegationAsync(Guid approverId);
    Task<ServiceResponse<Guid>> CreateDelegationAsync(CreateTimesheetDelegationDto dto, Guid delegatedFrom);

    // ── Rate Cards ──
    Task<ServiceResponse<List<EmployeeRateCardDto>>> GetRateCardsAsync(Guid organizationId);

    // ── Bulk Operations ──
    Task<ServiceResponse<bool>> BulkCopyFromPreviousAsync(BulkCopyRequestDto dto);
    Task<ServiceResponse<bool>> AutoFillFromScheduleAsync(BulkAutoFillRequestDto dto);
}