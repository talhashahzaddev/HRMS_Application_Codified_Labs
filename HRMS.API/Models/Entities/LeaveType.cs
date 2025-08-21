namespace HRMS.API.Models.Entities;

public class LeaveType : BaseEntity
{
    public Guid LeaveTypeId { get; set; }
    public Guid OrganizationId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxDaysPerYear { get; set; } = 0;
    public bool IsPaid { get; set; } = true;
    public bool CarryForwardAllowed { get; set; } = false;
    public int MaxCarryForwardDays { get; set; } = 0;
    public bool RequiresApproval { get; set; } = true;
    public int AdvanceNoticeDays { get; set; } = 0;
    public string Color { get; set; } = "#2196F3";
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveEntitlement> LeaveEntitlements { get; set; } = new List<LeaveEntitlement>();
}
