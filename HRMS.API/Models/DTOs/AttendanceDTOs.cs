namespace HRMS.API.Models.DTOs;

public class AttendanceDto
{
    public Guid? AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLocation { get; set; }
    public string? CheckOutLocation { get; set; }
    public decimal TotalHours { get; set; }
    public int SessionsCount { get; set; } = 0;
    public decimal OvertimeHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateAttendanceRequest
{
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Notes { get; set; }
}

public class ClockInOutRequest
{
    public string Action { get; set; } = string.Empty; // "in" or "out"
    public Dictionary<string, object>? Location { get; set; }
}

public class AttendanceSearchRequest
{
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}



public class AttendanceSummaryDto
{
    public int? TotalWorkDays { get; set; }
    public int? PresentDays { get; set; }
    public int? AbsentDays { get; set; }
    public int? LateDays { get; set; }
    public decimal? TotalHours { get; set; }
    public decimal? OvertimeHours { get; set; }
    public decimal? AverageHoursPerDay { get; set; }
}
public class AttendanceSessionWithInfoDto
{
    public Guid SessionId { get; set; }
    public Guid AttendanceId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Location { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
}


public class TimeTrackingSessionDto
{
    public Guid? SessionId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool IsActive { get; set; }
    public decimal ElapsedHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ElapsedTime { get; set; }
    public string? CurrentLocation { get; set; }
}

public class AttendanceSessionDto
{
    public Guid SessionId { get; set; }
    public Guid AttendanceId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
 
}


public class CreateShiftDto
{
    public string ShiftName { get; set; }             // Name of the shift e.g., "Morning", "Evening"
    public TimeSpan? StartTime { get; set; }           // Start time of the shift
    public TimeSpan? EndTime { get; set; }             // End time of the shift
    public int? BreakDuration { get; set; } = 0;     // Optional: Break duration in minutes
    public int[]? DaysofWeek { get; set; } = new int[] { 1, 2, 3, 4, 5 }; // Optional: Days of week
    public string? Timezone { get; set; } = "UTC";    // Optional: Timezone
}

public class UpdateShiftDto 
{
    public string ShiftName { get; set; }             // Name of the shift e.g., "Morning", "Evening"
    public TimeSpan? StartTime { get; set; }           // Start time of the shift
    public TimeSpan? EndTime { get; set; }             // End time of the shift
    public int? BreakDuration { get; set; } = 0;     // Optional: Break duration in minutes
    public int[]? DaysofWeek { get; set; } = new int[] { 1, 2, 3, 4, 5 }; // Optional: Days of week
    public string? Timezone { get; set; } = "UTC";    // Optional: Timezone
}


public class AssignShiftDto
{
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
}

public class EmployeeShiftDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string DepartmentName { get; set; }
    public string PositionName { get; set; }
    public string ShiftName { get; set; }
}


public class ShiftDto
{
    public Guid ShiftId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ShiftName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDuration { get; set; }
    public int[] DaysOfWeek { get; set; }
    public string Timezone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

//Attendace ShiftSwap

public class CreateShiftSwapRequestDto
{
    public Guid EmployeeId { get; set; }
    public Guid? CurrentShiftId { get; set; }
    public Guid RequestedShiftId { get; set; }
    public string? Reason { get; set; }
}

public class ApproveShiftSwapRequestDto
{
    public Guid RequestId { get; set; }
    public Guid ApprovedBy { get; set; }
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}



public class PendingShiftSwapDto
{
    public Guid RequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    public Guid CurrentShiftId { get; set; }
    public string CurrentShiftName { get; set; } = string.Empty;

    public Guid RequestedShiftId { get; set; }
    public string RequestedShiftName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;  // approved, rejected, pending
}


public class ManualAttendanceRequest
{
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime Date { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLocation { get; set; }
    public string? CheckOutLocation { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? Reason { get; set; }
}

public class AttendanceCalendarDto
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal TotalHours { get; set; }
    public bool IsWorkingDay { get; set; }
    public bool IsHoliday { get; set; }
}

public class DailyAttendanceStatsDto
{
    public DateTime Date { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public int LateEmployees { get; set; }
    public int EarlyLeavers { get; set; }
    public int OnLeaveEmployees { get; set; }
    public decimal AttendancePercentage { get; set; }
    public TimeSpan? AverageCheckInTime { get; set; }
    public TimeSpan? AverageCheckOutTime { get; set; }
}

public class AttendanceReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalWorkDays { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalPresentDays { get; set; }
    public int TotalAbsentDays { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public int TotalLateDays { get; set; } // Add this
    public decimal TotalHours { get; set; } // Add this
    public decimal OvertimeHours { get; set; } // Add this
    public List<AttendanceDto> Records { get; set; } = new();
    public AttendanceSummaryDto Summary { get; set; } = new();
    public List<DailyAttendanceStatsDto> DailyStats { get; set; } = new();
    public List<DepartmentAttendanceStatsDto> DepartmentStats { get; set; } = new();
}

public class BulkAttendanceRequest
{
    public List<Guid> AttendanceIds { get; set; } = new();
}

public class BulkAttendanceRejectRequest
{
    public List<Guid> AttendanceIds { get; set; } = new();
    public string? Reason { get; set; }
}

public class DepartmentAttendanceStatsDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public decimal AttendanceRate { get; set; }
}

