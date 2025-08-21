namespace HRMS.API.Models.Entities;

public class Employee : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? MaritalStatus { get; set; }
    public Dictionary<string, object> EmergencyContact { get; set; } = new();
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string Status { get; set; } = "active";
    public string? ProfilePictureUrl { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
}
