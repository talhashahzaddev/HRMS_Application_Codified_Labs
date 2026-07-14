using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Returns the complete employee personal overview:
    /// leave balances, financial snapshot (payslip), active loans/advances,
    /// and all pending requests across every module.
    /// </summary>
    Task<EmployeeOverviewDto> GetEmployeeOverviewAsync(Guid userId, Guid organizationId);

    /// <summary>
    /// Returns the manager overview: team attendance today, team overtime today, and pending approvals.
    /// </summary>
    Task<ManagerOverviewDto> GetManagerOverviewAsync(Guid managerId, Guid organizationId);

    /// <summary>
    /// Returns the HR and Finance overview: latest payroll run status, payroll outflow, and workforce metrics.
    /// </summary>
    Task<HrOverviewDto> GetHrOverviewAsync(Guid organizationId);

    /// <summary>
    /// Returns the deeper HR Stats: Workforce demographics, historical payroll outflow, and monthly expenses.
    /// </summary>
    Task<HrStatsDto> GetHrStatsAsync(Guid organizationId);

    /// <summary>
    /// Returns the latest 3 selected/hired candidates for the Recruitment pipeline.
    /// </summary>
    Task<List<LatestHiredEmployeeDto>> GetLatestHiresAsync(Guid organizationId);
}
