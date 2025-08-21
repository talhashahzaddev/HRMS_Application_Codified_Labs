namespace HRMS.API.Models.Entities;

public class SkillSet : BaseEntity
{
    public Guid SkillId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public int[] SkillLevelScale { get; set; } = {1, 2, 3, 4, 5};
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
}
