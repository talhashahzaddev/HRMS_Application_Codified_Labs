using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPayrollService
{
    Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request, Guid organizationId);
    Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId);
    Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid periodId);
    Task<int> CalculatePayrollAsync(Guid periodId);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50);
    Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId);
    Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference);
}
