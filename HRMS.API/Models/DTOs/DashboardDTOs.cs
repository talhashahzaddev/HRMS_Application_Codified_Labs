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

// ─────────────────────────────────────────────────────────────────────────────
// 6. HR/Finance Overview Section
// ─────────────────────────────────────────────────────────────────────────────
public class HrOverviewDto
{
    public PayrollRunStatusDto LatestPayrollRun { get; set; } = new();
    public decimal TotalPayrollOutflow { get; set; }
    public WorkforceMetricsDto WorkforceMetrics { get; set; } = new();
}

public class PayrollRunStatusDto
{
    public string PeriodName { get; set; } = string.Empty;
    public int TotalEmployeesProcessed { get; set; }
    public int PayslipsGenerated { get; set; }
    public int PayslipsUploaded { get; set; }
    public int EmailsSent { get; set; }
    public bool IsFullyCompleted => TotalEmployeesProcessed > 0 && EmailsSent == TotalEmployeesProcessed;
}

public class WorkforceMetricsDto
{
    public int TotalActiveEmployees { get; set; }
    public int TotalOpenPositions { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 7. HR Stats Section
// ─────────────────────────────────────────────────────────────────────────────
public class HrStatsDto
{
    public WorkforceDemographicsDto Demographics { get; set; } = new();
    public List<PayrollOutflowDto> PayrollOutflow { get; set; } = new();
    public List<MonthlyExpenseDto> MonthlyExpenses { get; set; } = new();
}

public class WorkforceDemographicsDto
{
    public decimal MalePercentage { get; set; }
    public decimal FemalePercentage { get; set; }
    public Dictionary<string, decimal> AgeGroupPercentages { get; set; } = new();
    public string LargestDepartmentName { get; set; } = string.Empty;
    public decimal LargestDepartmentPercentage { get; set; }
}

public class PayrollOutflowDto
{
    public string PeriodName { get; set; } = string.Empty;
    public decimal BasicSalarySum { get; set; }
    public decimal TotalDeductionsSum { get; set; }
    public decimal TotalBonusesSum { get; set; }
}

public class MonthlyExpenseDto
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalExpense { get; set; }
}

public class LatestHiredEmployeeDto
{
    public Guid JobApplyId { get; set; }
    public Guid JobId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string JobRoleName { get; set; } = string.Empty;
    public string Interviewers { get; set; } = string.Empty;
}

