using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Super Admin,HR Manager")]
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
    public async Task<ActionResult<ApiResponse<List<PayrollPeriodDto>>>> GetPayrollPeriods()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var periods = await _payrollService.GetPayrollPeriodsAsync(organizationId);
            
            return Ok(new ApiResponse<List<PayrollPeriodDto>>
            {
                Success = true,
                Data = periods
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll periods");
            return StatusCode(500, new ApiResponse<List<PayrollPeriodDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll periods"
            });
        }
    }

    [HttpGet("periods/{id}")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> GetPayrollPeriod(Guid id)
    {
        try
        {
            var period = await _payrollService.GetPayrollPeriodByIdAsync(id);
            if (period == null)
            {
                return NotFound(new ApiResponse<PayrollPeriodDto>
                {
                    Success = false,
                    Message = "Payroll period not found"
                });
            }

            return Ok(new ApiResponse<PayrollPeriodDto>
            {
                Success = true,
                Data = period
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll period {PeriodId}", id);
            return StatusCode(500, new ApiResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll period"
            });
        }
    }

    [HttpPost("periods")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> CreatePayrollPeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var period = await _payrollService.CreatePayrollPeriodAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetPayrollPeriod), new { id = period.PeriodId }, new ApiResponse<PayrollPeriodDto>
            {
                Success = true,
                Message = "Payroll period created successfully",
                Data = period
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return StatusCode(500, new ApiResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "An error occurred while creating payroll period"
            });
        }
    }

    [HttpPost("periods/{id}/calculate")]
    public async Task<ActionResult<ApiResponse<int>>> CalculatePayroll(Guid id)
    {
        try
        {
            var entriesCreated = await _payrollService.CalculatePayrollAsync(id);
            
            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = $"Payroll calculated successfully for {entriesCreated} employees",
                Data = entriesCreated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payroll for period {PeriodId}", id);
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Message = "An error occurred while calculating payroll"
            });
        }
    }

    [HttpGet("periods/{id}/entries")]
    public async Task<ActionResult<ApiResponse<PagedResult<PayrollEntryDto>>>> GetPayrollEntries(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var entries = await _payrollService.GetPayrollEntriesAsync(id, page, pageSize);
            
            return Ok(new ApiResponse<PagedResult<PayrollEntryDto>>
            {
                Success = true,
                Data = entries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll entries for period {PeriodId}", id);
            return StatusCode(500, new ApiResponse<PagedResult<PayrollEntryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll entries"
            });
        }
    }

    [HttpGet("periods/{id}/summary")]
    public async Task<ActionResult<ApiResponse<PayrollSummaryDto>>> GetPayrollSummary(Guid id)
    {
        try
        {
            var summary = await _payrollService.GetPayrollSummaryAsync(id);
            
            return Ok(new ApiResponse<PayrollSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll summary for period {PeriodId}", id);
            return StatusCode(500, new ApiResponse<PayrollSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll summary"
            });
        }
    }

    [HttpPost("entries/{id}/pay")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessPayment(Guid id, [FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var result = await _payrollService.ProcessPayrollPaymentAsync(id, request.PaymentMethod, request.PaymentReference);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Payment processed successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for entry {EntryId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while processing payment"
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        return Guid.Parse(orgIdClaim!);
    }
}

public class ProcessPaymentRequest
{
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
}
