namespace HRMS.API.Models.DTOs;

public class PayRollRuleDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RulesCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
