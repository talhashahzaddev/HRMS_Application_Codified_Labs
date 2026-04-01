namespace HRMS.API.Models.DTOs;

public class PayRollRuleDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PayRollPolicySectionDto
{
    public int RulesCount { get; set; }
    public List<PayRollRuleDto> Rules { get; set; } = new();
}

public class PayRollRulesGroupedDto
{
    public PayRollPolicySectionDto OvertimePolicy { get; set; } = new();
    public PayRollPolicySectionDto AttendanceDeductionPolicy { get; set; } = new();
    public PayRollPolicySectionDto LateArrivalPolicy { get; set; } = new();
    public PayRollPolicySectionDto LeaveDeductionPolicy { get; set; } = new();
    public PayRollPolicySectionDto PerformanceBonusPolicy { get; set; } = new();
    public PayRollPolicySectionDto EmployeeLoanPolicy { get; set; } = new();
    public PayRollPolicySectionDto SalaryAdvancePolicy { get; set; } = new();
    public PayRollPolicySectionDto ProvidentFundPolicy { get; set; } = new();
    public PayRollPolicySectionDto IncomeTaxPolicy { get; set; } = new();
    public PayRollPolicySectionDto SocialSecurityPolicy { get; set; } = new();
    public PayRollPolicySectionDto GratuityPolicy { get; set; } = new();
}

public class OvertimeRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OvertimeType { get; set; } = string.Empty;
    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOvertimeRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OvertimeType { get; set; } = string.Empty;
    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
}

public class UpdateOvertimeRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OvertimeType { get; set; } = string.Empty;
    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
}

public class OvertimeEntryDto
{
    public Guid OvertimeId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal? FinalAmount { get; set; }
    public string EntryStatus { get; set; } = string.Empty;
    public string OvertimeType { get; set; } = string.Empty;
    public Guid? ApprovedBy { get; set; }
    public DateTime OvertimeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOvertimeEntryDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal? FinalAmount { get; set; }
    public string EntryStatus { get; set; } = "pending";
    public DateTime OvertimeDate { get; set; }
}

public class UpdateOvertimeEntryDto
{
    public Guid? RuleId { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal? FinalAmount { get; set; }
    public string EntryStatus { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime OvertimeDate { get; set; }
}

public class OvertimeEntryFilterDto
{
    public Guid? PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public string? OvertimeType { get; set; }
    public string? EmployeeName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
public class PayrollPeriodDto
{
    public Guid PeriodId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = "open";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePayrollPeriodDto
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = "open";
    public string? Description { get; set; }
}

public class UpdatePayrollPeriodDto
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = "open";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
public class PayrollPeriodPagedResult : PagedResult<PayrollPeriodDto>
{
    public int OpenCount { get; set; }
    public int ProcessedCount { get; set; }
    public int LockedCount { get; set; }
}

public class PayrollPeriodFilterDto
{
    public string? PeriodName { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AttendanceDeductionRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? RuleName { get; set; }
    public string? Description { get; set; }
    public decimal? FixedDeduction { get; set; }
    public decimal? PercentageDeduction { get; set; }
    public decimal HalfDayMultiplier { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LateArrivalRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? FixedDeduction { get; set; }
    public decimal? PercentageDeduction { get; set; }
    public int GraceMinutes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLateArrivalRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? FixedDeduction { get; set; }
    public decimal? PercentageDeduction { get; set; }
    public int GraceMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateLateArrivalRuleDto : CreateLateArrivalRuleDto
{
}

public class CreateAttendanceDeductionRuleDto
{
    public string? RuleName { get; set; }
    public string? Description { get; set; }
    public decimal? FixedDeduction { get; set; }
    public decimal? PercentageDeduction { get; set; }
    public decimal HalfDayMultiplier { get; set; } = 0.50m;
    public bool IsActive { get; set; } = true;
}

public class UpdateAttendanceDeductionRuleDto : CreateAttendanceDeductionRuleDto
{
}

public class PayrollAttendanceSummaryDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public Guid? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public int ScheduledDays { get; set; }
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public decimal EffectiveAbsents { get; set; }
    public decimal AbsentDeduction { get; set; }
    public decimal HalfDayDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePayrollAttendanceSummaryDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    // ShiftId, ScheduledDays, TotalDays are auto-computed in the service from EmployeeId + PeriodId
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public decimal EffectiveAbsents { get; set; }
    public decimal AbsentDeduction { get; set; }
    public decimal HalfDayDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
}

public class UpdatePayrollAttendanceSummaryDto
{
    public Guid? RuleId { get; set; }
    // ShiftId, ScheduledDays, TotalDays are NOT updated via this DTO
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public decimal EffectiveAbsents { get; set; }
    public decimal AbsentDeduction { get; set; }
    public decimal HalfDayDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
}

public class PayrollAttendanceSummaryFilterDto
{
    public string? EmployeeName { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
