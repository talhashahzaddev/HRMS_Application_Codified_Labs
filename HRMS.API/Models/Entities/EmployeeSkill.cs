namespace HRMS.API.Models.Entities;

public class EmployeeSkill : BaseEntity
{
    public Guid EmployeeSkillId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid SkillId { get; set; }
    public int ProficiencyLevel { get; set; } = 1;
    public Guid? AssessedBy { get; set; }
    public DateTime? LastAssessed { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public SkillSet Skill { get; set; } = null!;
    public Employee? Assessor { get; set; }
}
