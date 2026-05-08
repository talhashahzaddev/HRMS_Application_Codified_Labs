namespace HRMS.API.Models.Entities;

public class LeaveRequest : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string typename { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public Employee? Approver { get; set; }
}
