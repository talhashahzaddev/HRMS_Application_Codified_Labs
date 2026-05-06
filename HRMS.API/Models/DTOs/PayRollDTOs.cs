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

public class SocialSecurityJurisdictionDto
{
    public Guid JurisdictionId { get; set; }
    public Guid OrganizationId { get; set; }
    public string JurisdictionCode { get; set; } = string.Empty;
    public string JurisdictionName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public string? Timezone { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? Metadata { get; set; }
}

public class SocialSecurityAuthorityDto
{
    public Guid AuthorityId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JurisdictionId { get; set; }
    public string AuthorityCode { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string? PortalUrl { get; set; }
    public string? RemittanceFrequency { get; set; }
    public string? FilingMode { get; set; }
    public string? PaymentMode { get; set; }
    public int? DueOffsetDays { get; set; }
    public bool IsActive { get; set; }
    public string? Metadata { get; set; }
}

public class SocialSecuritySchemeDto
{
    public Guid SchemeId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JurisdictionId { get; set; }
    public Guid? AuthorityId { get; set; }
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string? SchemeType { get; set; }
    public string? MandatoryMode { get; set; }
    public bool EmployeeMandatory { get; set; }
    public bool EmployerMandatory { get; set; }
    public bool AllowVoluntaryTopup { get; set; }
    public bool AllowCrossBorder { get; set; }
    public DateTime VersionStartDate { get; set; }
    public DateTime? VersionEndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Metadata { get; set; }
}

public class SocialSecurityRuleDto
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SchemeId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContributionBasis { get; set; } = "gross";
    public decimal EmployeeDefaultPct { get; set; }
    public decimal EmployerDefaultPct { get; set; }
    public decimal? EmployeeFixedAmount { get; set; }
    public decimal? EmployerFixedAmount { get; set; }
    public decimal? MinSalaryLimit { get; set; }
    public decimal? MaxSalaryLimit { get; set; }
    public decimal? AnnualSalaryCap { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSocialSecurityJurisdictionDto
{
    public string JurisdictionCode { get; set; } = string.Empty;
    public string JurisdictionName { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? Currency { get; set; }
    public string? Timezone { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? Metadata { get; set; }
}

public class UpdateSocialSecurityJurisdictionDto : CreateSocialSecurityJurisdictionDto
{
}

public class CreateSocialSecurityAuthorityDto
{
    public Guid JurisdictionId { get; set; }
    public string AuthorityCode { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string? PortalUrl { get; set; }
    public string? RemittanceFrequency { get; set; }
    public string? FilingMode { get; set; }
    public string? PaymentMode { get; set; }
    public int? DueOffsetDays { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Metadata { get; set; }
}

public class UpdateSocialSecurityAuthorityDto : CreateSocialSecurityAuthorityDto
{
}

public class CreateSocialSecuritySchemeDto
{
    public Guid JurisdictionId { get; set; }
    public Guid? AuthorityId { get; set; }
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string? SchemeType { get; set; }
    public string? MandatoryMode { get; set; }
    public bool EmployeeMandatory { get; set; } = true;
    public bool EmployerMandatory { get; set; } = true;
    public bool AllowVoluntaryTopup { get; set; } = false;
    public bool AllowCrossBorder { get; set; } = false;
    public DateTime? VersionStartDate { get; set; }
    public DateTime? VersionEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Metadata { get; set; }
}

public class UpdateSocialSecuritySchemeDto : CreateSocialSecuritySchemeDto
{
}

public class CreateSocialSecurityRuleDto
{
    public Guid SchemeId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContributionBasis { get; set; } = "gross";
    public decimal EmployeeDefaultPct { get; set; }
    public decimal EmployerDefaultPct { get; set; }
    public decimal? EmployeeFixedAmount { get; set; }
    public decimal? EmployerFixedAmount { get; set; }
    public decimal? MinSalaryLimit { get; set; }
    public decimal? MaxSalaryLimit { get; set; }
    public decimal? AnnualSalaryCap { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSocialSecurityRuleDto : CreateSocialSecurityRuleDto
{
}

public class SocialSecurityConfigDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? JurisdictionId { get; set; }
    public string? JurisdictionCode { get; set; }
    public string? JurisdictionName { get; set; }
    public Guid? AuthorityId { get; set; }
    public string? AuthorityCode { get; set; }
    public string? AuthorityName { get; set; }
    public string? PortalUrl { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeCode { get; set; }
    public string? SchemeName { get; set; }
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public string? ContributionBasis { get; set; }
    public decimal EmployeeContributionPct { get; set; }
    public decimal EmployerContributionPct { get; set; }
    public decimal? EmployeeFixedAmount { get; set; }
    public decimal? EmployerFixedAmount { get; set; }
    public decimal? MinSalaryLimit { get; set; }
    public decimal? MaxSalaryCap { get; set; }
    public string? Currency { get; set; }
    public bool IsStatutory { get; set; }
    public bool AllowVoluntary { get; set; }
    public bool AllowWithdrawal { get; set; }
    public string? Metadata { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSocialSecurityConfigDto
{
    public string ConfigName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? JurisdictionId { get; set; }
    public Guid? AuthorityId { get; set; }
    public Guid? SchemeId { get; set; }
    public Guid? RuleId { get; set; }
    public string? ContributionBasis { get; set; }
    public decimal EmployeeContributionPct { get; set; }
    public decimal EmployerContributionPct { get; set; }
    public decimal? EmployeeFixedAmount { get; set; }
    public decimal? EmployerFixedAmount { get; set; }
    public decimal? MinSalaryLimit { get; set; }
    public decimal? MaxSalaryCap { get; set; }
    public string? Currency { get; set; }
    public bool IsStatutory { get; set; } = true;
    public bool AllowVoluntary { get; set; } = false;
    public bool AllowWithdrawal { get; set; } = true;
    public string? Metadata { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSocialSecurityConfigDto : CreateSocialSecurityConfigDto
{
}

public class SocialSecurityConfigFilterDto
{
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public Guid? JurisdictionId { get; set; }
    public Guid? SchemeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SocialSecurityTransactionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public Guid? ConfigId { get; set; }
    public string? ConfigName { get; set; }
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public Guid? JurisdictionId { get; set; }
    public string? JurisdictionName { get; set; }
    public Guid? AuthorityId { get; set; }
    public string? AuthorityName { get; set; }
    public decimal? ActualSalary { get; set; }
    public decimal? SalaryCapped { get; set; }
    public decimal? ContributionBase { get; set; }
    public string? ContributionBasis { get; set; }
    public decimal? EmployeeRate { get; set; }
    public decimal? EmployerRate { get; set; }
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? TransactionType { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public bool IsEnrolled { get; set; }
    public bool WithdrawalRequest { get; set; }
    public string? WithdrawalStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class CreateSocialSecurityTransactionDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? ConfigId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal ActualSalary { get; set; }
    public bool IsEnrolled { get; set; } = true;
    public string RequestStatus { get; set; } = "pending";
}

public class UpdateSocialSecurityTransactionDto
{
    public Guid? ConfigId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal ActualSalary { get; set; }
    public bool IsEnrolled { get; set; } = true;
    public string RequestStatus { get; set; } = "pending";
}

public class SocialSecurityTransactionFilterDto
{
    public string? SearchTerm { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? ConfigId { get; set; }
    public Guid? SchemeId { get; set; }
    public Guid? RuleId { get; set; }
    public string? RequestStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class MySocialSecurityTransactionFilterDto
{
    public Guid? PeriodId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SocialSecurityConfigCalculationDto
{
    public Guid Id { get; set; }
    public Guid? RuleId { get; set; }
    public Guid? SchemeId { get; set; }
    public Guid? JurisdictionId { get; set; }
    public Guid? AuthorityId { get; set; }
    public decimal EmployeeContributionPct { get; set; }
    public decimal EmployerContributionPct { get; set; }
    public decimal? EmployeeFixedAmount { get; set; }
    public decimal? EmployerFixedAmount { get; set; }
    public decimal? MinSalaryLimit { get; set; }
    public decimal? MaxSalaryCap { get; set; }
    public string ContributionBasis { get; set; } = "gross";
}

public class SocialSecurityRuleCalculationDto
{
    public Guid RuleId { get; set; }
    public Guid? SchemeId { get; set; }
    public decimal EmployeeDefaultPct { get; set; }
    public decimal EmployerDefaultPct { get; set; }
    public decimal? EmployeeFixedAmount { get; set; }
    public decimal? EmployerFixedAmount { get; set; }
    public decimal? MinSalaryLimit { get; set; }
    public decimal? MaxSalaryLimit { get; set; }
    public string ContributionBasis { get; set; } = "gross";
    public DateTime? EffectiveFrom { get; set; }
}

public class ExistingSocialSecurityTransactionDto
{
    public Guid Id { get; set; }
    public Guid? ConfigId { get; set; }
    public Guid? RuleId { get; set; }
}

// ───────────────────────────── Social Security: Enrollments ─────────────────────────────

public class SocialSecurityEnrollmentDto
{
    public Guid EnrollmentId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RuleId { get; set; }
    public string? RuleName { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public Guid? ConfigId { get; set; }
    public string? ConfigName { get; set; }
    public string? ExternalMemberId { get; set; }
    public string EnrollmentType { get; set; } = "auto_enrolled";
    public string EnrollmentStatus { get; set; } = "pending";
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? EmployeeCustomPct { get; set; }
    public decimal? EmployerCustomPct { get; set; }
    public decimal? SalaryCapOverride { get; set; }
    public decimal? RuleEmployeeDefaultPct { get; set; }
    public decimal? RuleEmployerDefaultPct { get; set; }
    public string? ContributionBasis { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ───────────────────────────── Social Security: Enrollment Requests ─────────────────────

public class SocialSecurityEnrollmentRequestDto
{
    public Guid RequestId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? EnrollmentId { get; set; }
    public Guid? RuleId { get; set; }
    public string? RuleName { get; set; }
    public Guid? ConfigId { get; set; }
    public string? ConfigName { get; set; }
    public string RequestType { get; set; } = "enrollment";
    public string RequestStatus { get; set; } = "pending";
    public DateTime RequestedAt { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
    public string? RejectionReason { get; set; }
    public decimal? RequestedEmployeePct { get; set; }
    public decimal? RequestedEmployerPct { get; set; }
    public decimal? RequestedSalaryCap { get; set; }
    public DateTime? RequestedEffectiveDate { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int DocumentCount { get; set; }
    public int TotalCount { get; set; }
}

public class CreateSocialSecurityEnrollmentRequestDto
{
    public Guid? RuleId { get; set; }
    public Guid? ConfigId { get; set; }
    public string RequestType { get; set; } = "enrollment";
    public string? Reason { get; set; }
    public decimal? RequestedEmployeePct { get; set; }
    public decimal? RequestedEmployerPct { get; set; }
    public decimal? RequestedSalaryCap { get; set; }
    public DateTime? RequestedEffectiveDate { get; set; }
    public List<SocialSecurityRequestDocumentInputDto> Documents { get; set; } = new();
}

public class ApproveSocialSecurityEnrollmentRequestDto
{
    public string? Remarks { get; set; }
    public decimal? OverrideEmployeePct { get; set; }
    public decimal? OverrideEmployerPct { get; set; }
    public decimal? OverrideSalaryCap { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class RejectSocialSecurityEnrollmentRequestDto
{
    public string? RejectionReason { get; set; }
}

public class SocialSecurityEnrollmentRequestFilterDto
{
    public string? SearchTerm { get; set; }
    public string? RequestType { get; set; }
    public string? RequestStatus { get; set; }
    public Guid? RuleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class MySocialSecurityRequestFilterDto
{
    public string? RequestType { get; set; }
    public string? RequestStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ───────────────────────────── Social Security: Claims ─────────────────────────────

public class SocialSecurityClaimDto
{
    public Guid ClaimId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? EnrollmentId { get; set; }
    public Guid? RequestId { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimStatus { get; set; } = "draft";
    public DateTime ClaimDate { get; set; }
    public DateTime? IncidentDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public string? Currency { get; set; }
    public decimal? ClaimedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? AuthorityReference { get; set; }
    public string? DecisionNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DocumentCount { get; set; }
    public int TotalCount { get; set; }
}

public class CreateSocialSecurityClaimDto
{
    public string ClaimType { get; set; } = string.Empty;
    public DateTime? IncidentDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public string? Currency { get; set; }
    public decimal? ClaimedAmount { get; set; }
    public string? DecisionNotes { get; set; }
    public List<SocialSecurityRequestDocumentInputDto> Documents { get; set; } = new();
}

public class ApproveSocialSecurityClaimDto
{
    public decimal ApprovedAmount { get; set; }
    public string? DecisionNotes { get; set; }
}

public class RejectSocialSecurityClaimDto
{
    public string? RejectionReason { get; set; }
}

public class MarkSocialSecurityClaimPaidDto
{
    public decimal PaidAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentReference { get; set; }
    public string? AuthorityReference { get; set; }
}

public class SocialSecurityClaimFilterDto
{
    public string? SearchTerm { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class MySocialSecurityClaimFilterDto
{
    public string? ClaimType { get; set; }
    public string? ClaimStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ───────────────────────────── Social Security: Documents ─────────────────────────────

public class SocialSecurityRequestDocumentDto
{
    public Guid DocumentId { get; set; }
    public Guid? RequestId { get; set; }
    public Guid? ClaimId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public string? Issuer { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string VerifiedStatus { get; set; } = "pending";
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SocialSecurityRequestDocumentInputDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public string? Issuer { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class VerifySocialSecurityRequestDocumentDto
{
    public string VerifiedStatus { get; set; } = "verified";
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

public class TaxRegimeDto
{
    public Guid RegimeId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? Country { get; set; }
    public string? RegimeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaxRegimeDto
{
    public string? Country { get; set; }
    public string? RegimeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTaxRegimeDto : CreateTaxRegimeDto
{
}

public class TaxCategoryDto
{
    public Guid CategoryId { get; set; }
    public Guid RegimeId { get; set; }
    public string? RegimeName { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaxCategoryDto
{
    public Guid RegimeId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTaxCategoryDto : CreateTaxCategoryDto
{
}

public class TaxSlabDto
{
    public Guid SlabId { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int SlabOrder { get; set; }
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal Percentage { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaxSlabDto
{
    public Guid CategoryId { get; set; }
    public int SlabOrder { get; set; }
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal? FixedAmount { get; set; } = 0;
    public decimal? Percentage { get; set; } = 0;
}

public class UpdateTaxSlabDto : CreateTaxSlabDto
{
}

public class TaxComponentRuleDto
{
    public Guid ComponentRuleId { get; set; }
    public Guid RegimeId { get; set; }
    public string? RegimeName { get; set; }
    public Guid OrganizationId { get; set; }
    public string? ComponentName { get; set; }
    public string? Taxability { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal LimitPercentage { get; set; }
    public string? ApplyStage { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaxComponentRuleDto
{
    public Guid RegimeId { get; set; }
    public string? ComponentName { get; set; }
    public string? Taxability { get; set; }
    public decimal? LimitAmount { get; set; } = 0;
    public decimal? LimitPercentage { get; set; } = 0;
    public string? ApplyStage { get; set; }
}

public class UpdateTaxComponentRuleDto : CreateTaxComponentRuleDto
{
}

public class TaxTransactionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid PeriodId { get; set; }
    public string? PeriodName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? GrossIncome { get; set; }
    public decimal? TaxableIncome { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? MonthlyTax { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaxTransactionDto
{
    public Guid EmployeeId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? GrossIncome { get; set; }
    public decimal? TaxableIncome { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? MonthlyTax { get; set; }
}

public class UpdateTaxTransactionDto : CreateTaxTransactionDto
{
}

public class TaxTransactionFilterDto
{
    public string? EmployeeName { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
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

public class CalculatePayrollDto
{
    public Guid PeriodId { get; set; }
    public Guid CycleId { get; set; }
    public Guid? BonusRuleId { get; set; }
    public Guid? AttendanceRuleId { get; set; }
    public Guid? LateAttendanceRuleId { get; set; }
    public Guid? LeaveRuleId { get; set; }
}

public class PayrollLoanDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public Guid LoanId { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal NetSalaryAfterLoanDeduction { get; set; }
    public decimal RemainingLoanAmountBefore { get; set; }
    public decimal RemainingLoanAmountAfter { get; set; }
    public int InstallmentNumber { get; set; }
}

public class PayrollSalaryAdvanceDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public Guid AdvanceId { get; set; }
    public decimal DeductedAmount { get; set; }
    public decimal NetSalaryAfterSalaryAdvanceDeduction { get; set; }
}

public class PayrollProvidentFundDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public Guid PfId { get; set; }
    public decimal SalaryBasisAmount { get; set; }
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerAmount { get; set; }
    public decimal NetSalaryAfterProvidentFundDeduction { get; set; }
}

public class PayrollPerformanceBonusDetailDto
{
    public Guid EmployeeId { get; set; }
    public Guid RuleId { get; set; }
    public decimal Score { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal NetSalaryAfterBonus { get; set; }
}

public class PayrollGeneralBonusDetailDto
{
    public Guid EmployeeId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal NetSalaryAfterBonus { get; set; }
}

public class PayrollGratuityDetailDto
{
    public Guid EmployeeId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal GratuityAmount { get; set; }
    public decimal NetSalaryAfterGratuity { get; set; }
}

public class PayrollTaxDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public decimal MonthlyGross { get; set; }
    public decimal PFExemptAmount { get; set; }
    public decimal MonthlyTaxableIncome { get; set; }
    public decimal AnnualTaxableIncome { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal MonthlyTax { get; set; }
    public decimal NetSalaryAfterTaxDeduction { get; set; }
}

public class PayrollAttendanceDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public int ScheduledDays { get; set; }
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public decimal EffectiveAbsents { get; set; }
    public decimal AbsentDeduction { get; set; }
    public decimal HalfDayDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetSalaryAfterAttendanceDeduction { get; set; }
}

public class PayrollCalculationResultDto
{
    public Guid PeriodId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Keep module details separate so future modules can be appended.
    public int EmployeesConsidered { get; set; }
    public int LoanDeductionsApplied { get; set; }
    public decimal TotalLoanDeductionAmount { get; set; }
    public List<PayrollLoanDeductionDetailDto> LoanDeductionDetails { get; set; } = new();

    public int SalaryAdvanceDeductionsApplied { get; set; }
    public decimal TotalSalaryAdvanceDeductionAmount { get; set; }
    public List<PayrollSalaryAdvanceDeductionDetailDto> SalaryAdvanceDeductionDetails { get; set; } = new();

    public int ProvidentFundDeductionsApplied { get; set; }
    public decimal TotalProvidentFundDeductionAmount { get; set; }
    public List<PayrollProvidentFundDeductionDetailDto> ProvidentFundDeductionDetails { get; set; } = new();

    public int PerformanceBonusApplied { get; set; }
    public decimal TotalPerformanceBonusAmount { get; set; }
    public List<PayrollPerformanceBonusDetailDto> PerformanceBonusDetails { get; set; } = new();

    public int GeneralBonusApplied { get; set; }
    public decimal TotalGeneralBonusAmount { get; set; }
    public List<PayrollGeneralBonusDetailDto> GeneralBonusDetails { get; set; } = new();

    public int GratuityApplied { get; set; }
    public decimal TotalGratuityAmount { get; set; }
    public List<PayrollGratuityDetailDto> GratuityDetails { get; set; } = new();

    public int TaxDeductionsApplied { get; set; }
    public decimal TotalTaxDeductionAmount { get; set; }
    public List<PayrollTaxDeductionDetailDto> TaxDeductionDetails { get; set; } = new();

    public int AttendanceDeductionsApplied { get; set; }
    public decimal TotalAttendanceDeductionAmount { get; set; }
    public List<PayrollAttendanceDeductionDetailDto> AttendanceDeductionDetails { get; set; } = new();

    public int LateAttendanceDeductionsApplied { get; set; }
    public decimal TotalLateAttendanceDeductionAmount { get; set; }
    public List<PayrollLateAttendanceDeductionDetailDto> LateAttendanceDeductionDetails { get; set; } = new();

    public int LeaveDeductionsApplied { get; set; }
    public decimal TotalLeaveDeductionAmount { get; set; }
    public List<PayrollLeaveDeductionDetailDto> LeaveDeductionDetails { get; set; } = new();

    public int OvertimeBonusApplied { get; set; }
    public decimal TotalOvertimeBonusAmount { get; set; }
    public decimal TotalRegularOvertimeAmount { get; set; }
    public decimal TotalWeekendOvertimeAmount { get; set; }
    public decimal TotalHolidayOvertimeAmount { get; set; }
    public List<PayrollOvertimeDetailDto> OvertimeDetails { get; set; } = new();

    public decimal TotalDeductions => TotalLoanDeductionAmount + TotalSalaryAdvanceDeductionAmount + TotalProvidentFundDeductionAmount + TotalTaxDeductionAmount + TotalAttendanceDeductionAmount + TotalLateAttendanceDeductionAmount + TotalLeaveDeductionAmount;
}

public class PayrollLeaveDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public decimal HalfPaidDays { get; set; }
    public decimal PerDaySalary { get; set; }
    public decimal UnpaidMultiplier { get; set; }
    public decimal HalfPaidMultiplier { get; set; }
    public decimal UnpaidDeduction { get; set; }
    public decimal HalfPaidDeduction { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetSalaryAfterLeaveDeduction { get; set; }
}

public class PayrollLateAttendanceDeductionDetailDto
{
    public Guid EmployeeId { get; set; }
    public int TotalLateMinutes { get; set; }
    public int GraceMinutes { get; set; }
    public int BillableMinutes { get; set; }
    public decimal ShiftHours { get; set; }
    public decimal DeductionPerMinute { get; set; }
    public decimal TotalDeduction { get; set; }
    public bool IsGrace { get; set; }
    public decimal NetSalaryAfterLateDeduction { get; set; }
}

public class PayrollOvertimeDetailDto
{
    public Guid EmployeeId { get; set; }
    public decimal RegularOvertimeHours { get; set; }
    public decimal WeekendOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public decimal ShiftHours { get; set; }
    public decimal PerHourSalary { get; set; }
    public decimal RegularOvertimeAmount { get; set; }
    public decimal WeekendOvertimeAmount { get; set; }
    public decimal HolidayOvertimeAmount { get; set; }
    public decimal TotalOvertimeAmount { get; set; }
    public decimal NetSalaryAfterOvertime { get; set; }
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

// ─── Gratuity ────────────────────────────────────────────────────────────────

public class GratuityConfigDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ConfigRuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int YearsRequired { get; set; }
    public string CalculationType { get; set; } = string.Empty; // fixed | peryear | percentage
    public decimal CalculationValue { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGratuityConfigDto
{
    public string ConfigRuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int YearsRequired { get; set; } = 5;
    public string CalculationType { get; set; } = "peryear";
    public decimal CalculationValue { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateGratuityConfigDto : CreateGratuityConfigDto { }

public class GratuityTransactionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid? ConfigId { get; set; }
    public string? ConfigRuleName { get; set; }
    // Config fields joined from gratuityconfig (populated in my-status and detail queries)
    public string? CalculationType { get; set; }
    public decimal? CalculationValue { get; set; }
    public int? YearsRequired { get; set; }
    public decimal YearsWorked { get; set; }
    public decimal? LastSalary { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaidBy { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public int TotalCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGratuityTransactionDto
{
    public Guid EmployeeId { get; set; }
    public Guid? PeriodId { get; set; }
    public Guid ConfigId { get; set; }              // required: policy must be selected
    public decimal YearsWorked { get; set; }
    public decimal? LastSalary { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class UpdateGratuityTransactionDto
{
    public Guid? ConfigId { get; set; }
    public Guid? PeriodId { get; set; }
    public decimal YearsWorked { get; set; }
    public decimal? LastSalary { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
}

public class GratuityTransactionFilterDto
{
    public string? SearchTerm { get; set; }
    public Guid? PeriodId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ApproveGratuityDto
{
    public string? Notes { get; set; }
}

public class MarkGratuityPaidDto
{
    public Guid? PeriodId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
