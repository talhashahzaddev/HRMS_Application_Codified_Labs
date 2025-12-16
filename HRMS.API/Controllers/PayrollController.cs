using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using HRMS.API.Requests;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin,HR Manager,Admin")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(IPayrollService payrollService, ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _logger = logger;
    }

    [HttpGet("periods")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PayrollPeriodDto>>>> GetPayrollPeriods(
        [FromQuery] int page = 1, 
        [FromQuery] int limit = 20, 
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _payrollService.GetPayrollPeriodsPagedAsync(organizationId, page, limit, search, status);
            
            return Ok(new ServiceResponse<PagedResult<PayrollPeriodDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll periods");
            return StatusCode(500, new ServiceResponse<PagedResult<PayrollPeriodDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll periods"
            });
        }
    }

    [HttpGet("periods/{id}")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> GetPayrollPeriod(Guid id)
    {
        try
        {
            var period = await _payrollService.GetPayrollPeriodByIdAsync(id);
            if (period == null)
            {
                return NotFound(new ServiceResponse<PayrollPeriodDto>
                {
                    Success = false,
                    Message = "Payroll period not found"
                });
            }

            return Ok(new ServiceResponse<PayrollPeriodDto>
            {
                Success = true,
                Data = period
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll period"
            });
        }
    }

    [HttpPost("periods")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> CreatePayrollPeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var period = await _payrollService.CreatePayrollPeriodAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetPayrollPeriod), new { id = period.PeriodId }, new ServiceResponse<PayrollPeriodDto>
            {
                Success = true,
                Message = "Payroll period created successfully",
                Data = period
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "An error occurred while creating payroll period"
            });
        }
    }
    [HttpPost("periods/{id}/calculate")]
    public async Task<ActionResult<ServiceResponse<PayrollCalculationResult>>> CalculatePayroll(Guid id, [FromBody] CalculatePayrollRequest? request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var entriesCreated = await _payrollService.CalculatePayrollAsync(
                id, 
                organizationId,
                request?.RuleId, 
                request?.DepartmentIds, 
                request?.IncludeTax ?? false
            );
            
            // Get updated period details
            var period = await _payrollService.GetPayrollPeriodByIdAsync(id);
            
            return Ok(new ServiceResponse<PayrollCalculationResult>
            {
                Success = true,
                Message = $"Payroll calculated successfully for {entriesCreated} employees",
                Data = new PayrollCalculationResult
                {
                    EntriesCreated = entriesCreated,
                    PeriodId = id,
                    TotalEmployees = period?.TotalEmployees ?? 0,
                    TotalGrossAmount = period?.TotalGrossAmount ?? 0,
                    TotalTaxAmount = period?.TotalTaxAmount ?? 0,
                    TotalNetAmount = period?.TotalNetAmount ?? 0,
                    CalculatedAt = DateTime.UtcNow
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<PayrollCalculationResult>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<PayrollCalculationResult>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payroll for period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<PayrollCalculationResult>
            {
                Success = false,
                Message = "An error occurred while calculating payroll"
            });
        }
    }

    [HttpGet("periods/{id}/entries")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PayrollEntryDto>>>> GetPayrollEntries(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var entries = await _payrollService.GetPayrollEntriesAsync(id, page, pageSize);
            
            return Ok(new ServiceResponse<PagedResult<PayrollEntryDto>>
            {
                Success = true,
                Data = entries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll entries for period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<PagedResult<PayrollEntryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll entries"
            });
        }
    }

    [HttpGet("periods/{id}/summary")]
    public async Task<ActionResult<ServiceResponse<PayrollSummaryDto>>> GetPayrollSummary(Guid id)
    {
        try
        {
            var summary = await _payrollService.GetPayrollSummaryAsync(id);
            
            return Ok(new ServiceResponse<PayrollSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll summary for period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<PayrollSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll summary"
            });
        }
    }

    [HttpPost("entries/{id}/pay")]
    public async Task<ActionResult<ServiceResponse<bool>>> ProcessPayment(Guid id, [FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var result = await _payrollService.ProcessPayrollPaymentAsync(id, request.PaymentMethod, request.PaymentReference);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Payment processed successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for entry {EntryId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while processing payment"
            });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ServiceResponse<PayrollSummaryDto>>> GetPayrollDashboardSummary()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var summary = await _payrollService.GetPayrollDashboardSummaryAsync(organizationId);
            
            return Ok(new ServiceResponse<PayrollSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll summary");
            return StatusCode(500, new ServiceResponse<PayrollSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll summary"
            });
        }
    }

    [HttpGet("periods/current")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> GetCurrentPayrollPeriod()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var period = await _payrollService.GetCurrentPayrollPeriodAsync(organizationId);
            
            if (period == null)
            {
                return NotFound(new ServiceResponse<PayrollPeriodDto>
                {
                    Success = false,
                    Message = "No current payroll period found"
                });
            }

            return Ok(new ServiceResponse<PayrollPeriodDto>
            {
                Success = true,
                Data = period
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current payroll period");
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "An error occurred while retrieving current payroll period"
            });
        }
    }

    [HttpPut("periods/{id}")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> UpdatePayrollPeriod(Guid id, [FromBody] UpdatePayrollPeriodRequest request)
    {
        try
        {
            var period = await _payrollService.UpdatePayrollPeriodAsync(id, request);
            
            return Ok(new ServiceResponse<PayrollPeriodDto>
            {
                Success = true,
                Message = "Payroll period updated successfully",
                Data = period
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payroll period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "An error occurred while updating payroll period"
            });
        }
    }

    [HttpDelete("periods/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeletePayrollPeriod(Guid id)
    {
        try
        {
            var result = await _payrollService.DeletePayrollPeriodAsync(id);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Payroll period deleted successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting payroll period"
            });
        }
    }

    [HttpGet("entries")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PayrollEntryDto>>>> GetPayrollEntriesFiltered([FromQuery] PayrollFilterRequest filter, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var entries = await _payrollService.GetPayrollEntriesFilteredAsync(organizationId, filter, page, limit);
            
            return Ok(new ServiceResponse<PagedResult<PayrollEntryDto>>
            {
                Success = true,
                Data = entries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered payroll entries");
            return StatusCode(500, new ServiceResponse<PagedResult<PayrollEntryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll entries"
            });
        }
    }

    [HttpGet("employees/{employeeId}/entries")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PayrollEntryDto>>>> GetPayrollEntriesByEmployee(Guid employeeId, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        try
        {
            var entries = await _payrollService.GetPayrollEntriesByEmployeeAsync(employeeId, page, limit);
            
            return Ok(new ServiceResponse<PagedResult<PayrollEntryDto>>
            {
                Success = true,
                Data = entries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll entries for employee {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<PagedResult<PayrollEntryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee payroll entries"
            });
        }
    }

    [HttpGet("entries/{id}")]
    public async Task<ActionResult<ServiceResponse<PayrollEntryDto>>> GetPayrollEntry(Guid id)
    {
        try
        {
            var entry = await _payrollService.GetPayrollEntryByIdAsync(id);
            
            if (entry == null)
            {
                return NotFound(new ServiceResponse<PayrollEntryDto>
                {
                    Success = false,
                    Message = "Payroll entry not found"
                });
            }

            return Ok(new ServiceResponse<PayrollEntryDto>
            {
                Success = true,
                Data = entry
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll entry {EntryId}", id);
            return StatusCode(500, new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll entry"
            });
        }
    }

    [HttpPut("entries/{id}/approve")]
    public async Task<ActionResult<ServiceResponse<PayrollEntryDto>>> ApprovePayrollEntry(Guid id)
    {
        try
        {
            var entry = await _payrollService.ApprovePayrollEntryAsync(id);
            
            return Ok(new ServiceResponse<PayrollEntryDto>
            {
                Success = true,
                Message = "Payroll entry approved successfully",
                Data = entry
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payroll entry {EntryId}", id);
            return StatusCode(500, new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = "An error occurred while approving payroll entry"
            });
        }
    }

    [HttpPut("entries/approve-multiple")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveMultiplePayrollEntries([FromBody] BulkPayrollRequest request)
    {
        try
        {
            var result = await _payrollService.ApproveMultiplePayrollEntriesAsync(request.EntryIds);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully approved {request.EntryIds.Count} payroll entries",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving multiple payroll entries");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while approving payroll entries"
            });
        }
    }

    [HttpPut("entries/{id}/reject")]
    public async Task<ActionResult<ServiceResponse<PayrollEntryDto>>> RejectPayrollEntry(Guid  id, [FromBody] RejectPayrollRequest request)
    {
        try
        {
            var entry = await _payrollService.RejectPayrollEntryAsync(id, request.Reason);
            
            return Ok(new ServiceResponse<PayrollEntryDto>
            {
                Success = true,
                Message = "Payroll entry rejected successfully",
                Data = entry
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting payroll entry {EntryId}", id);
            return StatusCode(500, new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = "An error occurred while rejecting payroll entry"
            });
        }
    }

    [HttpPut("entries/{id}/mark-paid")]
    public async Task<ActionResult<ServiceResponse<PayrollEntryDto>>> MarkPayrollEntryAsPaid(Guid id)
    {
        try
        {
            var entry = await _payrollService.MarkPayrollEntryAsPaidAsync(id);
            
            return Ok(new ServiceResponse<PayrollEntryDto>
            {
                Success = true,
                Message = "Payroll entry marked as paid successfully",
                Data = entry
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payroll entry as paid {EntryId}", id);
            return StatusCode(500, new ServiceResponse<PayrollEntryDto>
            {
                Success = false,
                Message = "An error occurred while marking payroll entry as paid"
            });
        }
    }

    [HttpPut("entries/mark-paid-multiple")]
    public async Task<ActionResult<ServiceResponse<bool>>> MarkMultiplePayrollEntriesAsPaid([FromBody] BulkPayrollRequest request)
    {
        try
        {
            var result = await _payrollService.MarkMultiplePayrollEntriesAsPaidAsync(request.EntryIds);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully marked {request.EntryIds.Count} payroll entries as paid",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking multiple payroll entries as paid");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while marking payroll entries as paid"
            });
        }
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ServiceResponse<PayrollReportDto>>> GeneratePayrollReport([FromQuery] PayrollReportFilterRequest filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var report = await _payrollService.GeneratePayrollReportAsync(organizationId, filter);
            
            return Ok(new ServiceResponse<PayrollReportDto>
            {
                Success = true,
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payroll report");
            return StatusCode(500, new ServiceResponse<PayrollReportDto>
            {
                Success = false,
                Message = "An error occurred while generating payroll report"
            });
        }
    }

    [HttpGet("reports/export")]
    public async Task<IActionResult> ExportPayrollReport([FromQuery] PayrollReportFilterRequest filter, [FromQuery] string format = "excel")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var fileBytes = await _payrollService.ExportPayrollReportAsync(organizationId, filter, format);
            
            var formatLower = format.ToLower();
            var contentType = formatLower switch
            {
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
            
            var fileExtension = formatLower switch
            {
                "excel" => "xlsx",
                "pdf" => "pdf",
                _ => "xlsx"
            };
            
            var fileName = $"payroll-report-{DateTime.Now:yyyy-MM-dd}.{fileExtension}";
            
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payroll report");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while exporting payroll report"
            });
        }
    }

    [HttpPost("generate-payslips")]
    public async Task<ActionResult<ServiceResponse<bool>>> GeneratePayslips([FromBody] GeneratePayslipsRequest request)
    {
        try
        {
            var result = await _payrollService.GeneratePayslipsAsync(request.PayrollPeriodId, request.EmployeeIds);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Payslips generated successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payslips");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while generating payslips"
            });
        }
    }

    [HttpGet("entries/{id}/payslip")]
    public async Task<IActionResult> DownloadPayslip(Guid id)
    {
        try
        {
            var fileBytes = await _payrollService.DownloadPayslipAsync(id);
            
            return File(fileBytes, "application/pdf", $"payslip-{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading payslip for entry {EntryId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while downloading payslip"
            });
        }
    }

    [HttpPost("email-payslips")]
    public async Task<ActionResult<ServiceResponse<bool>>> EmailPayslips([FromBody] GeneratePayslipsRequest request)
    {
        try
        {
            var result = await _payrollService.EmailPayslipsAsync(request.PayrollPeriodId, request.EmployeeIds);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Payslips emailed successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emailing payslips");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while emailing payslips"
            });
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ServiceResponse<PayrollStatisticsDto>>> GetPayrollStatistics(
        [FromQuery] DateTime? startDate, 
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? payrollPeriodId = null,
        [FromQuery] string? department = null)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var statistics = await _payrollService.GetPayrollStatisticsAsync(organizationId, startDate, endDate, payrollPeriodId, department);
            
            return Ok(new ServiceResponse<PayrollStatisticsDto>
            {
                Success = true,
                Data = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll statistics");
            return StatusCode(500, new ServiceResponse<PayrollStatisticsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll statistics"
            });
        }
    }

    [HttpGet("department-breakdown")]
    public async Task<ActionResult<ServiceResponse<List<DepartmentPayrollBreakdownDto>>>> GetDepartmentPayrollBreakdown([FromQuery] Guid? payrollPeriodId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var breakdown = await _payrollService.GetDepartmentPayrollBreakdownAsync(organizationId, payrollPeriodId);
            
            return Ok(new ServiceResponse<List<DepartmentPayrollBreakdownDto>>
            {
                Success = true,
                Data = breakdown
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department payroll breakdown");
            return StatusCode(500, new ServiceResponse<List<DepartmentPayrollBreakdownDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving department payroll breakdown"
            });
        }
    }

    [HttpPost("calculate-tax")]
    public async Task<ActionResult<ServiceResponse<PayrollTaxSummaryDto>>> CalculateTaxDeductions([FromBody] CalculateTaxRequest request)
    {
        try
        {
            var taxSummary = await _payrollService.CalculateTaxDeductionsAsync(request.EmployeeId, request.GrossPay, request.PayrollPeriodId);
            
            return Ok(new ServiceResponse<PayrollTaxSummaryDto>
            {
                Success = true,
                Data = taxSummary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating tax deductions");
            return StatusCode(500, new ServiceResponse<PayrollTaxSummaryDto>
            {
                Success = false,
                Message = "An error occurred while calculating tax deductions"
            });
        }
    }

    [HttpGet("periods/{id}/deduction-summary")]
    public async Task<ActionResult<ServiceResponse<PayrollDeductionSummaryDto>>> GetDeductionSummary(Guid id)
    {
        try
        {
            var summary = await _payrollService.GetDeductionSummaryAsync(id);
            
            return Ok(new ServiceResponse<PayrollDeductionSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deduction summary for period {PeriodId}", id);
            return StatusCode(500, new ServiceResponse<PayrollDeductionSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving deduction summary"
            });
        }
    }

    [HttpGet("processing-history")]
    public async Task<ActionResult<ServiceResponse<List<PayrollProcessingHistoryDto>>>> GetProcessingHistory([FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var history = await _payrollService.GetProcessingHistoryAsync(organizationId, limit);
            
            return Ok(new ServiceResponse<List<PayrollProcessingHistoryDto>>
            {
                Success = true,
                Data = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processing history");
            return StatusCode(500, new ServiceResponse<List<PayrollProcessingHistoryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving processing history"
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        
        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }
        
        return organizationId;
    }
    
    #region Salary Components
    
    [HttpGet("components")]
    public async Task<ActionResult<ServiceResponse<List<SalaryComponentDto>>>> GetSalaryComponents()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var components = await _payrollService.GetSalaryComponentsAsync(organizationId);
            
            return Ok(new ServiceResponse<List<SalaryComponentDto>>
            {
                Success = true,
                Data = components
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary components");
            return StatusCode(500, new ServiceResponse<List<SalaryComponentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving salary components"
            });
        }
    }
    
    [HttpGet("components/{id}")]
    public async Task<ActionResult<ServiceResponse<SalaryComponentDto>>> GetSalaryComponent(Guid id)
    {
        try
        {
            var component = await _payrollService.GetSalaryComponentByIdAsync(id);
            if (component == null)
            {
                return NotFound(new ServiceResponse<SalaryComponentDto>
                {
                    Success = false,
                    Message = "Salary component not found"
                });
            }
            
            return Ok(new ServiceResponse<SalaryComponentDto>
            {
                Success = true,
                Data = component
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary component");
            return StatusCode(500, new ServiceResponse<SalaryComponentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the salary component"
            });
        }
    }
    
    [HttpPost("components")]
    public async Task<ActionResult<ServiceResponse<SalaryComponentDto>>> CreateSalaryComponent(CreateSalaryComponentRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var component = await _payrollService.CreateSalaryComponentAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetSalaryComponent), new { id = component.ComponentId }, 
                new ServiceResponse<SalaryComponentDto>
                {
                    Success = true,
                    Data = component,
                    Message = "Salary component created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary component");
            return StatusCode(500, new ServiceResponse<SalaryComponentDto>
            {
                Success = false,
                Message = "An error occurred while creating the salary component"
            });
        }
    }
    
    [HttpPut("components/{id}")]
    public async Task<ActionResult<ServiceResponse<SalaryComponentDto>>> UpdateSalaryComponent(Guid id, UpdateSalaryComponentRequest request)
    {
        try
        {
            var component = await _payrollService.UpdateSalaryComponentAsync(id, request);
            
            return Ok(new ServiceResponse<SalaryComponentDto>
            {
                Success = true,
                Data = component,
                Message = "Salary component updated successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ServiceResponse<SalaryComponentDto>
            {
                Success = false,
                Message = "Salary component not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary component");
            return StatusCode(500, new ServiceResponse<SalaryComponentDto>
            {
                Success = false,
                Message = "An error occurred while updating the salary component"
            });
        }
    }
    
    [HttpDelete("components/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSalaryComponent(Guid id)
    {
        try
        {
            var result = await _payrollService.DeleteSalaryComponentAsync(id);
            
            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Salary component not found"
                });
            }
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Salary component deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary component");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the salary component"
            });
        }
    }
    
    #endregion
    
    #region Salary Rules
    
    [HttpGet("rules")]
    public async Task<ActionResult<ServiceResponse<List<SalaryRuleDto>>>> GetSalaryRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var rules = await _payrollService.GetSalaryRulesAsync(organizationId);
            
            return Ok(new ServiceResponse<List<SalaryRuleDto>>
            {
                Success = true,
                Data = rules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary rules");
            return StatusCode(500, new ServiceResponse<List<SalaryRuleDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving salary rules"
            });
        }
    }
    
    [HttpGet("rules/{id}")]
    public async Task<ActionResult<ServiceResponse<SalaryRuleDto>>> GetSalaryRule(Guid id)
    {
        try
        {
            var rule = await _payrollService.GetSalaryRuleByIdAsync(id);
            if (rule == null)
            {
                return NotFound(new ServiceResponse<SalaryRuleDto>
                {
                    Success = false,
                    Message = "Salary rule not found"
                });
            }
            
            return Ok(new ServiceResponse<SalaryRuleDto>
            {
                Success = true,
                Data = rule
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary rule");
            return StatusCode(500, new ServiceResponse<SalaryRuleDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the salary rule"
            });
        }
    }
    
    [HttpPost("rules")]
    public async Task<ActionResult<ServiceResponse<SalaryRuleDto>>> CreateSalaryRule(CreateSalaryRuleRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var rule = await _payrollService.CreateSalaryRuleAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetSalaryRule), new { id = rule.RuleId }, 
                new ServiceResponse<SalaryRuleDto>
                {
                    Success = true,
                    Data = rule,
                    Message = "Salary rule created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary rule");
            return StatusCode(500, new ServiceResponse<SalaryRuleDto>
            {
                Success = false,
                Message = "An error occurred while creating the salary rule"
            });
        }
    }
    
    [HttpPut("rules/{id}")]
    public async Task<ActionResult<ServiceResponse<SalaryRuleDto>>> UpdateSalaryRule(Guid id, UpdateSalaryRuleRequest request)
    {
        try
        {
            var rule = await _payrollService.UpdateSalaryRuleAsync(id, request);
            
            return Ok(new ServiceResponse<SalaryRuleDto>
            {
                Success = true,
                Data = rule,
                Message = "Salary rule updated successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ServiceResponse<SalaryRuleDto>
            {
                Success = false,
                Message = "Salary rule not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary rule");
            return StatusCode(500, new ServiceResponse<SalaryRuleDto>
            {
                Success = false,
                Message = "An error occurred while updating the salary rule"
            });
        }
    }
    
    [HttpDelete("rules/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSalaryRule(Guid id)
    {
        try
        {
            var result = await _payrollService.DeleteSalaryRuleAsync(id);
            
            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Salary rule not found"
                });
            }
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Salary rule deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary rule");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the salary rule"
            });
        }
    }
    
    #endregion
}

public class ProcessPaymentRequest
{
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
}
