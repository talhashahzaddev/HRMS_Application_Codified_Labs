using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPayslipPdfBuilder
{
    Task<byte[]> BuildAsync(
        PayrollResultItemDto row,
        string periodLabel,
        string currencyCode,
        PayslipTemplateDto? template = null);
}
