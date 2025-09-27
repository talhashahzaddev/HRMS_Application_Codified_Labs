using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        IPayrollRepository payrollRepository,
        IMapper mapper,
        ILogger<PayrollService> logger)
    {
        _payrollRepository = payrollRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request, Guid organizationId)
    {
        var payrollPeriod = await _payrollRepository.CreatePayrollPeriodAsync(
            organizationId, request.PeriodName, request.StartDate, request.EndDate, request.PayDate);

        return _mapper.Map<PayrollPeriodDto>(payrollPeriod);
    }

    public async Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId)
    {
        return await _payrollRepository.GetPayrollPeriodsAsync(organizationId);
    }

    public async Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid periodId)
    {
        var period = await _payrollRepository.GetPayrollPeriodByIdAsync(periodId);
        return period != null ? _mapper.Map<PayrollPeriodDto>(period) : null;
    }

    public async Task<int> CalculatePayrollAsync(Guid periodId)
    {
        return await _payrollRepository.CalculatePayrollAsync(periodId);
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50)
    {
        return await _payrollRepository.GetPayrollEntriesAsync(periodId, page, pageSize);
    }

    public async Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId)
    {
        return await _payrollRepository.GetPayrollSummaryAsync(periodId);
    }

    public async Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference)
    {
        return await _payrollRepository.ProcessPayrollPaymentAsync(entryId, paymentMethod, paymentReference);
    }

    // New missing methods implementation
    public async Task<PayrollSummaryDto> GetPayrollDashboardSummaryAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting payroll dashboard summary for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetPayrollDashboardSummaryAsync(organizationId);
    }

    public async Task<PagedResult<PayrollPeriodDto>> GetPayrollPeriodsPagedAsync(Guid organizationId, int page, int limit, string? search)
    {
        _logger.LogInformation("Getting paged payroll periods for organization: {OrganizationId}, page: {Page}, limit: {Limit}", 
            organizationId, page, limit);
        
        var periods = await GetPayrollPeriodsAsync(organizationId);
        
        if (!string.IsNullOrEmpty(search))
        {
            periods = periods.Where(p => p.PeriodName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        var totalItems = periods.Count;
        var pagedItems = periods.Skip((page - 1) * limit).Take(limit).ToList();
        
        return new PagedResult<PayrollPeriodDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<PayrollPeriodDto?> GetCurrentPayrollPeriodAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting current payroll period for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetCurrentPayrollPeriodAsync(organizationId);
    }

    public async Task<PayrollPeriodDto> UpdatePayrollPeriodAsync(Guid periodId, UpdatePayrollPeriodRequest request)
    {
        _logger.LogInformation("Updating payroll period: {PeriodId}", periodId);
        
        var existingPeriod = await _payrollRepository.GetPayrollPeriodByIdAsync(periodId);
        if (existingPeriod == null)
            throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");

        var updatedPeriod = await _payrollRepository.UpdatePayrollPeriodAsync(periodId, request);
        return _mapper.Map<PayrollPeriodDto>(updatedPeriod);
    }

    public async Task<bool> DeletePayrollPeriodAsync(Guid periodId)
    {
        _logger.LogInformation("Deleting payroll period: {PeriodId}", periodId);
        
        var period = await _payrollRepository.GetPayrollPeriodByIdAsync(periodId);
        if (period == null)
            throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");

        return await _payrollRepository.DeletePayrollPeriodAsync(periodId);
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesFilteredAsync(Guid organizationId, PayrollFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting filtered payroll entries for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetPayrollEntriesFilteredAsync(organizationId, filter, page, limit);
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesByEmployeeAsync(Guid employeeId, int page, int limit)
    {
        _logger.LogInformation("Getting payroll entries for employee: {EmployeeId}", employeeId);
        return await _payrollRepository.GetPayrollEntriesByEmployeeAsync(employeeId, page, limit);
    }

    public async Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId)
    {
        _logger.LogInformation("Getting payroll entry by ID: {EntryId}", entryId);
        return await _payrollRepository.GetPayrollEntryByIdAsync(entryId);
    }

    public async Task<PayrollEntryDto> ApprovePayrollEntryAsync(Guid entryId)
    {
        _logger.LogInformation("Approving payroll entry: {EntryId}", entryId);
        
        var entry = await _payrollRepository.ApprovePayrollEntryAsync(entryId);
        return _mapper.Map<PayrollEntryDto>(entry);
    }

    public async Task<bool> ApproveMultiplePayrollEntriesAsync(List<Guid> entryIds)
    {
        _logger.LogInformation("Approving multiple payroll entries: {Count}", entryIds.Count);
        return await _payrollRepository.ApproveMultiplePayrollEntriesAsync(entryIds);
    }

    public async Task<PayrollEntryDto> RejectPayrollEntryAsync(Guid entryId, string reason)
    {
        _logger.LogInformation("Rejecting payroll entry: {EntryId} with reason: {Reason}", entryId, reason);
        
        var entry = await _payrollRepository.RejectPayrollEntryAsync(entryId, reason);
        return _mapper.Map<PayrollEntryDto>(entry);
    }

    public async Task<PayrollEntryDto> MarkPayrollEntryAsPaidAsync(Guid entryId)
    {
        _logger.LogInformation("Marking payroll entry as paid: {EntryId}", entryId);
        
        var entry = await _payrollRepository.MarkPayrollEntryAsPaidAsync(entryId);
        return _mapper.Map<PayrollEntryDto>(entry);
    }

    public async Task<bool> MarkMultiplePayrollEntriesAsPaidAsync(List<Guid> entryIds)
    {
        _logger.LogInformation("Marking multiple payroll entries as paid: {Count}", entryIds.Count);
        return await _payrollRepository.MarkMultiplePayrollEntriesAsPaidAsync(entryIds);
    }

    public async Task<PayrollReportDto> GeneratePayrollReportAsync(Guid organizationId, PayrollReportFilterRequest filter)
    {
        _logger.LogInformation("Generating payroll report for organization: {OrganizationId}", organizationId);
        
        // Get filtered entries
        var entriesFilter = new PayrollFilterRequest
        {
            PayrollPeriodId = filter.PayrollPeriodId,
            Department = filter.Department,
            Status = filter.Status
        };
        
        var entries = await _payrollRepository.GetPayrollEntriesFilteredAsync(organizationId, entriesFilter, 1, 1000);
        
        // Get summary data
        var summary = await GetPayrollDashboardSummaryAsync(organizationId);
        
        // Get department breakdown
        var departmentBreakdown = await GetDepartmentPayrollBreakdownAsync(organizationId, filter.PayrollPeriodId);
        
        return new PayrollReportDto
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate = filter.EndDate ?? DateTime.Now,
            Entries = entries.Data,
            Summary = summary,
            DepartmentBreakdown = new List<DepartmentPayrollBreakdownDto> { departmentBreakdown }
        };
    }

    public async Task<byte[]> ExportPayrollReportAsync(Guid organizationId, PayrollReportFilterRequest filter, string format)
    {
        _logger.LogInformation("Exporting payroll report for organization: {OrganizationId} in format: {Format}", organizationId, format);
        
        var report = await GeneratePayrollReportAsync(organizationId, filter);
        
        // Generate CSV export with real data
        var csvData = "Employee,EmployeeCode,BasicSalary,GrossSalary,TaxDeductions,NetSalary,Status\n";
        
        foreach (var entry in report.Entries)
        {
            csvData += $"{entry.EmployeeName},{entry.EmployeeCode},{entry.BasicSalary},{entry.GrossSalary},{entry.TaxAmount},{entry.NetSalary},{entry.Status}\n";
        }
        
        return System.Text.Encoding.UTF8.GetBytes(csvData);
    }

    public async Task<bool> GeneratePayslipsAsync(Guid payrollPeriodId, List<Guid>? employeeIds)
    {
        _logger.LogInformation("Generating payslips for period: {PeriodId}", payrollPeriodId);
        
        // Mock implementation
        return true;
    }

    public async Task<byte[]> DownloadPayslipAsync(Guid payrollEntryId)
    {
        _logger.LogInformation("Downloading payslip for entry: {EntryId}", payrollEntryId);
        
        // Mock PDF payslip content
        var pdfContent = "Mock PDF payslip content for entry " + payrollEntryId;
        return System.Text.Encoding.UTF8.GetBytes(pdfContent);
    }

    public async Task<bool> EmailPayslipsAsync(Guid payrollPeriodId, List<Guid>? employeeIds)
    {
        _logger.LogInformation("Emailing payslips for period: {PeriodId}", payrollPeriodId);
        
        // Mock email sending
        return true;
    }

    public async Task<PayrollStatisticsDto> GetPayrollStatisticsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate)
    {
        _logger.LogInformation("Getting payroll statistics for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetPayrollStatisticsAsync(organizationId, startDate, endDate);
    }

    public async Task<DepartmentPayrollBreakdownDto> GetDepartmentPayrollBreakdownAsync(Guid organizationId, Guid? payrollPeriodId)
    {
        _logger.LogInformation("Getting department payroll breakdown for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetDepartmentPayrollBreakdownAsync(organizationId, payrollPeriodId);
    }

    public async Task<PayrollTaxSummaryDto> CalculateTaxDeductionsAsync(Guid employeeId, decimal grossPay, Guid payrollPeriodId)
    {
        _logger.LogInformation("Calculating tax deductions for employee: {EmployeeId}, gross pay: {GrossPay}", employeeId, grossPay);
        return await _payrollRepository.CalculateTaxDeductionsAsync(employeeId, grossPay, payrollPeriodId);
    }

    public async Task<PayrollDeductionSummaryDto> GetDeductionSummaryAsync(Guid payrollPeriodId)
    {
        _logger.LogInformation("Getting deduction summary for period: {PeriodId}", payrollPeriodId);
        return await _payrollRepository.GetDeductionSummaryAsync(payrollPeriodId);
    }
}
