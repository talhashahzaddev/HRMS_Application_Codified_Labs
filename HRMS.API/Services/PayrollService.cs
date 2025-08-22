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
        
        // Mock implementation
        return new PayrollSummaryDto
        {
            TotalGrossSalary = 1250000m,
            TotalTax = 187500m,
            TotalNetSalary = 1062500m,
            TotalEmployees = 150,
            DepartmentWisePayroll = new Dictionary<string, decimal>
            {
                { "Engineering", 600000m },
                { "Sales", 300000m },
                { "Marketing", 200000m },
                { "HR", 100000m },
                { "Finance", 50000m }
            }
        };
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
        
        var periods = await GetPayrollPeriodsAsync(organizationId);
        var currentDate = DateTime.Now;
        
        return periods.FirstOrDefault(p => p.StartDate <= currentDate && p.EndDate >= currentDate);
    }

    public async Task<PayrollPeriodDto> UpdatePayrollPeriodAsync(Guid periodId, UpdatePayrollPeriodRequest request)
    {
        _logger.LogInformation("Updating payroll period: {PeriodId}", periodId);
        
        var existingPeriod = await GetPayrollPeriodByIdAsync(periodId);
        if (existingPeriod == null)
            throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");

        // Mock update
        existingPeriod.PeriodName = request.PeriodName;
        existingPeriod.StartDate = request.StartDate;
        existingPeriod.EndDate = request.EndDate;
        existingPeriod.Status = request.Status;
        
        return existingPeriod;
    }

    public async Task<bool> DeletePayrollPeriodAsync(Guid periodId)
    {
        _logger.LogInformation("Deleting payroll period: {PeriodId}", periodId);
        
        var period = await GetPayrollPeriodByIdAsync(periodId);
        if (period == null)
            throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");

        // Mock deletion
        return true;
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesFilteredAsync(Guid organizationId, PayrollFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting filtered payroll entries for organization: {OrganizationId}", organizationId);
        
        // Mock implementation
        var entries = new List<PayrollEntryDto>
        {
            new() { EntryId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), EmployeeName = "John Doe", EmployeeCode = "EMP001", BasicSalary = 5000, GrossSalary = 5500, NetSalary = 4400, Status = "Processed" },
            new() { EntryId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), EmployeeName = "Jane Smith", EmployeeCode = "EMP002", BasicSalary = 6000, GrossSalary = 6600, NetSalary = 5280, Status = "Approved" },
            new() { EntryId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), EmployeeName = "Bob Wilson", EmployeeCode = "EMP003", BasicSalary = 4500, GrossSalary = 4950, NetSalary = 3960, Status = "Pending" }
        };

        if (filter.EmployeeId.HasValue)
            entries = entries.Where(e => e.EmployeeId == filter.EmployeeId).ToList();
        if (!string.IsNullOrEmpty(filter.Status))
            entries = entries.Where(e => e.Status == filter.Status).ToList();
        if (!string.IsNullOrEmpty(filter.Search))
            entries = entries.Where(e => e.EmployeeName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalItems = entries.Count;
        var pagedItems = entries.Skip((page - 1) * limit).Take(limit).ToList();
        
        return new PagedResult<PayrollEntryDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesByEmployeeAsync(Guid employeeId, int page, int limit)
    {
        _logger.LogInformation("Getting payroll entries for employee: {EmployeeId}", employeeId);
        
        // Mock implementation
        var entries = new List<PayrollEntryDto>
        {
            new() { EntryId = Guid.NewGuid(), EmployeeId = employeeId, EmployeeName = "Employee Name", EmployeeCode = "EMP001", BasicSalary = 5000, GrossSalary = 5500, NetSalary = 4400, Status = "Paid" },
            new() { EntryId = Guid.NewGuid(), EmployeeId = employeeId, EmployeeName = "Employee Name", EmployeeCode = "EMP001", BasicSalary = 5000, GrossSalary = 5500, NetSalary = 4400, Status = "Paid" }
        };

        var totalItems = entries.Count;
        var pagedItems = entries.Skip((page - 1) * limit).Take(limit).ToList();
        
        return new PagedResult<PayrollEntryDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId)
    {
        _logger.LogInformation("Getting payroll entry by ID: {EntryId}", entryId);
        
        // Mock implementation
        return new PayrollEntryDto
        {
            EntryId = entryId,
            EmployeeId = Guid.NewGuid(),
            EmployeeName = "John Doe",
            EmployeeCode = "EMP001",
            BasicSalary = 5000,
            GrossSalary = 5500,
            NetSalary = 4400,
            Status = "Processed",
            Currency = "USD"
        };
    }

    public async Task<PayrollEntryDto> ApprovePayrollEntryAsync(Guid entryId)
    {
        _logger.LogInformation("Approving payroll entry: {EntryId}", entryId);
        
        var entry = await GetPayrollEntryByIdAsync(entryId);
        if (entry == null)
            throw new KeyNotFoundException($"Payroll entry with ID {entryId} not found");

        entry.Status = "Approved";
        return entry;
    }

    public async Task<bool> ApproveMultiplePayrollEntriesAsync(List<Guid> entryIds)
    {
        _logger.LogInformation("Approving multiple payroll entries: {Count}", entryIds.Count);
        
        var successCount = 0;
        foreach (var entryId in entryIds)
        {
            try
            {
                await ApprovePayrollEntryAsync(entryId);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to approve payroll entry {EntryId}", entryId);
            }
        }
        
        return successCount == entryIds.Count;
    }

    public async Task<PayrollEntryDto> RejectPayrollEntryAsync(Guid entryId, string reason)
    {
        _logger.LogInformation("Rejecting payroll entry: {EntryId} with reason: {Reason}", entryId, reason);
        
        var entry = await GetPayrollEntryByIdAsync(entryId);
        if (entry == null)
            throw new KeyNotFoundException($"Payroll entry with ID {entryId} not found");

        entry.Status = "Rejected";
        return entry;
    }

    public async Task<PayrollEntryDto> MarkPayrollEntryAsPaidAsync(Guid entryId)
    {
        _logger.LogInformation("Marking payroll entry as paid: {EntryId}", entryId);
        
        var entry = await GetPayrollEntryByIdAsync(entryId);
        if (entry == null)
            throw new KeyNotFoundException($"Payroll entry with ID {entryId} not found");

        entry.Status = "Paid";
        entry.PaidAt = DateTime.UtcNow;
        return entry;
    }

    public async Task<bool> MarkMultiplePayrollEntriesAsPaidAsync(List<Guid> entryIds)
    {
        _logger.LogInformation("Marking multiple payroll entries as paid: {Count}", entryIds.Count);
        
        var successCount = 0;
        foreach (var entryId in entryIds)
        {
            try
            {
                await MarkPayrollEntryAsPaidAsync(entryId);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark payroll entry as paid {EntryId}", entryId);
            }
        }
        
        return successCount == entryIds.Count;
    }

    public async Task<PayrollReportDto> GeneratePayrollReportAsync(Guid organizationId, PayrollReportFilterRequest filter)
    {
        _logger.LogInformation("Generating payroll report for organization: {OrganizationId}", organizationId);
        
        // Mock implementation
        return new PayrollReportDto
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate = filter.EndDate ?? DateTime.Now,
            Entries = new List<PayrollEntryDto>(),
            Summary = await GetPayrollDashboardSummaryAsync(organizationId),
            DepartmentBreakdown = new List<DepartmentPayrollBreakdownDto>
            {
                new() { DepartmentName = "Engineering", TotalAmount = 600000m, EmployeeCount = 60, AverageAmount = 10000m, Percentage = 48.0m },
                new() { DepartmentName = "Sales", TotalAmount = 300000m, EmployeeCount = 30, AverageAmount = 10000m, Percentage = 24.0m }
            }
        };
    }

    public async Task<byte[]> ExportPayrollReportAsync(Guid organizationId, PayrollReportFilterRequest filter, string format)
    {
        _logger.LogInformation("Exporting payroll report for organization: {OrganizationId} in format: {Format}", organizationId, format);
        
        var report = await GeneratePayrollReportAsync(organizationId, filter);
        
        // Mock CSV export
        var csvData = "Employee,BasicSalary,GrossSalary,TaxDeductions,NetSalary,Status\n";
        csvData += "John Doe,5000,5500,1100,4400,Paid\n";
        csvData += "Jane Smith,6000,6600,1320,5280,Paid\n";
        
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
        
        return new PayrollStatisticsDto
        {
            TotalPayroll = 1250000m,
            AveragePayroll = 8333m,
            TotalEmployees = 150,
            TotalDeductions = 187500m,
            TotalAllowances = 62500m,
            MonthlyTrends = new List<MonthlyPayrollDto>
            {
                new() { Month = "Jan 2024", Amount = 1200000m, EmployeeCount = 145 },
                new() { Month = "Feb 2024", Amount = 1250000m, EmployeeCount = 150 },
                new() { Month = "Mar 2024", Amount = 1300000m, EmployeeCount = 155 }
            }
        };
    }

    public async Task<DepartmentPayrollBreakdownDto> GetDepartmentPayrollBreakdownAsync(Guid organizationId, Guid? payrollPeriodId)
    {
        _logger.LogInformation("Getting department payroll breakdown for organization: {OrganizationId}", organizationId);
        
        // Mock implementation
        return new DepartmentPayrollBreakdownDto
        {
            DepartmentName = "Engineering",
            TotalAmount = 600000m,
            EmployeeCount = 60,
            AverageAmount = 10000m,
            Percentage = 48.0m
        };
    }

    public async Task<PayrollTaxSummaryDto> CalculateTaxDeductionsAsync(Guid employeeId, decimal grossPay, Guid payrollPeriodId)
    {
        _logger.LogInformation("Calculating tax deductions for employee: {EmployeeId}, gross pay: {GrossPay}", employeeId, grossPay);
        
        var incomeTax = grossPay * 0.15m;
        var socialSecurity = grossPay * 0.062m;
        var medicare = grossPay * 0.0145m;
        var totalDeductions = incomeTax + socialSecurity + medicare;
        
        return new PayrollTaxSummaryDto
        {
            GrossPay = grossPay,
            IncomeTax = incomeTax,
            SocialSecurity = socialSecurity,
            Medicare = medicare,
            TotalDeductions = totalDeductions,
            NetPay = grossPay - totalDeductions
        };
    }

    public async Task<PayrollDeductionSummaryDto> GetDeductionSummaryAsync(Guid payrollPeriodId)
    {
        _logger.LogInformation("Getting deduction summary for period: {PeriodId}", payrollPeriodId);
        
        return new PayrollDeductionSummaryDto
        {
            TotalDeductions = 187500m,
            TaxDeductions = 150000m,
            InsuranceDeductions = 25000m,
            LoanDeductions = 7500m,
            OtherDeductions = 5000m,
            DeductionItems = new List<DeductionItemDto>
            {
                new() { Name = "Federal Income Tax", Amount = 100000m, Type = "Tax" },
                new() { Name = "State Income Tax", Amount = 50000m, Type = "Tax" },
                new() { Name = "Health Insurance", Amount = 25000m, Type = "Insurance" },
                new() { Name = "Employee Loans", Amount = 7500m, Type = "Loan" }
            }
        };
    }
}
