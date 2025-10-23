using System;

namespace HRMS.API.Models.Entities;

public class SalaryRule
{
    public Guid RuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal? ValueOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}