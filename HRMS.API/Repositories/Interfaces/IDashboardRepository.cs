using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IDashboardRepository
{
    // ── Leave ────────────────────────────────────────────────────────────────
    Task<IReadOnlyList<LeaveTypeBalanceSummaryDto>> GetLeaveBalancesAsync(Guid employeeId, Guid organizationId, int year);

    // ── Financial Snapshot ───────────────────────────────────────────────────
    Task<FinancialSnapshotDto?> GetLatestPayslipAsync(Guid employeeId, Guid organizationId);

    // ── Active Loans & Advances ──────────────────────────────────────────────
    Task<IReadOnlyList<ActiveLoanSummaryDto>> GetActiveLoansAsync(Guid employeeId, Guid organizationId);
    Task<IReadOnlyList<ActiveAdvanceSummaryDto>> GetDisbursedAdvancesAsync(Guid employeeId, Guid organizationId);

    // ── Pending Requests ─────────────────────────────────────────────────────
    Task<IReadOnlyList<PendingRequestSummaryDto>> GetPendingLeaveRequestsAsync(Guid employeeId);
    Task<IReadOnlyList<PendingRequestSummaryDto>> GetPendingShiftSwapRequestsAsync(Guid employeeId);
    Task<IReadOnlyList<PendingRequestSummaryDto>> GetPendingOvertimeRequestsAsync(Guid employeeId, Guid organizationId);
    Task<IReadOnlyList<PendingRequestSummaryDto>> GetPendingSalaryAdvanceRequestsAsync(Guid employeeId, Guid organizationId);
    Task<IReadOnlyList<PendingRequestSummaryDto>> GetPendingLoanRequestsAsync(Guid employeeId, Guid organizationId);
    Task<IReadOnlyList<PendingRequestSummaryDto>> GetPendingPfRequestsAsync(Guid employeeId, Guid organizationId);

    // ── Manager Overview ─────────────────────────────────────────────────────
    Task<TeamAttendanceSnapshotDto> GetTeamAttendanceSnapshotAsync(Guid managerId, Guid organizationId, DateTime date);
    Task<int> GetTeamOvertimeCountTodayAsync(Guid managerId, Guid organizationId, DateTime date);
    Task<PendingManagerActionsDto> GetPendingManagerActionsAsync(Guid managerId, Guid organizationId);

    // ── HR/Finance Overview ──────────────────────────────────────────────────
    Task<PayrollRunStatusDto> GetLatestPayrollRunStatusAsync(Guid organizationId);
    Task<decimal> GetLatestPayrollOutflowAsync(Guid organizationId);
    Task<WorkforceMetricsDto> GetWorkforceMetricsAsync(Guid organizationId);

    // ── HR Stats Overview ────────────────────────────────────────────────────
    Task<WorkforceDemographicsDto> GetWorkforceDemographicsAsync(Guid organizationId);
    Task<IReadOnlyList<PayrollOutflowDto>> GetLast3PayrollOutflowAsync(Guid organizationId);
    Task<IReadOnlyList<MonthlyExpenseDto>> GetLast3MonthsExpensesAsync(Guid organizationId);

    // ── Recruitment Overview ─────────────────────────────────────────────────
    Task<IReadOnlyList<LatestHiredEmployeeDto>> GetLatestHiresAsync(Guid organizationId);
}
