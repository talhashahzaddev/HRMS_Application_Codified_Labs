using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayrollRepository
{
    Task<PayrollPeriod> CreatePayrollPeriodAsync(Guid organizationId, string periodName, DateTime startDate, DateTime endDate, DateTime? payDate = null);
    Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId);
    Task<PayrollPeriod?> GetPayrollPeriodByIdAsync(Guid periodId);
    Task<int> CalculatePayrollAsync(Guid periodId);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50);
    Task<PayrollEntry?> GetPayrollEntryAsync(Guid entryId);
    Task<bool> UpdatePayrollPeriodStatusAsync(Guid periodId, string status);
    Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId);
    Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference);
}
