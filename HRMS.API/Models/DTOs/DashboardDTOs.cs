namespace HRMS.API.Models.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// Root response DTO returned by GET /api/dashboard/my-overview
// ─────────────────────────────────────────────────────────────────────────────
public class EmployeeOverviewDto
{
    public LeaveBalanceSummaryDto LeaveBalances { get; set; } = new();
    public FinancialSnapshotDto FinancialSnapshot { get; set; } = new();
    public ActiveLoansAdvancesDto ActiveLoansAndAdvances { get; set; } = new();
    public List<PendingRequestSummaryDto> PendingRequests { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
// 1. Leave Balance Section
// ─────────────────────────────────────────────────────────────────────────────
public class LeaveBalanceSummaryDto
{
    public decimal TotalRemainingDays { get; set; }
    public List<LeaveTypeBalanceSummaryDto> ByType { get; set; } = new();
}

public class LeaveTypeBalanceSummaryDto
{
    public string LeaveTypeName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
    public decimal CarryForwardDays { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 2. Financial Snapshot Section
// ─────────────────────────────────────────────────────────────────────────────
public class FinancialSnapshotDto
{
    /// <summary>Net salary from the most recent generated payslip for the employee.</summary>
    public decimal? LastNetSalary { get; set; }

    /// <summary>Name of the payroll period (e.g. "July 2025").</summary>
    public string? LastPayPeriodName { get; set; }

    /// <summary>Direct download URL for the uploaded payslip PDF. Null if not yet generated.</summary>
    public string? PayslipUploadUrl { get; set; }

    /// <summary>Whether a payslip is available at all for the last period.</summary>
    public bool IsPayslipAvailable { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 3. Active Loans & Advances Section
// ─────────────────────────────────────────────────────────────────────────────
public class ActiveLoansAdvancesDto
{
    public decimal TotalRemainingLoanAmount { get; set; }
    public decimal TotalRemainingAdvanceAmount { get; set; }
    public List<ActiveLoanSummaryDto> ActiveLoans { get; set; } = new();
    public List<ActiveAdvanceSummaryDto> ActiveAdvances { get; set; } = new();
}

public class ActiveLoanSummaryDto
{
    public Guid LoanId { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int RemainingInstallments { get; set; }
    public decimal CompletedPercentage { get; set; }
}

public class ActiveAdvanceSummaryDto
{
    public Guid AdvanceId { get; set; }
    public decimal Amount { get; set; }
    public string AdvanceStatus { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 4. Pending Requests Section
// ─────────────────────────────────────────────────────────────────────────────
public class PendingRequestSummaryDto
{
    public Guid RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;  // "Leave", "ShiftSwap", "Overtime", "SalaryAdvance", "Loan", "PF"
    public string Description { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 5. Manager Overview Section
// ─────────────────────────────────────────────────────────────────────────────
public class ManagerOverviewDto
{
    public TeamAttendanceSnapshotDto TeamAttendanceSnapshot { get; set; } = new();
    public int TeamOvertimeCountToday { get; set; }
    public PendingManagerActionsDto PendingActions { get; set; } = new();
}

public class TeamAttendanceSnapshotDto
{
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int OnLeaveCount { get; set; }
}

public class PendingManagerActionsDto
{
    public int PendingLeaveRequests { get; set; }
    public int PendingTimesheetCorrections { get; set; }
    public int PendingShiftSwaps { get; set; }
}
