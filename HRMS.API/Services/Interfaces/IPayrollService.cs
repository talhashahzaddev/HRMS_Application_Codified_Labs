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
    
    // Additional methods based on frontend consumption
    Task<PayrollSummaryDto> GetPayrollDashboardSummaryAsync(Guid organizationId);
    Task<PayrollSummaryDto> GetPayrollReportSummaryAsync(Guid organizationId, PayrollReportFilterRequest filter);
    Task<PagedResult<PayrollPeriodDto>> GetPayrollPeriodsPagedAsync(Guid organizationId, int page, int limit, string? search);
    Task<PayrollPeriodDto?> GetCurrentPayrollPeriodAsync(Guid organizationId);
    Task<PayrollPeriodDto> UpdatePayrollPeriodAsync(Guid periodId, UpdatePayrollPeriodRequest request);
    Task<bool> DeletePayrollPeriodAsync(Guid periodId);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesFilteredAsync(Guid organizationId, PayrollFilterRequest filter, int page, int limit);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesByEmployeeAsync(Guid employeeId, int page, int limit);
    Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId);
    Task<PayrollEntryDto> ApprovePayrollEntryAsync(Guid entryId);
    Task<bool> ApproveMultiplePayrollEntriesAsync(List<Guid> entryIds);
    Task<PayrollEntryDto> RejectPayrollEntryAsync(Guid entryId, string reason);
    Task<PayrollEntryDto> MarkPayrollEntryAsPaidAsync(Guid entryId);
    Task<bool> MarkMultiplePayrollEntriesAsPaidAsync(List<Guid> entryIds);
    Task<PayrollReportDto> GeneratePayrollReportAsync(Guid organizationId, PayrollReportFilterRequest filter);
    Task<byte[]> ExportPayrollReportAsync(Guid organizationId, PayrollReportFilterRequest filter, string format);
    Task<bool> GeneratePayslipsAsync(Guid payrollPeriodId, List<Guid>? employeeIds);
    Task<byte[]> DownloadPayslipAsync(Guid payrollEntryId);
    Task<bool> EmailPayslipsAsync(Guid payrollPeriodId, List<Guid>? employeeIds);
    Task<PayrollStatisticsDto> GetPayrollStatisticsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate);
    Task<DepartmentPayrollBreakdownDto> GetDepartmentPayrollBreakdownAsync(Guid organizationId, Guid? payrollPeriodId);
    Task<PayrollTaxSummaryDto> CalculateTaxDeductionsAsync(Guid employeeId, decimal grossPay, Guid payrollPeriodId);
    Task<PayrollDeductionSummaryDto> GetDeductionSummaryAsync(Guid payrollPeriodId);
    Task<List<PayrollProcessingHistoryDto>> GetProcessingHistoryAsync(Guid organizationId, int limit = 10);
}
