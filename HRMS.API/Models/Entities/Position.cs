namespace HRMS.API.Models.Entities;

public class Position : BaseEntity
{
    public Guid PositionId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Requirements { get; set; } = new();
    public Dictionary<string, object> SalaryRange { get; set; } = new();
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
}
