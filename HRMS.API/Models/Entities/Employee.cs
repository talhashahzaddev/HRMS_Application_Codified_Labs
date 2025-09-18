using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("employees", Schema = "employee")]
public class Employee : BaseEntity
{
    [Key]
    [Column("employee_id")]
    public Guid EmployeeId { get; set; }
    
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
    
    [Column("user_id")]
    public Guid? UserId { get; set; }
    
    [Column("employee_code")]
    public string EmployeeNumber { get; set; } = string.Empty;
    
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;
    
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;
    
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Column("phone")]
    public string? Phone { get; set; }
    
    [Column("address")]
    public Dictionary<string, object> Address { get; set; } = new();
    
    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }
    
    [Column("gender")]
    public string? Gender { get; set; }
    
    [Column("nationality")]
    public string? Nationality { get; set; }
    
    [Column("marital_status")]
    public string? MaritalStatus { get; set; }
    
    [Column("emergency_contact")]
    public Dictionary<string, object> EmergencyContact { get; set; } = new();
    
    [Column("hire_date")]
    public DateTime HireDate { get; set; }
    
    [Column("termination_date")]
    public DateTime? TerminationDate { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "active";
    
    [Column("profile_picture_url")]
    public string? ProfilePictureUrl { get; set; }
    [Column("departmentid")]
    public Guid? DepartmentId { get; set; }
    [Column("positionid")]
    public Guid? PositionId { get; set; }
    [Column("basicsalary")]
    public decimal? BasicSalary { get; set; }

    // Navigation properties
    [NotMapped]
    public Organization Organization { get; set; } = null!;
    
    [NotMapped]
    public User? User { get; set; }
    
    [NotMapped]
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
    
    [NotMapped]
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    
    [NotMapped]
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    [NotMapped]
    public bool IsActive => Status == "active";
}
