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
