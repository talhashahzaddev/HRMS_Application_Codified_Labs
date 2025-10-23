using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;

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
        _logger.LogInformation("Starting payroll calculation for period: {PeriodId}", periodId);
        
        try
        {
            // Validate period exists and is in correct status
            var period = await _payrollRepository.GetPayrollPeriodByIdAsync(periodId);
            if (period == null)
            {
                throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");
            }

            if (period.Status != "draft")
            {
                throw new InvalidOperationException($"Cannot calculate payroll for period in status: {period.Status}");
            }

            // Call the database function to calculate payroll
            var entriesCreated = await _payrollRepository.CalculatePayrollAsync(periodId);
            
            // Update period status to calculated
            await _payrollRepository.UpdatePayrollPeriodStatusAsync(periodId, "calculated");
            
            _logger.LogInformation("Payroll calculation completed for period {PeriodId}. Created {EntriesCount} entries", 
                periodId, entriesCreated);
            
            return entriesCreated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payroll for period {PeriodId}", periodId);
            throw;
        }
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
    
    // Salary Component Operations
    public async Task<SalaryComponentDto> CreateSalaryComponentAsync(CreateSalaryComponentRequest request, Guid organizationId)
    {
        var component = await _payrollRepository.CreateSalaryComponentAsync(
            organizationId, 
            request);
            
        return _mapper.Map<SalaryComponentDto>(component);
    }
    
    public async Task<List<SalaryComponentDto>> GetSalaryComponentsAsync(Guid organizationId)
    {
        return await _payrollRepository.GetSalaryComponentsAsync(organizationId);
    }
    
    public async Task<SalaryComponentDto?> GetSalaryComponentByIdAsync(Guid componentId)
    {
        var component = await _payrollRepository.GetSalaryComponentByIdAsync(componentId);
        return component != null ? _mapper.Map<SalaryComponentDto>(component) : null;
    }
    
    public async Task<SalaryComponentDto> UpdateSalaryComponentAsync(Guid componentId, UpdateSalaryComponentRequest request)
    {
        var component = await _payrollRepository.UpdateSalaryComponentAsync(
            componentId,
            request);
            
        return _mapper.Map<SalaryComponentDto>(component);
    }
    
    public async Task<bool> DeleteSalaryComponentAsync(Guid componentId)
    {
        return await _payrollRepository.DeleteSalaryComponentAsync(componentId);
    }
    
    // Salary Rule Operations
    public async Task<SalaryRuleDto> CreateSalaryRuleAsync(CreateSalaryRuleRequest request, Guid organizationId)
    {
        var rule = await _payrollRepository.CreateSalaryRuleAsync(
            organizationId,
            request);
            
        return _mapper.Map<SalaryRuleDto>(rule);
    }
    
    public async Task<List<SalaryRuleDto>> GetSalaryRulesAsync(Guid organizationId)
    {
        return await _payrollRepository.GetSalaryRulesAsync(organizationId);
    }
    
    public async Task<SalaryRuleDto?> GetSalaryRuleByIdAsync(Guid ruleId)
    {
        var rule = await _payrollRepository.GetSalaryRuleByIdAsync(ruleId);
        return rule != null ? _mapper.Map<SalaryRuleDto>(rule) : null;
    }
    
    public async Task<SalaryRuleDto> UpdateSalaryRuleAsync(Guid ruleId, UpdateSalaryRuleRequest request)
    {
        var rule = await _payrollRepository.UpdateSalaryRuleAsync(
            ruleId,
            request);
            
        return _mapper.Map<SalaryRuleDto>(rule);
    }
    
    public async Task<bool> DeleteSalaryRuleAsync(Guid ruleId)
    {
        return await _payrollRepository.DeleteSalaryRuleAsync(ruleId);
    }

    // New missing methods implementation
    public async Task<PayrollSummaryDto> GetPayrollDashboardSummaryAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting payroll dashboard summary for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetPayrollDashboardSummaryAsync(organizationId);
    }

    public async Task<PayrollSummaryDto> GetPayrollReportSummaryAsync(Guid organizationId, PayrollReportFilterRequest filter)
    {
        _logger.LogInformation("Getting payroll report summary for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetPayrollReportSummaryAsync(organizationId, filter);
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
        
        // Get summary data based on filtered entries
        var summary = await GetPayrollReportSummaryAsync(organizationId, filter);
        
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
        
        return format.ToLower() switch
        {
            "excel" => await GenerateExcelReportAsync(report),
            "pdf" => await GeneratePdfReportAsync(report),
            _ => await GenerateExcelReportAsync(report) // Default to Excel
        };
    }

    private async Task<byte[]> GenerateExcelReportAsync(PayrollReportDto report)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Payroll Report");

        // Add title
        worksheet.Cell("A1").Value = "Payroll Report";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;
        worksheet.Range("A1:H1").Merge();

        // Add report period
        worksheet.Cell("A2").Value = $"Report Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}";
        worksheet.Cell("A2").Style.Font.Bold = true;
        worksheet.Range("A2:H2").Merge();

        // Add summary section
        worksheet.Cell("A4").Value = "Summary";
        worksheet.Cell("A4").Style.Font.Bold = true;
        worksheet.Cell("A4").Style.Font.FontSize = 14;

        worksheet.Cell("A5").Value = "Total Employees:";
        worksheet.Cell("B5").Value = report.Summary.TotalEmployees;
        worksheet.Cell("A6").Value = "Total Gross Salary:";
        worksheet.Cell("B6").Value = report.Summary.TotalGrossSalary;
        worksheet.Cell("A7").Value = "Total Tax:";
        worksheet.Cell("B7").Value = report.Summary.TotalTax;
        worksheet.Cell("A8").Value = "Total Net Salary:";
        worksheet.Cell("B8").Value = report.Summary.TotalNetSalary;

        // Add detailed entries
        worksheet.Cell("A10").Value = "Detailed Entries";
        worksheet.Cell("A10").Style.Font.Bold = true;
        worksheet.Cell("A10").Style.Font.FontSize = 14;

        // Headers
        var headers = new[] { "Employee Name", "Employee Code", "Department", "Basic Salary", "Gross Salary", "Tax Amount", "Net Salary", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(11, i + 1).Value = headers[i];
            worksheet.Cell(11, i + 1).Style.Font.Bold = true;
            worksheet.Cell(11, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        int row = 12;
        foreach (var entry in report.Entries)
        {
            worksheet.Cell(row, 1).Value = entry.EmployeeName;
            worksheet.Cell(row, 2).Value = entry.EmployeeCode;
            worksheet.Cell(row, 3).Value = entry.DepartmentName;
            worksheet.Cell(row, 4).Value = entry.BasicSalary;
            worksheet.Cell(row, 5).Value = entry.GrossSalary;
            worksheet.Cell(row, 6).Value = entry.TaxAmount;
            worksheet.Cell(row, 7).Value = entry.NetSalary;
            worksheet.Cell(row, 8).Value = entry.Status;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> GeneratePdfReportAsync(PayrollReportDto report)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4, 25, 25, 30, 30);
        var writer = PdfWriter.GetInstance(document, memoryStream);

        document.Open();

        // Add title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
        var title = new Paragraph("Payroll Report", titleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20
        };
        document.Add(title);

        // Add report period
        var periodFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
        var period = new Paragraph($"Report Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}", periodFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20
        };
        document.Add(period);

        // Add summary section
        var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
        var summaryTitle = new Paragraph("Summary", summaryFont)
        {
            SpacingAfter = 10
        };
        document.Add(summaryTitle);

        var summaryTable = new PdfPTable(2);
        summaryTable.WidthPercentage = 50;
        summaryTable.SetWidths(new float[] { 2f, 1f });

        var cell1 = new PdfPCell(new Phrase("Total Employees:", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell1.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell1);
        
        var cell2 = new PdfPCell(new Phrase(report.Summary.TotalEmployees.ToString(), FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell2.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell2);

        var cell3 = new PdfPCell(new Phrase("Total Gross Salary:", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell3.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell3);
        
        var cell4 = new PdfPCell(new Phrase(report.Summary.TotalGrossSalary.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell4.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell4);

        var cell5 = new PdfPCell(new Phrase("Total Tax:", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell5.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell5);
        
        var cell6 = new PdfPCell(new Phrase(report.Summary.TotalTax.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell6.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell6);

        var cell7 = new PdfPCell(new Phrase("Total Net Salary:", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell7.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell7);
        
        var cell8 = new PdfPCell(new Phrase(report.Summary.TotalNetSalary.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        cell8.Border = PdfPCell.NO_BORDER;
        summaryTable.AddCell(cell8);

        document.Add(summaryTable);

        // Add detailed entries
        var detailsTitle = new Paragraph("Detailed Entries", summaryFont)
        {
            SpacingBefore = 20,
            SpacingAfter = 10
        };
        document.Add(detailsTitle);

        var detailsTable = new PdfPTable(8);
        detailsTable.WidthPercentage = 100;
        detailsTable.SetWidths(new float[] { 2f, 1.5f, 1.5f, 1f, 1f, 1f, 1f, 1f });

        // Headers
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
        var headerCells = new[] { "Employee Name", "Employee Code", "Department", "Basic Salary", "Gross Salary", "Tax Amount", "Net Salary", "Status" };
        
        foreach (var header in headerCells)
        {
            var cell = new PdfPCell(new Phrase(header, headerFont));
            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            detailsTable.AddCell(cell);
        }

        // Data rows
        var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
        foreach (var entry in report.Entries)
        {
            var nameCell = new PdfPCell(new Phrase(entry.EmployeeName, dataFont));
            nameCell.Padding = 3;
            detailsTable.AddCell(nameCell);
            
            var codeCell = new PdfPCell(new Phrase(entry.EmployeeCode, dataFont));
            codeCell.Padding = 3;
            detailsTable.AddCell(codeCell);
            
            var deptCell = new PdfPCell(new Phrase(entry.DepartmentName, dataFont));
            deptCell.Padding = 3;
            detailsTable.AddCell(deptCell);
            
            var basicCell = new PdfPCell(new Phrase(entry.BasicSalary.ToString("C"), dataFont));
            basicCell.Padding = 3;
            basicCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            detailsTable.AddCell(basicCell);
            
            var grossCell = new PdfPCell(new Phrase(entry.GrossSalary.ToString("C"), dataFont));
            grossCell.Padding = 3;
            grossCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            detailsTable.AddCell(grossCell);
            
            var taxCell = new PdfPCell(new Phrase(entry.TaxAmount.ToString("C"), dataFont));
            taxCell.Padding = 3;
            taxCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            detailsTable.AddCell(taxCell);
            
            var netCell = new PdfPCell(new Phrase(entry.NetSalary.ToString("C"), dataFont));
            netCell.Padding = 3;
            netCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            detailsTable.AddCell(netCell);
            
            var statusCell = new PdfPCell(new Phrase(entry.Status, dataFont));
            statusCell.Padding = 3;
            statusCell.HorizontalAlignment = Element.ALIGN_CENTER;
            detailsTable.AddCell(statusCell);
        }

        document.Add(detailsTable);

        document.Close();
        return memoryStream.ToArray();
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

    public async Task<List<PayrollProcessingHistoryDto>> GetProcessingHistoryAsync(Guid organizationId, int limit = 10)
    {
        _logger.LogInformation("Getting processing history for organization: {OrganizationId}", organizationId);
        return await _payrollRepository.GetProcessingHistoryAsync(organizationId, limit);
    }
}
