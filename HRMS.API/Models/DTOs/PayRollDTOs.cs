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
