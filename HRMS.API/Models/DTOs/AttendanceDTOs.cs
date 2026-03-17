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
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsManagerOverride { get; set; }
    public bool IsFinalized { get; set; }
    public bool HasPendingRequest { get; set; }
    public bool HasDraftRequest { get; set; }
    public bool HasApprovedRequest { get; set; }
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string? RequestedStatus { get; set; }
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
    public string FinalStatus { get; set; }
    public string EmployeeName { get; set; }
}

public class EmployeeTimesheetDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public List<DailyAttendanceRecord> DailyRecords { get; set; } = new();
    public decimal TotalHoursWorked { get; set; }
    public bool IsFinalized { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }
    public int HalfDays { get; set; }
    public decimal AttendancePercentage { get; set; }
}
public class MonthlyTimesheetCreateDto
{
    public string TimesheetName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
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
    public int Month { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public int TotalPresentDays { get; set; }
    public int TotalAbsentDays { get; set; }
    public int TotalLateDays { get; set; }
    public decimal AttendancePercentage { get; set; }
    public string MonthName => System.Globalization.CultureInfo.CurrentCulture
        .DateTimeFormat.GetMonthName(Month);
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
    public string? Notes { get; set; }
    public bool IsManagerOverride { get; set; }
    public bool IsFinalized { get; set; }
    public bool HasPendingRequest { get; set; }
    public bool HasDraftRequest { get; set; }
    public bool HasApprovedRequest { get; set; }
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string? RequestedStatus { get; set; }
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