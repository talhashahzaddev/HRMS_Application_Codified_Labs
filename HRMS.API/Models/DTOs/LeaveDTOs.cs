namespace HRMS.API.Models.DTOs;

public class LeaveRequestDto
{
    public Guid RequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string ProfilePictureUrl { get; set; }
}

public class CreateLeaveRequest
{
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class UpdateLeaveRequestStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}

public class UpdateLeaveRequest
{
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class LeaveTypeDto
{
    public Guid LeaveTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxDaysPerYear { get; set; }
    public bool IsPaid { get; set; }
    public bool IsHalfPaid { get; set; }
    public bool CarryForwardAllowed { get; set; }
    public int MaxCarryForwardDays { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class LeaveEntitlementDto
{
    public Guid EntitlementId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
    public decimal CarryForwardDays { get; set; }
}

public class LeaveBalanceDto
{
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
    public decimal CarryForwardDays { get; set; }

    public string Color { get; set; } = string.Empty;
}

public class EmployeeLeaveBalanceDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}

public class TeamRemainingLeavesDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<LeaveTypeBalanceDto> LeaveBalances { get; set; } = new();
}

public class LeaveTypeBalanceDto
{
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}
