using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayrollRepository
{
    Task<PayrollPeriod> CreatePayrollPeriodAsync(Guid organizationId, string periodName, DateTime startDate, DateTime endDate, DateTime? payDate = null);
    Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId);
    Task<PayrollPeriod?> GetPayrollPeriodByIdAsync(Guid periodId);
    Task<int> CalculatePayrollAsync(Guid periodId, Guid? ruleId);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50);
    Task<PayrollEntry?> GetPayrollEntryAsync(Guid entryId);
    Task<bool> UpdatePayrollPeriodStatusAsync(Guid periodId, string status);
    Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId);
    Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference);
    
    // Salary Component Operations
    Task<SalaryComponent> CreateSalaryComponentAsync(Guid organizationId, CreateSalaryComponentRequest request);
    Task<List<SalaryComponentDto>> GetSalaryComponentsAsync(Guid organizationId);
    Task<SalaryComponentDto?> GetSalaryComponentByIdAsync(Guid componentId);
    Task<SalaryComponent> UpdateSalaryComponentAsync(Guid componentId, UpdateSalaryComponentRequest request);
    Task<bool> DeleteSalaryComponentAsync(Guid componentId);
    
    // Salary Rule Operations
    Task<SalaryRule> CreateSalaryRuleAsync(Guid organizationId, CreateSalaryRuleRequest request);
    Task<List<SalaryRuleDto>> GetSalaryRulesAsync(Guid organizationId);
    Task<SalaryRuleDto?> GetSalaryRuleByIdAsync(Guid ruleId);
    Task<SalaryRule> UpdateSalaryRuleAsync(Guid ruleId, UpdateSalaryRuleRequest request);
    Task<bool> DeleteSalaryRuleAsync(Guid ruleId);
    
    // Additional methods for complete implementation
    //Task<PayrollPeriodDto?> GetCurrentPayrollPeriodAsync(Guid organizationId);
    //Task<PayrollPeriod> UpdatePayrollPeriodAsync(Guid periodId, UpdatePayrollPeriodRequest request);
    //Task<bool> DeletePayrollPeriodAsync(Guid periodId);
    //Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesFilteredAsync(Guid organizationId, PayrollFilterRequest filter, int page, int limit);
    //Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesByEmployeeAsync(Guid employeeId, int page, int limit);
    //Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId);
    //Task<PayrollEntry> ApprovePayrollEntryAsync(Guid entryId);
    //Task<bool> ApproveMultiplePayrollEntriesAsync(List<Guid> entryIds);
    //Task<PayrollEntry> RejectPayrollEntryAsync(Guid entryId, string reason);
    //Task<PayrollEntry> MarkPayrollEntryAsPaidAsync(Guid entryId);
    //Task<bool> MarkMultiplePayrollEntriesAsPaidAsync(List<Guid> entryIds);
    //Task<PayrollSummaryDto> GetPayrollDashboardSummaryAsync(Guid organizationId);
    //Task<PayrollStatisticsDto> GetPayrollStatisticsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate);
    //Task<DepartmentPayrollBreakdownDto> GetDepartmentPayrollBreakdownAsync(Guid organizationId, Guid? payrollPeriodId);
    //Task<PayrollTaxSummaryDto> CalculateTaxDeductionsAsync(Guid employeeId, decimal grossPay, Guid payrollPeriodId);
    //Task<PayrollDeductionSummaryDto> GetDeductionSummaryAsync(Guid payrollPeriodId);
    
    // Additional methods for complete implementation
    Task<PayrollPeriodDto?> GetCurrentPayrollPeriodAsync(Guid organizationId);
    Task<PayrollPeriod> UpdatePayrollPeriodAsync(Guid periodId, UpdatePayrollPeriodRequest request);
    Task<bool> DeletePayrollPeriodAsync(Guid periodId);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesFilteredAsync(Guid organizationId, PayrollFilterRequest filter, int page, int limit);
    Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesByEmployeeAsync(Guid employeeId, int page, int limit);
    Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId);
    Task<PayrollEntry> ApprovePayrollEntryAsync(Guid entryId);
    Task<bool> ApproveMultiplePayrollEntriesAsync(List<Guid> entryIds);
    Task<PayrollEntry> RejectPayrollEntryAsync(Guid entryId, string reason);
    Task<PayrollEntry> MarkPayrollEntryAsPaidAsync(Guid entryId);
    Task<bool> MarkMultiplePayrollEntriesAsPaidAsync(List<Guid> entryIds);
    Task<PayrollSummaryDto> GetPayrollDashboardSummaryAsync(Guid organizationId);
    Task<PayrollSummaryDto> GetPayrollReportSummaryAsync(Guid organizationId, PayrollReportFilterRequest filter);
    Task<PayrollStatisticsDto> GetPayrollStatisticsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate);
    Task<DepartmentPayrollBreakdownDto> GetDepartmentPayrollBreakdownAsync(Guid organizationId, Guid? payrollPeriodId);
    Task<PayrollTaxSummaryDto> CalculateTaxDeductionsAsync(Guid employeeId, decimal grossPay, Guid payrollPeriodId);
    Task<PayrollDeductionSummaryDto> GetDeductionSummaryAsync(Guid payrollPeriodId);
    Task<List<PayrollProcessingHistoryDto>> GetProcessingHistoryAsync(Guid organizationId, int limit = 10);
}
