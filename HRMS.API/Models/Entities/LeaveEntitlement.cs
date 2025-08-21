namespace HRMS.API.Models.Entities;

public class LeaveEntitlement : BaseEntity
{
    public Guid EntitlementId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; } = 0;
    public decimal UsedDays { get; set; } = 0;
    public decimal RemainingDays { get; set; } = 0;
    public decimal CarryForwardDays { get; set; } = 0;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
