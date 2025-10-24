using System;

namespace HRMS.API.Models.DTOs;

public class SalaryRuleDto
{
    public string RuleName { get; set; }
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? PositionId { get; set; }
    public string? PositionTitle { get; set; }
    public Guid ComponentId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public decimal ComponentValue { get; set; }
    public decimal? ValueOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSalaryRuleRequest
{
    public string RuleName { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal? ValueOverride { get; set; }
}

public class UpdateSalaryRuleRequest
{
    public string RuleName { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal? ValueOverride { get; set; }
}