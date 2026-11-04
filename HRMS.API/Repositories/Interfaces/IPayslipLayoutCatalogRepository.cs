using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayslipLayoutCatalogRepository
{
    Task<IReadOnlyList<PayslipLayoutCatalogDto>> GetActiveLayoutsAsync();
    Task<PayslipLayoutCatalogDto?> GetByLayoutKeyAsync(string layoutKey);
    Task<bool> IsActiveLayoutKeyAsync(string layoutKey);
}
