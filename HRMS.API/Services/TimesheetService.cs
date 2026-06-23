using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class TimesheetService : ITimesheetService
{
    private readonly ITimesheetRepository _repo;
    private readonly ILogger<TimesheetService> _logger;

    public TimesheetService(ITimesheetRepository repo, ILogger<TimesheetService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ── Guard Helpers ──────────────────────────────────────────────────────
    /// <summary>
    /// Validates that the actor is not the same employee whose timesheet is being acted upon.
    /// Prevents self-approval: a manager cannot approve their own timesheet.
    /// </summary>
    private async Task<ServiceResponse<bool>?> ValidateNotSelfApprovalAsync(Guid actorId, Guid timesheetId)
    {
        var employeeIds = (await _repo.GetEmployeeIdsInTimesheetAsync(timesheetId)).ToList();
        if (employeeIds.Contains(actorId))
            return new ServiceResponse<bool> { Success = false, Message = "You cannot approve or reject your own timesheet." };
        return null; // OK
    }

    /// <summary>
    /// Validates that the actor is authorized to approve/reject this timesheet.
    /// The actor must be either:
    /// 1. An assigned approver in the approval workflow, OR
    /// 2. A reporting manager of at least one employee in the timesheet (checked via workflow progress)
    /// </summary>
    private async Task<ServiceResponse<bool>?> ValidateApproverAuthorizationAsync(Guid actorId, Guid timesheetId)
    {
        var isApprover = await _repo.IsActorTimesheetApproverAsync(timesheetId, actorId);
        if (!isApprover)
            return new ServiceResponse<bool> { Success = false, Message = "You are not authorized to approve or reject this timesheet." };
        return null; // OK
    }

    // ── Config ──────────────────────────────────────────────────────────
    public async Task<ServiceResponse<TimesheetConfigDto>> GetConfigAsync(Guid orgId)
    {
        var config = await _repo.GetConfigAsync(orgId);
        if (config == null)
            return new ServiceResponse<TimesheetConfigDto> { Success = false, Message = "No timesheet config found for this organization." };
        return new ServiceResponse<TimesheetConfigDto> { Success = true, Data = config };
    }

    public async Task<ServiceResponse<TimesheetConfigDto>> UpdateConfigAsync(Guid orgId, TimesheetConfigDto dto, Guid actorId)
    {
        if (orgId == Guid.Empty)
            return new ServiceResponse<TimesheetConfigDto> { Success = false, Message = "Organization ID cannot be empty." };
        if (dto == null)
            return new ServiceResponse<TimesheetConfigDto> { Success = false, Message = "Request body is required." };

        var errors = new List<string>();
        if (dto.FullDayThresholdHours <= 0 || dto.FullDayThresholdHours > 24)
            errors.Add("Full day threshold must be between 0.1 and 24.0");
        if (dto.HalfDayThresholdHours <= 0 || dto.HalfDayThresholdHours >= dto.FullDayThresholdHours)
            errors.Add("Half day threshold must be between 0.1 and full day threshold");
        if (dto.LateGraceMinutes < 0 || dto.LateGraceMinutes > 120)
            errors.Add("Late grace minutes must be between 0 and 120");
        if (dto.CorrectionDeadlineDays < 0)
            errors.Add("Correction deadline days must be 0 or positive");
        if (dto.OvertimeMultiplierRegular <= 1.0m)
            errors.Add("Overtime multiplier must be greater than 1.0");
        if (errors.Any())
            return new ServiceResponse<TimesheetConfigDto> { Success = false, Message = "Config validation failed", Errors = errors };

        var config = await _repo.UpsertConfigAsync(orgId, dto);
        return new ServiceResponse<TimesheetConfigDto> { Success = true, Data = config };
    }

    // ── Periods ──────────────────────────────────────────────────────────
    public async Task<ServiceResponse<List<TimesheetPeriodDto>>> GetPeriodsAsync(Guid orgId, DateTime? from, DateTime? to)
    {
        if (orgId == Guid.Empty)
            return new ServiceResponse<List<TimesheetPeriodDto>> { Success = false, Message = "Organization ID cannot be empty." };
        var periods = (await _repo.GetPeriodsAsync(orgId, from, to)).ToList();
        foreach (var p in periods) p.NextActions = GetNextActions(p.Status);
        return new ServiceResponse<List<TimesheetPeriodDto>> { Success = true, Data = periods };
    }
    public async Task<ServiceResponse<TimesheetPeriodDto>> GetPeriodAsync(Guid timesheetId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<TimesheetPeriodDto> { Success = false, Message = "Timesheet ID cannot be empty." };
        var period = await _repo.GetPeriodAsync(timesheetId);
        if (period == null)
            return new ServiceResponse<TimesheetPeriodDto> { Success = false, Message = "Timesheet not found." };
        period.NextActions = GetNextActions(period.Status);
        return new ServiceResponse<TimesheetPeriodDto> { Success = true, Data = period };
    }

    public async Task<ServiceResponse<Guid>> CreatePeriodAsync(Guid orgId, CreateTimesheetPeriodDto dto, Guid createdBy)
    {
        if (orgId == Guid.Empty)
            return new ServiceResponse<Guid> { Success = false, Message = "Organization ID cannot be empty." };
        if (dto == null)
            return new ServiceResponse<Guid> { Success = false, Message = "Request body is required." };
        if (string.IsNullOrWhiteSpace(dto.PeriodName))
            return new ServiceResponse<Guid> { Success = false, Message = "Period name is required." };
        if (dto.PeriodEnd < dto.PeriodStart)
            return new ServiceResponse<Guid> { Success = false, Message = "Period end date must be after start date." };
        if ((dto.PeriodEnd - dto.PeriodStart).TotalDays > 92)
            return new ServiceResponse<Guid> { Success = false, Message = "Period cannot exceed 92 days." };

        // Scope validation
        var scopeType = string.IsNullOrWhiteSpace(dto.ScopeType) ? "all" : dto.ScopeType;
        if (scopeType is not ("all" or "department" or "employees"))
            return new ServiceResponse<Guid> { Success = false, Message = "Invalid scope type. Must be 'all', 'department', or 'employees'." };
        if (scopeType == "department" && (dto.DepartmentIds == null || !dto.DepartmentIds.Any()))
            return new ServiceResponse<Guid> { Success = false, Message = "At least one department must be selected." };
        if (scopeType == "employees" && (dto.EmployeeIds == null || !dto.EmployeeIds.Any()))
            return new ServiceResponse<Guid> { Success = false, Message = "At least one employee must be selected." };

        var id = await _repo.CreatePeriodAsync(orgId, dto, createdBy);
        return new ServiceResponse<Guid> { Success = true, Data = id };
    }

    public async Task<ServiceResponse<bool>> TransitionStatusAsync(Guid timesheetId, string newStatus, Guid actorId, string? reason)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<bool> { Success = false, Message = "Timesheet ID cannot be empty." };
        if (string.IsNullOrWhiteSpace(newStatus))
            return new ServiceResponse<bool> { Success = false, Message = "New status is required." };

        var period = await _repo.GetPeriodAsync(timesheetId);
        if (period == null)
            return new ServiceResponse<bool> { Success = false, Message = "Timesheet not found." };

        var allowed = GetNextActions(period.Status);
        if (!allowed.Contains(newStatus))
            return new ServiceResponse<bool> { Success = false, Message = $"Cannot move period from '{period.Status}' to '{newStatus}'." };

        // Guard: self-approval prevention — actor cannot approve their own timesheet
        if (newStatus is "Approved" or "UnderReview")
        {
            var selfApprovalCheck = await ValidateNotSelfApprovalAsync(actorId, timesheetId);
            if (selfApprovalCheck != null) return selfApprovalCheck;
        }

        // Guard: actor authorization — only authorized approvers can transition to approval states
        if (newStatus is "Approved" or "UnderReview")
        {
            var authCheck = await ValidateApproverAuthorizationAsync(actorId, timesheetId);
            if (authCheck != null) return authCheck;
        }

        if (newStatus == "Finalized")
        {
            var pendingCount = await _repo.CountPendingCorrectionsAsync(timesheetId);
            if (pendingCount > 0)
                return new ServiceResponse<bool> { Success = false, Message = $"{pendingCount} pending correction(s) must be resolved before finalizing." };
        }

        if (newStatus == "Locked")
        {
            if (period.Status != "Finalized")
                return new ServiceResponse<bool> { Success = false, Message = "Timesheet must be Finalized before Locked." };
            var pendingCount = await _repo.CountPendingCorrectionsAsync(timesheetId);
            if (pendingCount > 0)
                return new ServiceResponse<bool> { Success = false, Message = $"{pendingCount} pending correction(s) must be resolved." };
        }

        await _repo.TransitionStatusAsync(timesheetId, newStatus, actorId, reason);
        return new ServiceResponse<bool> { Success = true, Data = true };
    }

    public async Task<ServiceResponse<TimesheetLockBlockersDto>> GetBlockersAsync(Guid timesheetId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<TimesheetLockBlockersDto> { Success = false, Message = "Timesheet ID cannot be empty." };
        var period = await _repo.GetPeriodAsync(timesheetId);
        if (period == null)
            return new ServiceResponse<TimesheetLockBlockersDto> { Success = false, Message = "Timesheet not found." };

        var blockers = new List<string>();
        if (period.Status != "Finalized")
            blockers.Add("Timesheet must be Finalized before Locked");

        var pendingCount = await _repo.CountPendingCorrectionsAsync(timesheetId);
        if (pendingCount > 0)
            blockers.Add($"{pendingCount} pending correction(s) must be resolved");

        return new ServiceResponse<TimesheetLockBlockersDto>
        {
            Success = true,
            Data = new TimesheetLockBlockersDto { CanLock = !blockers.Any(), Blockers = blockers }
        };
    }

    // ── Employee Days ────────────────────────────────────────────────────
    public async Task<ServiceResponse<List<EmployeeTimesheetDto>>> GetEmployeeDaysAsync(Guid timesheetId, Guid orgId, Guid? employeeId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<List<EmployeeTimesheetDto>> { Success = false, Message = "Timesheet ID cannot be empty." };
        if (orgId == Guid.Empty)
            return new ServiceResponse<List<EmployeeTimesheetDto>> { Success = false, Message = "Organization ID cannot be empty." };
        var days = (await _repo.GetEmployeeDaysAsync(timesheetId, orgId, employeeId)).ToList();
        return new ServiceResponse<List<EmployeeTimesheetDto>> { Success = true, Data = days };
    }

    // ── Corrections ───────────────────────────────────────────────────────
    public async Task<ServiceResponse<List<PendingCorrectionDto>>> GetPendingCorrectionsAsync(Guid timesheetId, Guid orgId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<List<PendingCorrectionDto>> { Success = false, Message = "Timesheet ID cannot be empty." };
        if (orgId == Guid.Empty)
            return new ServiceResponse<List<PendingCorrectionDto>> { Success = false, Message = "Organization ID cannot be empty." };
        var corrections = (await _repo.GetPendingCorrectionsAsync(timesheetId, orgId)).ToList();
        return new ServiceResponse<List<PendingCorrectionDto>> { Success = true, Data = corrections };
    }

    public async Task<ServiceResponse<Guid>> SubmitCorrectionAsync(Guid timesheetId, Guid employeeId, Guid orgId, TimesheetCorrectionDto dto)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<Guid> { Success = false, Message = "Timesheet ID cannot be empty." };
        if (employeeId == Guid.Empty)
            return new ServiceResponse<Guid> { Success = false, Message = "Employee ID cannot be empty." };
        if (dto == null)
            return new ServiceResponse<Guid> { Success = false, Message = "Request body is required." };

        var period = await _repo.GetPeriodAsync(timesheetId);
        if (period == null)
            return new ServiceResponse<Guid> { Success = false, Message = "Timesheet not found." };

        if (period.Status is "Locked" or "Archived")
            return new ServiceResponse<Guid> { Success = false, Message = "Cannot submit corrections on a locked or archived period." };

        var config = await _repo.GetConfigAsync(orgId);
        if (config != null && config.CorrectionDeadlineDays > 0 && dto.WorkDate.AddDays(config.CorrectionDeadlineDays) < DateTime.UtcNow.Date)
            return new ServiceResponse<Guid> { Success = false, Message = "The correction deadline has passed." };

        // Guard: if an attendanceId is provided, verify it belongs to the authenticated employee.
        // This prevents an employee from submitting a correction for another employee's attendance row.
        if (dto.AttendanceId.HasValue)
        {
            var ownsRecord = await _repo.IsAttendanceOwnedByEmployeeAsync(dto.AttendanceId.Value, employeeId);
            if (!ownsRecord)
                return new ServiceResponse<Guid> { Success = false, Message = "The attendance record does not belong to you." };
        }

        // Guard: if attendanceId is null (absent day), verify the employee has NO other attendance
        // record on the same date that belongs to a different employee — this catches cases where
        // the frontend sends a null attendanceId but the date actually belongs to someone else.
        // Also, if the DTO carries an employeeId that differs from the auth employee, block it.
        if (dto.EmployeeId != Guid.Empty && dto.EmployeeId != employeeId)
        {
            return new ServiceResponse<Guid> { Success = false, Message = "You cannot submit corrections for another employee." };
        }

        // Guard: reject correction on a manager-overridden record (mirrors fn_submit_attendance_request)
        var isOverridden = await _repo.IsRecordOverriddenAsync(dto.AttendanceId, employeeId, dto.WorkDate);
        if (isOverridden)
            return new ServiceResponse<Guid> { Success = false, Message = "This record has been overridden by a manager and cannot be corrected." };

        var id = await _repo.CreateCorrectionAsync(timesheetId, employeeId, orgId, dto);
        if (!id.HasValue)
            return new ServiceResponse<Guid> { Success = false, Message = "Failed to create correction." };
        return new ServiceResponse<Guid> { Success = true, Data = id.Value };
    }

    public async Task<ServiceResponse<bool>> ApproveCorrectionAsync(Guid correctionId, Guid managerId)
    {
        if (correctionId == Guid.Empty)
            return new ServiceResponse<bool> { Success = false, Message = "Correction ID cannot be empty." };

        // Get the correction to check employee ownership (before lock — fast path check)
        var correction = await _repo.GetCorrectionAsync(correctionId);
        if (correction == null)
            return new ServiceResponse<bool> { Success = false, Message = "Correction not found." };

        // Guard: self-approval — manager cannot approve their own correction
        if (correction.EmployeeId == managerId)
            return new ServiceResponse<bool> { Success = false, ErrorCode = TimesheetErrorCodes.SelfApproval, Message = "You cannot approve your own correction request." };

        var result = await _repo.ApproveCorrectionAsync(correctionId, managerId);
        if (!result.Success && result.ErrorCode == TimesheetErrorCodes.AlreadyActioned)
            return new ServiceResponse<bool>
            {
                Success = false,
                ErrorCode = TimesheetErrorCodes.AlreadyActioned,
                Message = result.Message,
                Data = false
            };
        if (!result.Success)
            return new ServiceResponse<bool> { Success = false, Message = result.Message ?? "Correction not found or already processed." };
        return new ServiceResponse<bool> { Success = true, Data = true };
    }

    public async Task<ServiceResponse<bool>> RejectCorrectionAsync(Guid correctionId, Guid managerId, string reason)
    {
        if (correctionId == Guid.Empty)
            return new ServiceResponse<bool> { Success = false, Message = "Correction ID cannot be empty." };
        if (string.IsNullOrWhiteSpace(reason))
            return new ServiceResponse<bool> { Success = false, Message = "Rejection reason is required." };

        // Get the correction to check employee ownership
        var correction = await _repo.GetCorrectionAsync(correctionId);
        if (correction == null)
            return new ServiceResponse<bool> { Success = false, Message = "Correction not found." };

        // Guard: self-rejection — manager cannot reject their own correction
        if (correction.EmployeeId == managerId)
            return new ServiceResponse<bool> { Success = false, ErrorCode = TimesheetErrorCodes.SelfApproval, Message = "You cannot reject your own correction request." };

        var result = await _repo.RejectCorrectionAsync(correctionId, managerId, reason);
        if (!result.Success && result.ErrorCode == TimesheetErrorCodes.AlreadyActioned)
            return new ServiceResponse<bool>
            {
                Success = false,
                ErrorCode = TimesheetErrorCodes.AlreadyActioned,
                Message = result.Message,
                Data = false
            };
        if (!result.Success)
            return new ServiceResponse<bool> { Success = false, Message = result.Message ?? "Correction not found or already processed." };
        return new ServiceResponse<bool> { Success = true, Data = true };
    }

    public async Task<ServiceResponse<BulkApproveResult>> ApproveBulkCorrectionsAsync(TimesheetBulkApproveDto dto, Guid managerId)
    {
        if (dto == null || dto.CorrectionIds == null || !dto.CorrectionIds.Any())
            return new ServiceResponse<BulkApproveResult> { Success = false, Message = "No correction IDs provided." };

        // Pre-flight: self-approval check for all items
        foreach (var cid in dto.CorrectionIds)
        {
            var correction = await _repo.GetCorrectionAsync(cid);
            if (correction != null && correction.EmployeeId == managerId)
            {
                return new ServiceResponse<BulkApproveResult>
                {
                    Success = false,
                    Message = "Bulk approval failed. No changes were applied.",
                    ErrorCode = TimesheetErrorCodes.SelfApproval,
                    Data = new BulkApproveResult
                    {
                        Success = false,
                        Message = "Bulk approval failed. No changes were applied.",
                        FailedItem = new FailedCorrectionItem
                        {
                            CorrectionId = cid,
                            EmployeeName = "You",
                            WorkDate = DateOnly.FromDateTime(correction.WorkDate),
                            Reason = "You cannot approve your own correction request."
                        }
                    }
                };
            }
        }

        var result = await _repo.ApproveBulkCorrectionsAsync(dto.CorrectionIds, managerId);
        if (!result.Success)
            return new ServiceResponse<BulkApproveResult> { Success = false, Message = result.Message ?? "Bulk approval failed.", Data = result };

        return new ServiceResponse<BulkApproveResult> { Success = true, Data = result };
    }

    // ── Overrides ─────────────────────────────────────────────────────────
    public async Task<ServiceResponse<bool>> ApplyOverrideAsync(Guid timesheetId, Guid managerId, Guid orgId, TimesheetOverrideDto dto)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<bool> { Success = false, Message = "Timesheet ID cannot be empty." };
        if (dto == null)
            return new ServiceResponse<bool> { Success = false, Message = "Request body is required." };
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return new ServiceResponse<bool> { Success = false, Message = "Override reason is required." };

        var period = await _repo.GetPeriodAsync(timesheetId);
        if (period == null)
            return new ServiceResponse<bool> { Success = false, Message = "Timesheet not found." };
        if (period.Status is "Locked" or "Archived")
            return new ServiceResponse<bool> { Success = false, Message = "Cannot override on a locked or archived period." };

        await _repo.ApplyOverrideAsync(timesheetId, managerId, orgId, dto);
        return new ServiceResponse<bool> { Success = true, Data = true };
    }

    // ── Finalization ─────────────────────────────────────────────────────
    public async Task<ServiceResponse<int>> FinalizeBatchAsync(Guid timesheetId, Guid orgId, Guid actorId, Guid? employeeId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<int> { Success = false, Message = "Timesheet ID cannot be empty." };
        var period = await _repo.GetPeriodAsync(timesheetId);
        if (period == null)
            return new ServiceResponse<int> { Success = false, Message = "Timesheet not found." };

        if (period.Status is "Finalized" or "Locked" or "Archived")
            return new ServiceResponse<int> { Success = false, Message = "This period is already finalized or locked." };

        var pendingCount = await _repo.CountPendingCorrectionsAsync(timesheetId);
        if (pendingCount > 0)
            return new ServiceResponse<int> { Success = false, Message = $"{pendingCount} pending correction(s) must be resolved before finalizing." };

        var count = await _repo.FinalizeBatchAsync(timesheetId, orgId, actorId, employeeId);
        return new ServiceResponse<int> { Success = true, Data = count };
    }

    // ── Payroll Bridge ──────────────────────────────────────────────────────
    public async Task<ServiceResponse<List<TimesheetPeriodLinkDto>>> GetFinalizedForPayrollAsync(Guid orgId)
    {
        if (orgId == Guid.Empty)
            return new ServiceResponse<List<TimesheetPeriodLinkDto>> { Success = false, Message = "Organization ID cannot be empty." };
        var links = (await _repo.GetFinalizedTimesheetLinksAsync(orgId)).ToList();
        return new ServiceResponse<List<TimesheetPeriodLinkDto>> { Success = true, Data = links };
    }

    public async Task<ServiceResponse<TimesheetPayrollSummaryDto>> GetPayrollSummaryAsync(Guid timesheetId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<TimesheetPayrollSummaryDto> { Success = false, Message = "Timesheet ID cannot be empty." };
        var summary = await _repo.GetPayrollSummaryAsync(timesheetId);
        if (summary == null)
            return new ServiceResponse<TimesheetPayrollSummaryDto> { Success = false, Message = "Payroll summary not found." };
        return new ServiceResponse<TimesheetPayrollSummaryDto> { Success = true, Data = summary };
    }

    public async Task<ServiceResponse<bool>> LockForPayrollAsync(Guid timesheetId, Guid lockedBy, Guid? payrollPeriodId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<bool> { Success = false, Message = "Timesheet ID cannot be empty." };
        await _repo.LockForPayrollAsync(timesheetId, lockedBy, payrollPeriodId);
        return new ServiceResponse<bool> { Success = true, Data = true };
    }

    // ── Audit ───────────────────────────────────────────────────────────────
    public async Task<ServiceResponse<List<TimesheetAuditEntryDto>>> GetAuditLogAsync(Guid timesheetId)
    {
        if (timesheetId == Guid.Empty)
            return new ServiceResponse<List<TimesheetAuditEntryDto>> { Success = false, Message = "Timesheet ID cannot be empty." };
        var log = (await _repo.GetAuditLogAsync(timesheetId)).ToList();
        return new ServiceResponse<List<TimesheetAuditEntryDto>> { Success = true, Data = log };
    }

    // ── Helper ──────────────────────────────────────────────────────────────
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        ["Draft"] = new[] { "InProgress", "Archived" },
        ["InProgress"] = new[] { "Submitted", "Archived" },
        ["Submitted"] = new[] { "UnderReview", "InProgress" },
        ["UnderReview"] = new[] { "Approved", "Submitted" },
        ["Approved"] = new[] { "Finalized" },
        ["Finalized"] = new[] { "Locked", "Archived" },
        ["Locked"] = new[] { "Archived" }
    };

    private static string[] GetNextActions(string currentStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var actions) ? actions : Array.Empty<string>();
    }


    // ═══ Phase 2: Projects ═══════════════════════════════════════
    public async Task<ServiceResponse<List<TimesheetProjectDto>>> GetProjectsAsync(Guid organizationId)
    {
        var projects = await _repo.GetProjectsAsync(organizationId);
        return new ServiceResponse<List<TimesheetProjectDto>> { Success = true, Data = projects };
    }

    public async Task<ServiceResponse<Guid>> CreateProjectAsync(TimesheetProjectDto dto)
    {
        var id = await _repo.CreateProjectAsync(dto);
        return new ServiceResponse<Guid> { Success = true, Data = id, Message = "Project created" };
    }

    public async Task<ServiceResponse<List<TimesheetTaskDto>>> GetProjectTasksAsync(Guid projectId)
    {
        var tasks = await _repo.GetProjectTasksAsync(projectId);
        return new ServiceResponse<List<TimesheetTaskDto>> { Success = true, Data = tasks };
    }

    // ═══ Phase 2: Time Allocations ══════════════════════════════
    public async Task<ServiceResponse<List<TimeAllocationDto>>> GetTimeAllocationsAsync(Guid timesheetId, Guid employeeId)
    {
        var allocations = await _repo.GetTimeAllocationsAsync(timesheetId, employeeId);
        return new ServiceResponse<List<TimeAllocationDto>> { Success = true, Data = allocations };
    }

    public async Task<ServiceResponse<bool>> SaveTimeAllocationsAsync(SaveTimeAllocationDto dto)
    {
        await _repo.SaveTimeAllocationsAsync(dto);
        return new ServiceResponse<bool> { Success = true, Data = true, Message = "Time allocations saved" };
    }

    // ═══ Phase 3: Comp Time ═════════════════════════════════════
    public async Task<ServiceResponse<CompTimeBalanceDto>> GetCompTimeBalanceAsync(Guid employeeId, Guid organizationId, int year)
    {
        var balance = await _repo.GetCompTimeBalanceAsync(employeeId, organizationId, year);
        return new ServiceResponse<CompTimeBalanceDto> { Success = true, Data = balance };
    }

    // ═══ Phase 4: Dashboard ═════════════════════════════════════
    public async Task<ServiceResponse<TimesheetDashboardDto>> GetDashboardAsync(Guid organizationId, Guid? periodId)
    {
        var dashboard = await _repo.GetDashboardAsync(organizationId, periodId);
        return new ServiceResponse<TimesheetDashboardDto> { Success = true, Data = dashboard };
    }

    // ═══ Phase 5: Delegation ════════════════════════════════════
    public async Task<ServiceResponse<TimesheetDelegationDto?>> GetActiveDelegationAsync(Guid approverId)
    {
        var delegation = await _repo.GetActiveDelegationAsync(approverId);
        return new ServiceResponse<TimesheetDelegationDto?> { Success = true, Data = delegation };
    }

    public async Task<ServiceResponse<Guid>> CreateDelegationAsync(CreateTimesheetDelegationDto dto, Guid delegatedFrom)
    {
        var id = await _repo.CreateDelegationAsync(dto, delegatedFrom);
        return new ServiceResponse<Guid> { Success = true, Data = id, Message = "Delegation created" };
    }

    // ═══ Phase 5: Rate Cards ════════════════════════════════════
    public async Task<ServiceResponse<List<EmployeeRateCardDto>>> GetRateCardsAsync(Guid organizationId)
    {
        var cards = await _repo.GetRateCardsAsync(organizationId);
        return new ServiceResponse<List<EmployeeRateCardDto>> { Success = true, Data = cards };
    }

    // ═══ Phase 5: Bulk Operations ═══════════════════════════════
    public async Task<ServiceResponse<bool>> BulkCopyFromPreviousAsync(BulkCopyRequestDto dto)
    {
        await _repo.BulkCopyFromPreviousAsync(dto);
        return new ServiceResponse<bool> { Success = true, Data = true, Message = "Records copied successfully" };
    }

    public async Task<ServiceResponse<bool>> AutoFillFromScheduleAsync(BulkAutoFillRequestDto dto)
    {
        await _repo.AutoFillFromScheduleAsync(dto);
        return new ServiceResponse<bool> { Success = true, Data = true, Message = "Schedule auto-filled" };
    }
}