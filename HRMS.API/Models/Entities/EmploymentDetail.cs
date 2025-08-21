namespace HRMS.API.Models.Entities;

public class EmploymentDetail : BaseEntity
{
    public Guid DetailId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? ShiftId { get; set; }
    public string EmploymentType { get; set; } = "full_time";
    public string PayType { get; set; } = "salary";
    public decimal? BasicSalary { get; set; }
    public string Currency { get; set; } = "USD";
    public string? WorkLocation { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; } = true;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Department? Department { get; set; }
    public Position? Position { get; set; }
    public Shift? Shift { get; set; }
    public Employee? ReportingManager { get; set; }
}
