namespace HRMS.API.Models.DTOs;

public class AttendanceDto
{
    public Guid? AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; }
    public string DepartmentName { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLocation { get; set; }
    public string? CheckOutLocation { get; set; }
    public decimal TotalHours { get; set; }
    public int SessionsCount { get; set; } = 0;
    public decimal OvertimeHours { get; set; }
    public string? OvertimeType { get; set; }
    public int LateMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary>Leavetype FK on the day (set when status = on_leave). Drives leave_type / leave_pay_type.</summary>
    public Guid? LeaveTypeId { get; set; }
    /// <summary>Hydrated leavetype.typename for display.</summary>
    public string? LeaveTypeName { get; set; }
    /// <summary>Derived: 'paid' / 'unpaid' / 'half_paid' from leavetypes flags.</summary>
    public string? LeavePayType { get; set; }
    public string? Notes { get; set; }
    public bool IsManagerOverride { get; set; }
    public bool IsFinalized { get; set; }
    public bool HasPendingRequest { get; set; }
    public bool HasDraftRequest { get; set; }
    public bool HasApprovedRequest { get; set; }
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string? RequestedStatus { get; set; }
    /// <summary>Latest pending/draft/approved request's leavetype id.</summary>
    public Guid? RequestedLeaveTypeId { get; set; }
    public string? RequestedLeaveTypeName { get; set; }
    public string? RequestedLeavePayType { get; set; }
    public decimal? RequestedOvertimeHours { get; set; }
    public string? RequestedOvertimeType { get; set; }
    public int? RequestedLateMinutes { get; set; }
    public Guid? ShiftId { get; set; }
    public string? Location { get; set; }
    public string? checkinip { get; set; }
    public string? checkoutip { get; set; }
    public string? checkinLocationType { get; set; }
    public string? checkoutLocationType { get; set; }
    public string ipAddress { get; set; }

}
public class ClockInOutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class IpAddress
{
    public Guid Id { get; set; }
    public string IpAddressValue { get; set; }
    public string Name { get; set; }      // Example: "Main Office", "Branch 1"
    public DateTime CreatedAt { get; set; }
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
    public string? Notes { get; set; }
    public Guid ShiftId { get; set; }
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
    public string ipAddress { get; set; }
    public string? checkinip { get; set; }
    public string? checkoutip { get; set; }

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
    public string ShiftName { get; set; }                 // Name of the shift e.g., "Morning", "Evening"
    public TimeSpan? StartTime { get; set; }              // Start time of the shift
    public TimeSpan? EndTime { get; set; }                // End time of the shift
    public int? marginHours { get; set; }
    public bool? ApplyMarginhours { get; set; } = false;

    public int? BreakDuration { get; set; } = 0;          // Break duration in minutes
    public TimeSpan? BreakStartTime { get; set; }         // Added: Break start time
    public TimeSpan? BreakEndTime { get; set; }           // Added: Break end time

    public TimeSpan? OvertimeStartTime { get; set; }      // Added: From when overtime begins
    public int? MaxOvertimeInMinutes { get; set; } = 0;   // Added: Maximum overtime allowed

    /// <summary>
    /// Per-shift grace period in minutes for late calculation. Used by the timesheet
    /// late-minutes function so orgs without the payroll module can still configure
    /// lateness behaviour. 0 means "fall back to payroll late-arrival rule, then 15-min default".
    /// </summary>
    public int? GraceMinutes { get; set; } = 0;

    public int[]? DaysofWeek { get; set; }
        = new int[] { 1, 2, 3, 4, 5 };                    // Days of week

    public string? Timezone { get; set; } = "UTC";        // Timezone
}


public class UpdateShiftDto
{
    public string ShiftName { get; set; }                   // Name of the shift e.g., "Morning", "Evening"
    public TimeSpan? StartTime { get; set; }               // Start time of the shift
    public TimeSpan? EndTime { get; set; }                 // End time of the shift
    public int marginHours { get; set; }
    public bool ApplyMarginhours { get; set; } = false;
    public TimeSpan? BreakStartTime { get; set; }          // Optional: Break start time
    public TimeSpan? BreakEndTime { get; set; }            // Optional: Break end time
    public TimeSpan? OvertimeStartTime { get; set; }       // Optional: Overtime start time
    public int? MaxOvertimeInMinutes { get; set; }         // Optional: Maximum overtime in minutes
    public int? GraceMinutes { get; set; } = 0;            // See CreateShiftDto.GraceMinutes

    public int? BreakDuration { get; set; } = 0;          // Optional: Break duration in minutes
    public int[]? DaysofWeek { get; set; } = new int[] { 1, 2, 3, 4, 5 }; // Optional: Days of week
    public string? Timezone { get; set; } = "UTC";        // Optional: Timezone
}



//public class AssignShiftDto
//{
//    public Guid EmployeeId { get; set; }
//    public Guid ShiftId { get; set; }
//    public DateTime AssignedDate { get; set; }

//}

public class AssignShiftDto
{
    public Guid[]? SelectedEmployees { get; set; }
    public Guid ShiftId { get; set; }
    public DateTime AssignedDate { get; set; }

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
    public int marginHours { get; set; }

    public bool ApplyMarginhours { get; set; } = false;

    public TimeSpan? BreakStartTime { get; set; }      // optional
    public TimeSpan? BreakEndTime { get; set; }        // optional
    public TimeSpan? OvertimeStartTime { get; set; }   // optional
    public int? MaxOvertimeInMinutes { get; set; }     // optional

    public int BreakDuration { get; set; }
    public int GraceMinutes { get; set; }
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

//Getting create shift swap response
public class ShiftSwapRequestCreatedDto
{
    public Guid RequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ReportingManagerId { get; set; }
}

public class ApproveShiftSwapRequestDto
{
    public Guid RequestId { get; set; }
    public Guid ApprovedBy { get; set; }
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}

public class ShiftSummary
{
    public int TotalShifts { get; set; }
    public int ShiftAssignedEmployee { get; set; }
    public int UnassignedShiftsEmp { get; set; }
    public int shiftSwapRequests { get; set; }
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

//shiftswap request dto
public class ShiftSwapRequestDto
{
    public Guid RequestId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }

    public Guid? CurrentShiftId { get; set; }
    public Guid RequestedShiftId { get; set; }

    public string? Reason { get; set; }
    public string Status { get; set; }

    public DateTime SubmittedAt { get; set; }

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? ReportingManagerId { get; set; }
}
public class FinalizeBatchRequestDto
{
    public Guid TimesheetId { get; set; }
    /// <summary>
    /// Optional. When provided, finalization is scoped to a single employee.
    /// When null, the entire timesheet (all employees) is finalized.
    /// </summary>
    public Guid EmployeeId { get; set; }
}

public class FinalizedTimesheetRecordDto
{
    public Guid RecordId { get; set; }
    public Guid TimesheetId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? FinalCheckIn { get; set; }
    public DateTime? FinalCheckOut { get; set; }
    public decimal FinalTotalHours { get; set; }
    public decimal FinalRegularHours { get; set; }
    public decimal FinalOvertimeHours { get; set; }
    public decimal FinalRegularOvertimeHours { get; set; }
    public decimal FinalWeekendOvertimeHours { get; set; }
    public decimal FinalHolidayOvertimeHours { get; set; }
    public int FinalLateMinutes { get; set; }
    public decimal FinalLateHours { get; set; }
    public int FinalEarlyDepartureMinutes { get; set; }
    public string FinalStatus { get; set; }
    public string? LeaveType { get; set; }
    public string? LeavePayType { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsWeekend { get; set; }
    public string EmployeeName { get; set; }
}

public class EmployeeTimesheetDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public List<DailyAttendanceRecord> DailyRecords { get; set; } = new();
    public decimal TotalHoursWorked { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal RegularOvertimeHours { get; set; }
    public decimal WeekendOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public int LateMinutes { get; set; }
    public decimal LateHours { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }
    public int HalfDays { get; set; }
    public int HolidayDays { get; set; }
    public int WeekendDays { get; set; }
    public decimal AttendancePercentage { get; set; }
}
public class MonthlyTimesheetCreateDto
{
    public string TimesheetName { get; set; }
    /// <summary>Period start (inclusive). Replaces Month/Year.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Period end (inclusive). Replaces Month/Year.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Legacy. Optional; ignored if StartDate/EndDate are provided.</summary>
    public int? Month { get; set; }
    /// <summary>Legacy. Optional; ignored if StartDate/EndDate are provided.</summary>
    public int? Year { get; set; }
}
public class TimeZoneDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UtcOffset { get; set; } = string.Empty;
}
public class OrgSubmissionProgressDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalEmployees { get; set; }
    public int FinalizedCount { get; set; }
    public int SubmittedCount { get; set; }
    public int DraftCount { get; set; }
    public int RejectedCount { get; set;}
    public int FinalizationPercentage { get; set; }
    public int PendingReviewCount { get; set; }
    public int InProgressCount { get; set; }
    public int UntouchedCount { get; set; }
    public decimal SubmissionRate { get; set; }
    //=> TotalEmployees > 0
    //    ? Math.Round(((decimal)(FinalizedCount + SubmittedCount) / TotalEmployees) * 100, 1)
    //    : 0;
    public decimal ComplianceRate { get; set; } 
        //=> TotalEmployees > 0
      // ? Math.Round(((decimal)FinalizedCount / TotalEmployees) * 100, 1)
       //: 0;
}
public class AttendanceUpdateRequestDto
{
    public Guid? AttendanceId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string RequestedStatus { get; set; }
    public string ReasonForEdit { get; set; }
    public string? RequestedNotes { get; set; }
    public Guid? TimesheetId { get; set; }
    /// <summary>
    /// Optional. Required when RequestedStatus = 'on_leave'. The attendance.leavetypes row
    /// the employee is requesting. On approval, this is copied onto attendance.attendance.leave_type_id
    /// and ultimately drives finalized_timesheet_records.leave_type / leave_pay_type.
    /// </summary>
    public Guid? RequestedLeaveTypeId { get; set; }

    /// <summary>Optional. Hours of overtime the employee is claiming for the day.</summary>
    public decimal? RequestedOvertimeHours { get; set; }

    /// <summary>Optional. 'regular' | 'weekend' | 'holiday' — drives typed OT bucket on the bridge.</summary>
    public string? RequestedOvertimeType { get; set; }

    /// <summary>Optional. Minutes-late override (0 to waive a late mark, positive to set explicitly).</summary>
    public int? RequestedLateMinutes { get; set; }
}

public class ProcessAttendanceRequestDto
{
    public Guid RequestId { get; set; }
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}
public class MonthlyTimesheetSummary
{
    public Guid TimesheetId { get; set; }
    public string TimesheetName { get; set; } = string.Empty;

    /// <summary>Inclusive period start.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Inclusive period end.</summary>
    public DateTime EndDate   { get; set; }

    /// <summary>Legacy month (nullable). Derive from StartDate when reading new data.</summary>
    public int? Month { get; set; }
    /// <summary>Legacy year (nullable). Derive from StartDate when reading new data.</summary>
    public int? Year  { get; set; }

    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? LockedAt    { get; set; }

    public int     TotalEmployees       { get; set; }
    public decimal TotalHoursWorked     { get; set; }
    public decimal TotalOvertimeHours   { get; set; }
    public int     TotalPresentDays     { get; set; }
    public int     TotalAbsentDays      { get; set; }
    public int     TotalLateDays        { get; set; }
    public decimal AttendancePercentage { get; set; }

    public string MonthName =>
        System.Globalization.CultureInfo.CurrentCulture
              .DateTimeFormat.GetMonthName(StartDate == default ? (Month ?? 1) : StartDate.Month);
}
public class PendingAttendanceRequest
{
    public Guid RequestId { get; set; }
    public Guid? AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? OriginalCheckIn { get; set; }
    public DateTime? OriginalCheckOut { get; set; }
    public string OriginalStatus { get; set; } = string.Empty;
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string RequestedStatus { get; set; } = string.Empty;
    public string ReasonForEdit { get; set; } = string.Empty;
    public string? RequestedNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime Submittedat { get; set; }
    public string Status { get; set; } = "pending";
}
public class ApproveAllRequestDto
{
    public Guid TimesheetId { get; set; }
    public Guid? EmployeeId { get; set; }
}

public class EmployeeSubmissionPackage
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid TimesheetId { get; set; }
    public string TimesheetName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public List<PendingAttendanceRequest> Corrections { get; set; } = new();
}
public class EmployeeReviewPackage
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public Guid TimesheetId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public List<ReviewAttendanceRecord> FullMonthRecords { get; set; } = new();
    public int PendingRequestCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int FinalizedCount { get; set; }
    public int TotalRecords { get; set; }
    public bool IsFinalized { get; set; }
    public bool HasPendingRequest => PendingRequestCount > 0;
    public bool HasDraftRequest => FullMonthRecords.Any(r => r.HasDraftRequest);
}

public class ReviewAttendanceRecord : DailyAttendanceRecord
{
    public Guid? RequestId { get; set; }
    public DateTime? OriginalCheckIn => CheckInTime;
    public DateTime? OriginalCheckOut => CheckOutTime;
    public string OriginalStatus => Status;
    public decimal OriginalTotalHours => TotalHours;
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string? RequestedStatus { get; set; }
    public string? ReasonForEdit { get; set; }
    public string RequestStatus { get; set; } = "none";
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
    public string? HolidayName { get; set; }
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

public class DailyAttendanceRecord
{
    public Guid? AttendanceId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; }
    public decimal TotalHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? OvertimeType { get; set; }
    public int LateMinutes { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public string? LeaveTypeName { get; set; }
    public string? LeavePayType { get; set; }
    public string? Notes { get; set; }
    public bool IsManagerOverride { get; set; }
    public bool IsFinalized { get; set; }
    public bool HasPendingRequest { get; set; }
    public bool HasDraftRequest { get; set; }
    public bool HasApprovedRequest { get; set; }
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string? RequestedStatus { get; set; }
    /// <summary>Draft/pending request: leavetype the employee asked for.</summary>
    public Guid? RequestedLeaveTypeId { get; set; }
    public string? RequestedLeaveTypeName { get; set; }
    public string? RequestedLeavePayType { get; set; }
    /// <summary>Draft/pending request: payroll-relevant counter overrides.</summary>
    public decimal? RequestedOvertimeHours { get; set; }
    public string? RequestedOvertimeType { get; set; }
    public int? RequestedLateMinutes { get; set; }
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
    public PagedResult<AttendanceDto> Records { get; set; } = new();
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

public class ManualAttendanceSearchDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? EmployeeId { get; set; }
}

public class ManualAttendanceUpdateDto
{
    public Guid? AttendanceId { get; set; }
    public Guid TimesheetId { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public DateTime WorkDate { get; set; }
    public string Status { get; set; }
    public string? Notes { get; set; }
    public string? Reason { get; set; }
    public Guid EmployeeId { get; set; }
    /// <summary>
    /// Manager-supplied leavetype when override Status = 'on_leave'. Used to set
    /// finalized_timesheet_records.leave_type / leave_pay_type directly so the
    /// override is payroll-correct without going through attendance.leaverequests.
    /// </summary>
    public Guid? LeaveTypeId { get; set; }

    /// <summary>Manager override of total OT hours for the day.</summary>
    public decimal? OvertimeHours { get; set; }

    /// <summary>'regular' | 'weekend' | 'holiday' — routes OT into the matching typed bucket on the bridge.</summary>
    public string? OvertimeType { get; set; }

    /// <summary>Manager override of late minutes (0 waives, positive sets explicitly).</summary>
    public int? LateMinutes { get; set; }
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


public class CreateEmployeeOvertimeDto
{
    public DateTime OverTimeDate { get; set; }
    public string OvertimeType { get; set; }
    public string Reason { get; set; }
    public TimeSpan OvertimeStartTime { get; set; }  // ✅ Changed
    public TimeSpan OvertimeEndTime { get; set; }    // ✅ Changed
}

public class ManagerOvertimeRequestDto
{
    public Guid EmployeeId { get; set; }
    public DateTime OvertimeDate { get; set; }
    public string OvertimeType { get; set; }
    public string Reason { get; set; }
    public TimeSpan OvertimeStartTime { get; set; }  // ✅ Changed
    public TimeSpan OvertimeEndTime { get; set; }    // ✅ Changed
}

public class OvertimeRequestDto
{
    public Guid RequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RequestedBy { get; set; }
    public string RequestedByName { get; set; }
    public string RequestType { get; set; }
    public string OvertimeType { get; set; }
    public DateTime OvertimeDate { get; set; }
    public TimeSpan OvertimeStart { get; set; }  // ✅ Changed
    public TimeSpan OvertimeEnd { get; set; }    // ✅ Changed
    public decimal RequestedHours { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmployeeOverTimeDto
{
    public Guid RequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid Managerid { get; set; }
    public Guid? AttendanceId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string OvertimeType { get; set; } = string.Empty;
    public DateTime OvertimeDate { get; set; }
    public TimeSpan OvertimeStart { get; set; }  // ✅ Changed
    public TimeSpan OvertimeEnd { get; set; }    // ✅ Changed
    public decimal RequestedHours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}




// DTOs/Attendance/GetOvertimesQueryDto.cs
public class GetOvertimesQueryDto
{
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}


// ============================================================================
// PAYROLL BRIDGE DTOs
// Provided by AttendanceController.GetTimesheetPayrollSummary so the payroll
// module can compute deductions/overtime/leave-impact from a finalized timesheet.
// ============================================================================

/// <summary>
/// Lightweight reference to a finalized timesheet — used to populate the payroll
/// module's "select timesheet for this payroll period" picker.
/// </summary>
public class TimesheetPeriodLinkDto
{
    public Guid TimesheetId { get; set; }
    public string TimesheetName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public DateTime? FinalizedAt { get; set; }
}

/// <summary>
/// Per-employee attendance counters derived from a finalized timesheet — every field
/// payroll needs to compute absent/late/leave deductions, overtime pay, etc.
/// </summary>
public class EmployeePayrollSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public Guid TimesheetId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd   { get; set; }

    public int TotalCalendarDays    { get; set; }
    public int ScheduledWorkingDays { get; set; }
    public int WeekendDays          { get; set; }
    public int HolidayDays          { get; set; }

    public int PresentDays          { get; set; }
    public int AbsentDays           { get; set; }
    public int LateDays             { get; set; }
    public int HalfDays             { get; set; }
    public int EarlyDepartureDays   { get; set; }

    public decimal LeaveDaysTotal     { get; set; }
    public decimal PaidLeaveDays      { get; set; }
    public decimal UnpaidLeaveDays    { get; set; }
    public decimal HalfPaidLeaveDays  { get; set; }

    public decimal TotalHoursWorked   { get; set; }
    public decimal RegularHours       { get; set; }
    public decimal OvertimeHours      { get; set; }
    public decimal RegularOvertimeHours { get; set; }
    public decimal WeekendOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }

    public int LateMinutes            { get; set; }
    public decimal LateHours          { get; set; }
    public int EarlyDepartureMinutes  { get; set; }

    public decimal EffectivePresentDays { get; set; }
    public decimal AttendancePercentage { get; set; }

    public bool IsConsumedByPayroll   { get; set; }
    public DateTime? AggregatedAt     { get; set; }
}

/// <summary>
/// Full payroll-ready snapshot for a finalized timesheet: header + per-employee rows.
/// Returned by GET /api/attendance/timesheet/payroll-summary?timesheetId=...
/// </summary>
public class TimesheetPayrollSummaryDto
{
    public Guid TimesheetId { get; set; }
    public string TimesheetName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd   { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? LockedAt    { get; set; }
    public List<EmployeePayrollSummaryDto> Employees { get; set; } = new();
}

/// <summary>
/// Payload for POST /api/attendance/timesheet/lock — fired when payroll consumes
/// a finalized timesheet so it can no longer be edited.
/// </summary>
public class LockTimesheetRequestDto
{
    public Guid TimesheetId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
}