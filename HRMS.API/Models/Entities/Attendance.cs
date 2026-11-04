using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("attendance", Schema = "attendance")]
public class Attendance : BaseEntity
{
    [Key]
    [Column("attendance_id")]
    public Guid? AttendanceId { get; set; }
    
    [Column("employee_id")]
    public Guid EmployeeId { get; set; }
    
    [Column("work_date")]
    public DateTimeOffset WorkDate { get; set; }

    [NotMapped]
    public DateTimeOffset Date { get; set; }

    [Column("check_in_time")]
    public DateTimeOffset? CheckInTime { get; set; }

    [Column("check_out_time")]
    public DateTimeOffset? CheckOutTime { get; set; }
    
    [NotMapped]
    public string? CheckInLocation { get; set; }
    
    [NotMapped]
    public string? CheckOutLocation { get; set; }
    
    [Column("break_start_time")]
    public DateTimeOffset? BreakStartTime { get; set; }

    [Column("break_end_time")]
    public DateTimeOffset? BreakEndTime { get; set; }
    
    [Column("total_hours")]
    public decimal TotalHours { get; set; } = 0;
    
    [Column("overtime_hours")]
    public decimal OvertimeHours { get; set; } = 0;
    
    [Column("break_hours")]
    public decimal BreakHours { get; set; } = 0;
    
    [Column("status")]
    public string Status { get; set; } = "absent";
    
    [Column("location")]
    public Dictionary<string, object> Location { get; set; } = new();
    
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("attendance_state")]
    public string AttendanceState { get; set; } = "not_started";
    
    [NotMapped]
    public bool IsManualEntry { get; set; }
    
    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }
    
    [Column("approved_at")]
    public DateTimeOffset? ApprovedAt { get; set; }

    // Navigation properties
    [NotMapped]
    public Employee Employee { get; set; } = null!;
    
    [NotMapped]
    public Employee? Approver { get; set; }

    [NotMapped]
    public string ipAddress
    {
        get
        {
            if (Location != null && Location.TryGetValue("ip", out var ipObj))
            {
                return ipObj?.ToString() ?? "N/A";
            }
            return "N/A";
        }
    }

}
