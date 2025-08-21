namespace HRMS.API.Models.Entities;

public class Attendance : BaseEntity
{
    public Guid AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public DateTime? BreakStartTime { get; set; }
    public DateTime? BreakEndTime { get; set; }
    public decimal TotalHours { get; set; } = 0;
    public decimal OvertimeHours { get; set; } = 0;
    public decimal BreakHours { get; set; } = 0;
    public string Status { get; set; } = "absent";
    public Dictionary<string, object> Location { get; set; } = new();
    public string? Notes { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Employee? Approver { get; set; }
}
