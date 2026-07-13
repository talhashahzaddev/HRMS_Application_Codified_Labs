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
}
