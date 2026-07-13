using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IDashboardRepository
{
    // ── Leave ────────────────────────────────────────────────────────────────
    Task<List<LeaveTypeBalanceSummaryDto>> GetLeaveBalancesAsync(Guid employeeId, int year);

    // ── Financial Snapshot ───────────────────────────────────────────────────
    Task<FinancialSnapshotDto?> GetLatestPayslipAsync(Guid employeeId, Guid organizationId);

    // ── Active Loans & Advances ──────────────────────────────────────────────
    Task<List<ActiveLoanSummaryDto>> GetActiveLoansAsync(Guid employeeId, Guid organizationId);
    Task<List<ActiveAdvanceSummaryDto>> GetDisbursedAdvancesAsync(Guid employeeId, Guid organizationId);

    // ── Pending Requests ─────────────────────────────────────────────────────
    Task<List<PendingRequestSummaryDto>> GetPendingLeaveRequestsAsync(Guid employeeId);
    Task<List<PendingRequestSummaryDto>> GetPendingShiftSwapRequestsAsync(Guid employeeId);
    Task<List<PendingRequestSummaryDto>> GetPendingOvertimeRequestsAsync(Guid employeeId, Guid organizationId);
    Task<List<PendingRequestSummaryDto>> GetPendingSalaryAdvanceRequestsAsync(Guid employeeId, Guid organizationId);
    Task<List<PendingRequestSummaryDto>> GetPendingLoanRequestsAsync(Guid employeeId, Guid organizationId);
    Task<List<PendingRequestSummaryDto>> GetPendingPfRequestsAsync(Guid employeeId, Guid organizationId);

    // ── Manager Overview ─────────────────────────────────────────────────────
    Task<TeamAttendanceSnapshotDto> GetTeamAttendanceSnapshotAsync(Guid managerId, Guid organizationId, DateTime date);
    Task<int> GetTeamOvertimeCountTodayAsync(Guid managerId, Guid organizationId, DateTime date);
    Task<PendingManagerActionsDto> GetPendingManagerActionsAsync(Guid managerId, Guid organizationId);
}
