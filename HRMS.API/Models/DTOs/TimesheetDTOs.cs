using System.Text.Json.Serialization;

namespace HRMS.API.Models.DTOs;

public class TimesheetConfigDto
{
    [JsonPropertyName("configId")]
    public Guid ConfigId { get; set; }

    [JsonPropertyName("organizationId")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("requiresManagerApproval")]
    public bool RequiresManagerApproval { get; set; } = true;

    [JsonPropertyName("requiresHrApproval")]
    public bool RequiresHrApproval { get; set; }

    [JsonPropertyName("allowEmployeeCorrections")]
    public bool AllowEmployeeCorrections { get; set; } = true;

    [JsonPropertyName("correctionDeadlineDays")]
    public int CorrectionDeadlineDays { get; set; } = 7;

    [JsonPropertyName("workingDays")]
    public string[] WorkingDays { get; set; } = new[] { "Mon", "Tue", "Wed", "Thu", "Fri" };

    [JsonPropertyName("halfDayThresholdHours")]
    public decimal HalfDayThresholdHours { get; set; } = 4.0m;

    [JsonPropertyName("fullDayThresholdHours")]
    public decimal FullDayThresholdHours { get; set; } = 8.0m;

    [JsonPropertyName("lateGraceMinutes")]
    public int LateGraceMinutes { get; set; } = 15;

    [JsonPropertyName("autoFinalizeOnLock")]
    public bool AutoFinalizeOnLock { get; set; } = true;

    [JsonPropertyName("overtimeMultiplierRegular")]
    public decimal OvertimeMultiplierRegular { get; set; } = 1.50m;

    [JsonPropertyName("overtimeMultiplierWeekend")]
    public decimal OvertimeMultiplierWeekend { get; set; } = 2.00m;

    [JsonPropertyName("overtimeMultiplierHoliday")]
    public decimal OvertimeMultiplierHoliday { get; set; } = 3.00m;

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "UTC";

    [JsonPropertyName("shiftStartHour")]
    public int ShiftStartHour { get; set; } = 9;

    [JsonPropertyName("shiftStartMinute")]
    public int ShiftStartMinute { get; set; }
}

public class CreateTimesheetPeriodDto
{
    [JsonPropertyName("periodName")]
    public string PeriodName { get; set; } = string.Empty;

    [JsonPropertyName("periodStart")]
    public DateTime PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    [JsonPropertyName("month")]
    public int? Month { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("scopeType")]
    public string ScopeType { get; set; } = "all";

    [JsonPropertyName("departmentIds")]
    public List<Guid>? DepartmentIds { get; set; }

    [JsonPropertyName("employeeIds")]
    public List<Guid>? EmployeeIds { get; set; }
}

public class TimesheetPeriodDto
{
    [JsonPropertyName("timesheetId")]
    public Guid TimesheetId { get; set; }

    [JsonPropertyName("timesheetName")]
    public string TimesheetName { get; set; } = string.Empty;

    [JsonPropertyName("periodStart")]
    public DateTime PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Draft";

    [JsonPropertyName("totalEmployees")]
    public int TotalEmployees { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("finalizedAt")]
    public DateTime? FinalizedAt { get; set; }

    [JsonPropertyName("lockedAt")]
    public DateTime? LockedAt { get; set; }

    [JsonPropertyName("nextActions")]
    public string[] NextActions { get; set; } = Array.Empty<string>();

    [JsonPropertyName("scopeType")]
    public string ScopeType { get; set; } = "all";

    [JsonPropertyName("scopeLabel")]
    public string? ScopeLabel { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}

public class TimesheetTransitionDto
{
    [JsonPropertyName("newStatus")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class TimesheetLockBlockersDto
{
    [JsonPropertyName("canLock")]
    public bool CanLock { get; set; }

    [JsonPropertyName("blockers")]
    public List<string> Blockers { get; set; } = new();
}

public class TimesheetCorrectionDto
{
    [JsonPropertyName("timesheetId")]
    public Guid TimesheetId { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("attendanceId")]
    public Guid? AttendanceId { get; set; }

    [JsonPropertyName("workDate")]
    public DateTime WorkDate { get; set; }

    [JsonPropertyName("requestedStatus")]
    public string? RequestedStatus { get; set; }

    [JsonPropertyName("requestedCheckIn")]
    public DateTime? RequestedCheckIn { get; set; }

    [JsonPropertyName("requestedCheckOut")]
    public DateTime? RequestedCheckOut { get; set; }

    [JsonPropertyName("requestedOvertimeHours")]
    public decimal? RequestedOvertimeHours { get; set; }

    [JsonPropertyName("requestedOvertimeType")]
    public string? RequestedOvertimeType { get; set; }

    [JsonPropertyName("requestedLateMinutes")]
    public int? RequestedLateMinutes { get; set; }

    [JsonPropertyName("requestedLeaveTypeId")]
    public Guid? RequestedLeaveTypeId { get; set; }

    [JsonPropertyName("requestedNotes")]
    public string? RequestedNotes { get; set; }

    [JsonPropertyName("reasonForEdit")]
    public string ReasonForEdit { get; set; } = string.Empty;

    [JsonPropertyName("idempotencyKey")]
    public Guid? IdempotencyKey { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class TimesheetBulkApproveDto
{
    [JsonPropertyName("correctionIds")]
    public List<Guid> CorrectionIds { get; set; } = new();
}

public class BulkApproveResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("failedItem")]
    public FailedCorrectionItem? FailedItem { get; set; }
}

public class FailedCorrectionItem
{
    [JsonPropertyName("correctionId")]
    public Guid CorrectionId { get; set; }

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = "";

    [JsonPropertyName("workDate")]
    public DateOnly WorkDate { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}

public class CorrectionActionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("currentStatus")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("actionedBy")]
    public string? ActionedBy { get; set; }

    [JsonPropertyName("actionedAt")]
    public string? ActionedAt { get; set; }
}

public class TimesheetOverrideDto
{
    [JsonPropertyName("attendanceId")]
    public Guid? AttendanceId { get; set; }

    [JsonPropertyName("workDate")]
    public DateTime WorkDate { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("checkInTime")]
    public DateTimeOffset? CheckInTime { get; set; }

    [JsonPropertyName("checkOutTime")]
    public DateTimeOffset? CheckOutTime { get; set; }

    [JsonPropertyName("overtimeHours")]
    public decimal? OvertimeHours { get; set; }

    [JsonPropertyName("overtimeType")]
    public string? OvertimeType { get; set; }

    [JsonPropertyName("lateMinutes")]
    public int? LateMinutes { get; set; }

    [JsonPropertyName("leaveTypeId")]
    public Guid? LeaveTypeId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class TimesheetAuditEntryDto
{
    [JsonPropertyName("auditId")]
    public Guid AuditId { get; set; }

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("actorRole")]
    public string ActorRole { get; set; } = string.Empty;

    [JsonPropertyName("oldValues")]
    public string? OldValues { get; set; }

    [JsonPropertyName("newValues")]
    public string? NewValues { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class PendingCorrectionDto
{
    [JsonPropertyName("requestId")]
    public Guid RequestId { get; set; }

    [JsonPropertyName("attendanceId")]
    public Guid? AttendanceId { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = string.Empty;

    [JsonPropertyName("timesheetId")]
    public Guid TimesheetId { get; set; }

    [JsonPropertyName("workDate")]
    public DateTime WorkDate { get; set; }

    // --- Original attendance values (from attendance.attendance) ---
    [JsonPropertyName("originalStatus")]
    public string? OriginalStatus { get; set; }

    [JsonPropertyName("originalCheckIn")]
    public DateTime? OriginalCheckIn { get; set; }

    [JsonPropertyName("originalCheckOut")]
    public DateTime? OriginalCheckOut { get; set; }

    [JsonPropertyName("originalOvertimeHours")]
    public decimal? OriginalOvertimeHours { get; set; }

    [JsonPropertyName("originalOvertimeType")]
    public string? OriginalOvertimeType { get; set; }

    [JsonPropertyName("originalLateMinutes")]
    public int? OriginalLateMinutes { get; set; }

    [JsonPropertyName("originalLeaveTypeId")]
    public Guid? OriginalLeaveTypeId { get; set; }

    [JsonPropertyName("originalLeaveTypeName")]
    public string? OriginalLeaveTypeName { get; set; }

    // --- Requested corrections (from attendance_update_requests) ---
    [JsonPropertyName("requestedStatus")]
    public string? RequestedStatus { get; set; }

    [JsonPropertyName("requestedCheckIn")]
    public DateTime? RequestedCheckIn { get; set; }

    [JsonPropertyName("requestedCheckOut")]
    public DateTime? RequestedCheckOut { get; set; }

    [JsonPropertyName("requestedOvertimeHours")]
    public decimal? RequestedOvertimeHours { get; set; }

    [JsonPropertyName("requestedOvertimeType")]
    public string? RequestedOvertimeType { get; set; }

    [JsonPropertyName("requestedLateMinutes")]
    public int? RequestedLateMinutes { get; set; }

    [JsonPropertyName("requestedLeaveTypeId")]
    public Guid? RequestedLeaveTypeId { get; set; }

    [JsonPropertyName("requestedLeaveTypeName")]
    public string? RequestedLeaveTypeName { get; set; }

    [JsonPropertyName("requestedNotes")]
    public string? RequestedNotes { get; set; }

    [JsonPropertyName("reasonForEdit")]
    public string ReasonForEdit { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}


// ══════════════════════════════════════════════════════════════════
// Phase 2-5: Project, Time Allocation, Comp Time, Dashboard,
//           Delegation, Rate Cards, Bulk Operations DTOs
// ══════════════════════════════════════════════════════════════════

// --- Projects ---
public class TimesheetProjectDto
{
    [JsonPropertyName("projectId")] public Guid ProjectId { get; set; }
    [JsonPropertyName("organizationId")] public Guid OrganizationId { get; set; }
    [JsonPropertyName("projectName")] public string ProjectName { get; set; } = "";
    [JsonPropertyName("projectCode")] public string? ProjectCode { get; set; }
    [JsonPropertyName("clientName")] public string? ClientName { get; set; }
    [JsonPropertyName("costCenterCode")] public string? CostCenterCode { get; set; }
    [JsonPropertyName("glAccountCode")] public string? GlAccountCode { get; set; }
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("startDate")] public DateTime? StartDate { get; set; }
    [JsonPropertyName("endDate")] public DateTime? EndDate { get; set; }
}

public class TimesheetTaskDto
{
    [JsonPropertyName("taskId")] public Guid TaskId { get; set; }
    [JsonPropertyName("projectId")] public Guid ProjectId { get; set; }
    [JsonPropertyName("taskName")] public string TaskName { get; set; } = "";
    [JsonPropertyName("isBillableDefault")] public bool IsBillableDefault { get; set; }
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("hourlyRate")] public decimal? HourlyRate { get; set; }
}

// --- Time Allocations ---
public class TimeAllocationDto
{
    [JsonPropertyName("allocationId")] public Guid AllocationId { get; set; }
    [JsonPropertyName("attendanceId")] public Guid? AttendanceId { get; set; }
    [JsonPropertyName("timesheetId")] public Guid TimesheetId { get; set; }
    [JsonPropertyName("employeeId")] public Guid EmployeeId { get; set; }
    [JsonPropertyName("workDate")] public DateTime WorkDate { get; set; }
    [JsonPropertyName("projectId")] public Guid ProjectId { get; set; }
    [JsonPropertyName("taskId")] public Guid TaskId { get; set; }
    [JsonPropertyName("projectName")] public string? ProjectName { get; set; }
    [JsonPropertyName("taskName")] public string? TaskName { get; set; }
    [JsonPropertyName("hours")] public decimal Hours { get; set; }
    [JsonPropertyName("isBillable")] public bool IsBillable { get; set; }
    [JsonPropertyName("billingRate")] public decimal? BillingRate { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("costCenterCode")] public string? CostCenterCode { get; set; }
}

public class SaveTimeAllocationDto
{
    [JsonPropertyName("timesheetId")] public Guid TimesheetId { get; set; }
    [JsonPropertyName("entries")] public List<TimeAllocationEntry> Entries { get; set; } = new();
}

public class TimeAllocationEntry
{
    [JsonPropertyName("attendanceId")] public Guid? AttendanceId { get; set; }
    [JsonPropertyName("employeeId")] public Guid EmployeeId { get; set; }
    [JsonPropertyName("workDate")] public DateTime WorkDate { get; set; }
    [JsonPropertyName("projectId")] public Guid ProjectId { get; set; }
    [JsonPropertyName("taskId")] public Guid TaskId { get; set; }
    [JsonPropertyName("hours")] public decimal Hours { get; set; }
    [JsonPropertyName("isBillable")] public bool IsBillable { get; set; }
    [JsonPropertyName("billingRate")] public decimal? BillingRate { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("costCenterCode")] public string? CostCenterCode { get; set; }
}

// --- Comp Time / OT Bank ---
public class CompTimeBalanceDto
{
    [JsonPropertyName("compTimeId")] public Guid CompTimeId { get; set; }
    [JsonPropertyName("employeeId")] public Guid EmployeeId { get; set; }
    [JsonPropertyName("employeeName")] public string EmployeeName { get; set; } = "";
    [JsonPropertyName("year")] public int Year { get; set; }
    [JsonPropertyName("overtimeHoursBanked")] public decimal OvertimeHoursBanked { get; set; }
    [JsonPropertyName("overtimeHoursUsed")] public decimal OvertimeHoursUsed { get; set; }
    [JsonPropertyName("overtimeHoursAvailable")] public decimal OvertimeHoursAvailable { get; set; }
    [JsonPropertyName("conversionRatio")] public decimal ConversionRatio { get; set; }
    [JsonPropertyName("expiresAt")] public DateTime? ExpiresAt { get; set; }
}

public class OvertimeBreakdownDto
{
    [JsonPropertyName("timesheetId")] public Guid TimesheetId { get; set; }
    [JsonPropertyName("employeeId")] public Guid EmployeeId { get; set; }
    [JsonPropertyName("employeeName")] public string EmployeeName { get; set; } = "";
    [JsonPropertyName("totalOvertimeHours")] public decimal TotalOvertimeHours { get; set; }
    [JsonPropertyName("regularOvertime")] public decimal RegularOvertime { get; set; }
    [JsonPropertyName("weekendOvertime")] public decimal WeekendOvertime { get; set; }
    [JsonPropertyName("holidayOvertime")] public decimal HolidayOvertime { get; set; }
    [JsonPropertyName("nightDifferentialHours")] public decimal NightDifferentialHours { get; set; }
    [JsonPropertyName("consecutiveDayOvertime")] public decimal ConsecutiveDayOvertime { get; set; }
}

// --- Dashboard ---
public class TimesheetDashboardDto
{
    [JsonPropertyName("completionRate")] public MetricCardDto CompletionRate { get; set; } = new();
    [JsonPropertyName("avgHoursPerDay")] public MetricCardDto AvgHoursPerDay { get; set; } = new();
    [JsonPropertyName("overtimeTrend")] public MetricCardDto OvertimeTrend { get; set; } = new();
    [JsonPropertyName("pendingApprovals")] public MetricCardDto PendingApprovals { get; set; } = new();
    [JsonPropertyName("departmentUtilization")] public List<DepartmentUtilizationDto> DepartmentUtilization { get; set; } = new();
    [JsonPropertyName("utilizationTrend")] public List<UtilizationTrendDto> UtilizationTrend { get; set; } = new();
    [JsonPropertyName("overtimeReasons")] public List<OvertimeReasonBreakdownDto> OvertimeReasons { get; set; } = new();
}

public class MetricCardDto
{
    [JsonPropertyName("value")] public decimal Value { get; set; }
    [JsonPropertyName("changePercent")] public decimal? ChangePercent { get; set; }
    [JsonPropertyName("trend")] public string? Trend { get; set; }
}

public class DepartmentUtilizationDto
{
    [JsonPropertyName("department")] public string Department { get; set; } = "";
    [JsonPropertyName("scheduled")] public decimal Scheduled { get; set; }
    [JsonPropertyName("actual")] public decimal Actual { get; set; }
    [JsonPropertyName("utilizationPercent")] public decimal UtilizationPercent { get; set; }
}

public class UtilizationTrendDto
{
    [JsonPropertyName("period")] public string Period { get; set; } = "";
    [JsonPropertyName("utilizationPercent")] public decimal UtilizationPercent { get; set; }
    [JsonPropertyName("overtimePercent")] public decimal OvertimePercent { get; set; }
}

public class OvertimeReasonBreakdownDto
{
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("totalHours")] public decimal TotalHours { get; set; }
    [JsonPropertyName("employeeCount")] public int EmployeeCount { get; set; }
}

// --- Delegation ---
public class TimesheetDelegationDto
{
    [JsonPropertyName("delegationId")] public Guid DelegationId { get; set; }
    [JsonPropertyName("delegatedFrom")] public Guid DelegatedFrom { get; set; }
    [JsonPropertyName("delegatedFromName")] public string DelegatedFromName { get; set; } = "";
    [JsonPropertyName("delegatedTo")] public Guid DelegatedTo { get; set; }
    [JsonPropertyName("delegatedToName")] public string DelegatedToName { get; set; } = "";
    [JsonPropertyName("startDate")] public DateTime StartDate { get; set; }
    [JsonPropertyName("endDate")] public DateTime EndDate { get; set; }
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
}

public class CreateTimesheetDelegationDto
{
    [JsonPropertyName("delegatedTo")] public Guid DelegatedTo { get; set; }
    [JsonPropertyName("startDate")] public DateTime StartDate { get; set; }
    [JsonPropertyName("endDate")] public DateTime EndDate { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
}

// --- Rate Cards ---
public class EmployeeRateCardDto
{
    [JsonPropertyName("employeeId")] public Guid EmployeeId { get; set; }
    [JsonPropertyName("employeeName")] public string EmployeeName { get; set; } = "";
    [JsonPropertyName("employeeCode")] public string EmployeeCode { get; set; } = "";
    [JsonPropertyName("standardRate")] public decimal StandardRate { get; set; }
    [JsonPropertyName("overtimeRate")] public decimal OvertimeRate { get; set; }
    [JsonPropertyName("weekendRate")] public decimal WeekendRate { get; set; }
    [JsonPropertyName("holidayRate")] public decimal HolidayRate { get; set; }
    [JsonPropertyName("effectiveFrom")] public DateTime EffectiveFrom { get; set; }
}

// --- Bulk Operations ---
public class BulkCopyRequestDto
{
    [JsonPropertyName("sourceTimesheetId")] public Guid SourceTimesheetId { get; set; }
    [JsonPropertyName("targetTimesheetId")] public Guid TargetTimesheetId { get; set; }
    [JsonPropertyName("employeeIds")] public List<Guid> EmployeeIds { get; set; } = new();
    [JsonPropertyName("copyCorrections")] public bool CopyCorrections { get; set; }
}

public class BulkAutoFillRequestDto
{
    [JsonPropertyName("timesheetId")] public Guid TimesheetId { get; set; }
    [JsonPropertyName("employeeId")] public Guid EmployeeId { get; set; }
    [JsonPropertyName("dates")] public List<DateTime> Dates { get; set; } = new();
    [JsonPropertyName("status")] public string Status { get; set; } = "present";
    [JsonPropertyName("checkInTime")] public string? CheckInTime { get; set; }
    [JsonPropertyName("checkOutTime")] public string? CheckOutTime { get; set; }
}

// ══════════════════════════════════════════════════════════════════
// Error Code Registry — keep in sync with frontend TimesheetErrorCodes
// ══════════════════════════════════════════════════════════════════
public static class TimesheetErrorCodes
{
    public const string AlreadyActioned  = "ALREADY_ACTIONED";
    public const string OverrideLocked   = "OVERRIDE_LOCKED";
    public const string SelfApproval     = "SELF_APPROVAL";
    public const string Unauthorized     = "UNAUTHORIZED_APPROVER";
    public const string DeadlineExceeded = "DEADLINE_EXCEEDED";
    public const string PendingBlocks    = "PENDING_CORRECTIONS_EXIST";
}