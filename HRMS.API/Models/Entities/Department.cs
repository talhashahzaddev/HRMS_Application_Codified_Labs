using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("departments", Schema = "employee")]
public class Department : BaseEntity
{
    [Key]
    [Column("department_id")]
    public Guid DepartmentId { get; set; }
    
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
    
    [Column("department_name")]
    public string DepartmentName { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("manager_id")]
    public Guid? ManagerId { get; set; }
    
    [Column("parent_department_id")]
    public Guid? ParentDepartmentId { get; set; }
    
    [Column("budget")]
    public decimal? Budget { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [NotMapped]
    public Organization Organization { get; set; } = null!;
    
    [NotMapped]
    public Employee? Manager { get; set; }
    
    [NotMapped]
    public Department? ParentDepartment { get; set; }
    
    [NotMapped]
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    
    [NotMapped]
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    
    [NotMapped]
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
}
