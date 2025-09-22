using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("positions", Schema = "employee")]
public class Position : BaseEntity
{
    [Key]
    [Column("position_id")]
    public Guid PositionId { get; set; }
    
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
    
    [Column("position_title")]
    public string PositionTitle { get; set; } = string.Empty;
    
    [Column("department_id")]
    public Guid? DepartmentId { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    public string? DepartmentName { get; set; }
    [Column("requirements")]
    public Dictionary<string, object> Requirements { get; set; } = new();
    
    [Column("salary_range")]
    public Dictionary<string, object> SalaryRange { get; set; } = new();
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [NotMapped]
    public Organization Organization { get; set; } = null!;
    
    [NotMapped]
    public Department? Department { get; set; }
    
    [NotMapped]
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
}
