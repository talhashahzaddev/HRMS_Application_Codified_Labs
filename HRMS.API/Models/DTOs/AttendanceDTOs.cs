namespace HRMS.API.Models.DTOs;

public class AttendanceDto
{
    public Guid AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal TotalHours { get; set; }
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
