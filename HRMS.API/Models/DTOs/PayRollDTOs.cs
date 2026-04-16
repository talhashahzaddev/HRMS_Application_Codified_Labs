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
    public PayRollPolicySectionDto BonusPolicy { get; set; } = new();
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

public class LeaveRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnpaidMultiplier { get; set; }
    public decimal HalfPaidMultiplier { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLeaveRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnpaidMultiplier { get; set; } = 1.00m;
    public decimal HalfPaidMultiplier { get; set; } = 0.50m;
    public bool IsActive { get; set; } = true;
}

public class UpdateLeaveRuleDto : CreateLeaveRuleDto
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

public class LateAttendanceDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public int LateMinutes { get; set; }
    public decimal Deduction { get; set; }
    public bool IsGrace { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLateAttendanceDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public int LateMinutes { get; set; }
    public decimal Deduction { get; set; }
    public bool IsGrace { get; set; }
    public DateTime AttendanceDate { get; set; }
}

public class UpdateLateAttendanceDto
{
    public Guid? RuleId { get; set; }
    public int LateMinutes { get; set; }
    public decimal Deduction { get; set; }
    public bool IsGrace { get; set; }
    public DateTime AttendanceDate { get; set; }
}

public class LateAttendanceFilterDto
{
    public string? EmployeeName { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class LeaveSummaryDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public decimal HalfPaidDays { get; set; }
    public decimal UnpaidDeduction { get; set; }
    public decimal HalfPaidDeduction { get; set; }
    public decimal TotalLeaveDays { get; set; }
    public decimal TotalDeduction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLeaveSummaryDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public decimal HalfPaidDays { get; set; }
    public decimal UnpaidDeduction { get; set; }
    public decimal HalfPaidDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
}

public class UpdateLeaveSummaryDto
{
    public Guid? RuleId { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public decimal HalfPaidDays { get; set; }
    public decimal UnpaidDeduction { get; set; }
    public decimal HalfPaidDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
}

public class LeaveSummaryFilterDto
{
    public string? EmployeeName { get; set; }
    public Guid? RuleId { get; set; }
    public Guid? PeriodId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PerformanceRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePerformanceRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePerformanceRuleDto : CreatePerformanceRuleDto
{
}

public class BonusRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BonusType { get; set; } = string.Empty;
    public decimal? BonusAmount { get; set; }
    public decimal? BonusPercentage { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBonusRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BonusType { get; set; } = string.Empty;
    public decimal? BonusAmount { get; set; }
    public decimal? BonusPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateBonusRuleDto : CreateBonusRuleDto
{
}

public class PerformancePayDto
{
    public Guid PerformancePayId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public decimal? Score { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePerformancePayDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal? Score { get; set; }
    public decimal FinalAmount { get; set; }
}

public class UpdatePerformancePayDto
{
    public Guid? RuleId { get; set; }
    public decimal? Score { get; set; }
    public decimal FinalAmount { get; set; }
}

public class PerformancePayFilterDto
{
    public string? EmployeeName { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class BonusEntryDto
{
    public Guid BonusId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public string? RuleDescription { get; set; }
    public decimal BonusAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBonusEntryDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal BonusAmount { get; set; }
}

public class UpdateBonusEntryDto
{
    public Guid? RuleId { get; set; }
    public decimal BonusAmount { get; set; }
}

public class BonusEntryFilterDto
{
    public string? EmployeeName { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? RuleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class LoanRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MaxLoanAmount { get; set; }
    public int? MaxInstallments { get; set; }
    public decimal InterestRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLoanRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MaxLoanAmount { get; set; }
    public int? MaxInstallments { get; set; }
    public decimal InterestRate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateLoanRuleDto : CreateLoanRuleDto
{
}

public class SalaryAdvanceRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MaxPercentage { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSalaryAdvanceRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MaxPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSalaryAdvanceRuleDto : CreateSalaryAdvanceRuleDto
{
}

public class ProvidentFundRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultEmployeePct { get; set; }
    public decimal DefaultEmployerPct { get; set; }
    public string ContributionBasis { get; set; } = "basic";
    public string? WithdrawalConfig { get; set; }
    public bool AllowPartialWithdraw { get; set; }
    public int VestingMonths { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProvidentFundRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultEmployeePct { get; set; }
    public decimal DefaultEmployerPct { get; set; }
    public string ContributionBasis { get; set; } = "basic";
    public string? WithdrawalConfig { get; set; }
    public bool AllowPartialWithdraw { get; set; } = false;
    public int VestingMonths { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateProvidentFundRuleDto : CreateProvidentFundRuleDto
{
}

public class CreateProvidentFundEnrollmentDto
{
    public Guid RuleId { get; set; }
    public decimal EmployeePct { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

public class UpdateProvidentFundPercentageDto
{
    public Guid RuleId { get; set; }
    public decimal EmployeePct { get; set; }
}

public class CreateProvidentFundWithdrawRequestDto
{
    public string WithdrawalType { get; set; } = "temporary"; // temporary | permanent
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class ProvidentFundRequestDto
{
    public Guid RequestId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RuleId { get; set; }
    public string? RuleName { get; set; }

    public Guid? PfId { get; set; }
    public string RequestType { get; set; } = string.Empty; // enrollment | update | withdrawal
    public string Status { get; set; } = string.Empty; // pending | approved | rejected

    public decimal? RequestedEmployeePct { get; set; }
    public decimal? RequestedEmployerPct { get; set; }

    public string? WithdrawalType { get; set; } // temporary | permanent
    public string? Reason { get; set; }
    public decimal? RequestedAmount { get; set; }

    public string? Remarks { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class ProvidentFundRequestFilterDto
{
    public string? Status { get; set; } // pending | approved | rejected
    public string? RequestType { get; set; } // enrollment | update | withdrawal
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ProvidentFundAccountDto
{
    public Guid PfId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RuleId { get; set; }
    public string? RuleName { get; set; }
    public string? ContributionBasis { get; set; }
    public bool AllowPartialWithdraw { get; set; }
    public string? WithdrawalConfig { get; set; }
    public decimal EmployeePct { get; set; }
    public decimal EmployerPct { get; set; }
    public string PfStatus { get; set; } = string.Empty; // active | closed | pending
    public DateTime EffectiveFrom { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProvidentFundEnrollmentDto
{
    public Guid PfId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RuleId { get; set; }
    public string? RuleName { get; set; }
    public decimal EmployeePct { get; set; }
    public decimal EmployerPct { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string PfStatus { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Remarks { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProvidentFundTransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public Guid PfId { get; set; }
    public Guid? PfRequestId { get; set; }
    public decimal SalaryBasisAmount { get; set; }
    public decimal EmployeePct { get; set; }
    public decimal EmployerPct { get; set; }
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class ProvidentFundTransactionFilterDto
{
    public int? Year { get; set; }
    public string? TransactionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AdminProvidentFundRequestDto
{
    public Guid RequestId { get; set; }
    public Guid? PfId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal? BasicSalary { get; set; }
    public Guid RuleId { get; set; }
    public string? RuleName { get; set; }
    public decimal? RequestedEmployeePct { get; set; }
    public decimal? RequestedEmployerPct { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? WithdrawalType { get; set; }
    public string? Reason { get; set; }
    public decimal? RequestedAmount { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Remarks { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class AdminProvidentFundRequestFilterDto
{
    public string? RequestType { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AdminProvidentFundAccountDto
{
    public Guid PfId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal? BasicSalary { get; set; }
    public Guid RuleId { get; set; }
    public string? RuleName { get; set; }
    public decimal EmployeePct { get; set; }
    public decimal EmployerPct { get; set; }
    public string PfStatus { get; set; } = string.Empty; // active | closed | pending
    public DateTime EffectiveFrom { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class AdminProvidentFundAccountFilterDto
{
    public string? PfStatus { get; set; } // active | closed | pending
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AdminProvidentFundRepaymentDto
{
    public Guid TransactionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid PfId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public decimal EmployeePct { get; set; }
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerPct { get; set; }
    public decimal EmployerAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public decimal TotalAmount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string PfStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class AdminProvidentFundRepaymentFilterDto
{
    public string? SearchTerm { get; set; }
    public Guid? PeriodId { get; set; }
    public string? TransactionType { get; set; }
    public string? PfStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ApproveProvidentFundRequestDto
{
    public Guid RequestId { get; set; }
}

public class RejectProvidentFundRequestDto
{
    public string? Remarks { get; set; }
}

public class CreateProvidentFundMonthlyTransactionDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
}

public class ManualProvidentFundEnrollmentDto
{
    public Guid RuleId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal EmployeePct { get; set; }
    public decimal EmployerPct { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

public class SalaryAdvanceDto
{
    public Guid AdvanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string AdvanceStatus { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSalaryAdvanceDto
{
    public Guid? RuleId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class MySalaryAdvanceDto
{
    public Guid AdvanceId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string AdvanceStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? DeductedPeriodId { get; set; }
    public DateTime? DeductedAt { get; set; }
}

public class SalaryAdvanceFilterDto
{
    public string? AdvanceStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AdminSalaryAdvanceDto
{
    public Guid AdvanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string AdvanceStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? DeductedPeriodId { get; set; }
    public DateTime? DeductedAt { get; set; }
}

public class AdminSalaryAdvanceFilterDto
{
    public string? AdvanceStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class RejectSalaryAdvanceDto
{
    public string RejectionReason { get; set; } = string.Empty;
}

public class DisburseSalaryAdvanceDto
{
    public string? DisbursementNote { get; set; }
}

public class PayrollOverviewDto
{
    public List<string> ActiveModules { get; set; } = new();
}

public class LoanDto
{
    public Guid LoanId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? MonthlyInstallment { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public string RepaymentType { get; set; } = string.Empty;
    public string RequestStatus { get; set; } = string.Empty;
    public string LoanStatus { get; set; } = string.Empty;
    public string DisbursementStatus { get; set; } = string.Empty;
    public DateTime? DisbursementDate { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoanReferenceDto
{
    public Guid LoanId { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
}

public class CreateLoanRequestDto
{
    public Guid? RuleId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal? MonthlyInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public string? Description { get; set; }
    public string RepaymentType { get; set; } = "installment";
    public DateTime? ReturnDate { get; set; }
}

public class LoanFilterDto
{
    public string? SearchTerm { get; set; }
    public string? RequestStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UpdateLoanStatusDto
{
    public Guid LoanId { get; set; }
    public string RequestStatus { get; set; } = string.Empty; // accepted, rejected
    public string? RejectionReason { get; set; }
}

public class DisburseLoanDto
{
    public Guid LoanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class MyActiveLoanDto : LoanDto
{
    public int RemainingInstallments { get; set; }
    public decimal CompletedPercentage { get; set; }
    public int TotalCount { get; set; }
}

public class LoanPaymentDto
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? PeriodId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string RepaymentType { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DisbursedLoanFilterDto
{
    public string? SearchTerm { get; set; } // Employee name
    public string? LoanStatus { get; set; } 
    public decimal? MinTotalAmount { get; set; }
    public decimal? MaxTotalAmount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AddLoanPaymentDto
{
    public string ReferenceId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Guid? PeriodId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal InstallmentAmount { get; set; }
    public string RepaymentType { get; set; } = "installment";
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string PaymentMethod { get; set; } = "cash"; // cash, bank_transfer, etc.
}

public class UpdateLoanPaymentDto
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? PeriodId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal InstallmentAmount { get; set; }
    public string RepaymentType { get; set; } = "installment";
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string PaymentMethod { get; set; } = "cash";
    public string PaymentStatus { get; set; } = "settled";
}

public class RepaymentHistoryDto : LoanPaymentDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public decimal LoanTotalAmount { get; set; }
    public string LoanStatus { get; set; } = string.Empty;
    public int TotalCount { get; set; }
}

public class RepaymentFilterDto
{
    public string? SearchTerm { get; set; } // Employee Name
    public string? RepaymentType { get; set; }
    public string? PaymentMethod { get; set; }
    public string? LoanStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class MyLoanHistoryFilterDto
{
    public Guid? LoanId { get; set; }
    public Guid? PeriodId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class MyLoanPaymentDto : LoanPaymentDto
{
    public int TotalCount { get; set; }
}
