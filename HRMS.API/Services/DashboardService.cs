using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HRMS.API.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IAttendanceService   _attendanceService;   // used to resolve userId → employeeId
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IAttendanceService   attendanceService,
        ILogger<DashboardService> logger)
    {
        _dashboardRepository = dashboardRepository;
        _attendanceService   = attendanceService;
        _logger              = logger;
    }

    /// <summary>
    /// Aggregates all personal dashboard widgets for the logged-in employee.
    /// All sub-queries run in parallel via Task.WhenAll for maximum performance.
    /// </summary>
    public async Task<EmployeeOverviewDto> GetEmployeeOverviewAsync(Guid userId, Guid organizationId)
    {
        try
        {
            // Resolve the employee row id from the users table (same pattern used across all controllers)
            var employeeId = await _attendanceService.GetEmployeeFromTable(userId);

            var currentYear = DateTime.UtcNow.Year;

            // ── Fire all queries in parallel ──────────────────────────────────────
            var leaveTask = _dashboardRepository.GetLeaveBalancesAsync(employeeId, organizationId, currentYear);
            var payslipTask = _dashboardRepository.GetLatestPayslipAsync(employeeId, organizationId);
            var activeLoansTask = _dashboardRepository.GetActiveLoansAsync(employeeId, organizationId);
            var advancesTask = _dashboardRepository.GetDisbursedAdvancesAsync(employeeId, organizationId);
            var pendingLeaveTask = _dashboardRepository.GetPendingLeaveRequestsAsync(employeeId);
            var pendingShiftTask = _dashboardRepository.GetPendingShiftSwapRequestsAsync(employeeId);
            var pendingOtTask = _dashboardRepository.GetPendingOvertimeRequestsAsync(employeeId, organizationId);
            var pendingAdvTask = _dashboardRepository.GetPendingSalaryAdvanceRequestsAsync(employeeId, organizationId);
            var pendingLoanTask = _dashboardRepository.GetPendingLoanRequestsAsync(employeeId, organizationId);
            var pendingPfTask = _dashboardRepository.GetPendingPfRequestsAsync(employeeId, organizationId);

            await Task.WhenAll(
                leaveTask, payslipTask,
                activeLoansTask, advancesTask,
                pendingLeaveTask, pendingShiftTask,
                pendingOtTask, pendingAdvTask,
                pendingLoanTask, pendingPfTask);
            // ── Leave balance summary ────────────────────────────────────────────
            var leaveTypes = await leaveTask;
            var leaveBalanceSummary = new LeaveBalanceSummaryDto
            {
                TotalRemainingDays = leaveTypes.Sum(l => l.RemainingDays),
                ByType = leaveTypes
            };

            // ── Financial snapshot ────────────────────────────────────────────────
            var payslip = await payslipTask ?? new FinancialSnapshotDto { IsPayslipAvailable = false };

            // ── Active loans & advances ──────────────────────────────────────────
            var activeLoans = await activeLoansTask;
            var activeAdvances = await advancesTask;

            var activeLoansAdvances = new ActiveLoansAdvancesDto
            {
                TotalRemainingLoanAmount = activeLoans.Sum(l => l.RemainingAmount),
                TotalRemainingAdvanceAmount = activeAdvances.Sum(a => a.Amount),
                ActiveLoans = activeLoans,
                ActiveAdvances = activeAdvances
            };

            // ── Merge all pending requests, sorted newest first ───────────────────
            var allPending = new List<PendingRequestSummaryDto>();
            allPending.AddRange(await pendingLeaveTask);
            allPending.AddRange(await pendingShiftTask);
            allPending.AddRange(await pendingOtTask);
            allPending.AddRange(await pendingAdvTask);
            allPending.AddRange(await pendingLoanTask);
            allPending.AddRange(await pendingPfTask);
            allPending = allPending.OrderByDescending(r => r.SubmittedAt).ToList();

            return new EmployeeOverviewDto
            {
                LeaveBalances = leaveBalanceSummary,
                FinancialSnapshot = payslip,
                ActiveLoansAndAdvances = activeLoansAdvances,
                PendingRequests = allPending
            };

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<ManagerOverviewDto> GetManagerOverviewAsync(Guid userId, Guid organizationId)
    {
        try
        {
            var managerId = await _attendanceService.GetEmployeeFromTable(userId);
            var today = DateTime.UtcNow.Date;

            // Start all tasks concurrently
            var snapshotTask = _dashboardRepository.GetTeamAttendanceSnapshotAsync(managerId, organizationId, today);
            var overtimeTask = _dashboardRepository.GetTeamOvertimeCountTodayAsync(managerId, organizationId, today);
            var actionsTask = _dashboardRepository.GetPendingManagerActionsAsync(managerId, organizationId);

            await Task.WhenAll(snapshotTask, overtimeTask, actionsTask);

            return new ManagerOverviewDto
            {
                TeamAttendanceSnapshot = await snapshotTask,
                TeamOvertimeCountToday = await overtimeTask,
                PendingActions = await actionsTask
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<HrOverviewDto> GetHrOverviewAsync(Guid organizationId)
    {
        try
        {
            var payrollRunTask = _dashboardRepository.GetLatestPayrollRunStatusAsync(organizationId);
            var outflowTask = _dashboardRepository.GetLatestPayrollOutflowAsync(organizationId);
            var workforceTask = _dashboardRepository.GetWorkforceMetricsAsync(organizationId);

            await Task.WhenAll(payrollRunTask, outflowTask, workforceTask);

            return new HrOverviewDto
            {
                LatestPayrollRun = await payrollRunTask,
                TotalPayrollOutflow = await outflowTask,
                WorkforceMetrics = await workforceTask
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<HrStatsDto> GetHrStatsAsync(Guid organizationId)
    {
        try
        {
            var demographicsTask = _dashboardRepository.GetWorkforceDemographicsAsync(organizationId);
            var payrollTask = _dashboardRepository.GetLast3PayrollOutflowAsync(organizationId);
            var expensesTask = _dashboardRepository.GetLast3MonthsExpensesAsync(organizationId);

            await Task.WhenAll(demographicsTask, payrollTask, expensesTask);

            return new HrStatsDto
            {
                Demographics = await demographicsTask,
                PayrollOutflow = await payrollTask,
                MonthlyExpenses = await expensesTask
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
}
