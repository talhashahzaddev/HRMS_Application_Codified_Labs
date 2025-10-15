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
    
    public string? RoleName { get; set; }
    
    [Column("requirements")]
    public Dictionary<string, object> Requirements { get; set; } = new();
    
    [Column("salary_range")]
    public Dictionary<string, object> SalaryRange { get; set; } = new();
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("roleid")]
    public Guid? RoleId { get; set; }

    // Navigation properties
    [NotMapped]
    public Organization Organization { get; set; } = null!;
    
    [NotMapped]
    public Department? Department { get; set; }
    
    [NotMapped]
    public Role? Role { get; set; }
    
    [NotMapped]
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
}

public class PositionWithEmployees
{
    // Position properties
    public Guid PositionId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Requirements { get; set; } = new();
    public Dictionary<string, object> SalaryRange { get; set; } = new();
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Employee properties
    public Guid? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeFirstName { get; set; }
    public string? EmployeeLastName { get; set; }
    public string? EmployeeEmail { get; set; }
    public string? EmployeePhone { get; set; }
    public string? EmployeeStatus { get; set; }
    public DateTime HireDate { get; set; }

    // Employment details
    public string? EmploymentType { get; set; }
    public string? PayType { get; set; }
    public decimal? BasicSalary { get; set; }
    public string? WorkLocation { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsCurrentEmployment { get; set; }
}
