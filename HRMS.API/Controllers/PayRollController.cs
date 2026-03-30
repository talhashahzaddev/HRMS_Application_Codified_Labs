using HRMS.API.Models.DTOs;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayRollController : ControllerBase
{
    private readonly IPayRollService _payRollService;
    private readonly ILogger<PayRollController> _logger;
    private readonly IAttendanceService _attendanceService ;

    public PayRollController(IPayRollService payRollService, ILogger<PayRollController> logger,IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
        _payRollService = payRollService;
        _logger = logger;
    }

    [HttpGet("rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayRollRulesGroupedDto>>> GetAllRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllRulesAsync(organizationId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while retrieving payroll rules");
            return StatusCode(403, new ServiceResponse<PayRollRulesGroupedDto>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll rules");
            return StatusCode(500, new ServiceResponse<PayRollRulesGroupedDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll rules"
            });
        }
    }

    [HttpGet("overtime-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<OvertimeRuleDto>>>> GetAllOvertimeRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllOvertimeRulesAsync(organizationId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overtime rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overtime-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<OvertimeRuleDto>>>> GetActiveOvertimeRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveOvertimeRulesAsync(organizationId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active overtime rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overtime-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<OvertimeRuleDto>>> GetOvertimeRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetOvertimeRuleByIdAsync(id, organizationId);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overtime rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("overtime-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<OvertimeRuleDto>>> CreateOvertimeRule([FromBody] CreateOvertimeRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateOvertimeRuleAsync(organizationId, dto);

            if (!response.Success)
                return BadRequest(response);

            return CreatedAtAction(nameof(GetOvertimeRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime rule");
            return StatusCode(500, new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("overtime-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<OvertimeRuleDto>>> UpdateOvertimeRule(Guid id, [FromBody] UpdateOvertimeRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateOvertimeRuleAsync(id, organizationId, dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overtime rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("overtime-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteOvertimeRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.ToggleOvertimeRuleDeletionAsync(id, organizationId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overtime rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("overtime-rules/{id}/toggle-status")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> ToggleOvertimeRuleStatus(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.ToggleOvertimeRuleStatusAsync(id, organizationId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling overtime rule status: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overtime-entries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<OvertimeEntryDto>>>> GetAllOvertimeEntries([FromQuery] OvertimeEntryFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllOvertimeEntriesAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overtime entries");
            return StatusCode(500, new ServiceResponse<PagedResult<OvertimeEntryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overtime-entries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<OvertimeEntryDto>>> GetOvertimeEntryById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetOvertimeEntryByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overtime entry by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("overtime-entries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<OvertimeEntryDto>>> CreateOvertimeEntry([FromBody] CreateOvertimeEntryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateOvertimeEntryAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetOvertimeEntryById), new { id = response.Data?.OvertimeId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime entry");
            return StatusCode(500, new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("overtime-entries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<OvertimeEntryDto>>> UpdateOvertimeEntry(Guid id, [FromBody] UpdateOvertimeEntryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateOvertimeEntryAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overtime entry: {Id}", id);
            return StatusCode(500, new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("overtime-entries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteOvertimeEntry(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteOvertimeEntryAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overtime entry: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Payroll Periods
    [HttpGet("periods")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodPagedResult>>> GetAllPayrollPeriods([FromQuery] PayrollPeriodFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllPayrollPeriodsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll periods");
            return StatusCode(500, new ServiceResponse<PayrollPeriodPagedResult> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("periods/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> GetPayrollPeriodById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetPayrollPeriodByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll period by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("periods")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> CreatePayrollPeriod([FromBody] CreatePayrollPeriodDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreatePayrollPeriodAsync(organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetPayrollPeriodById), new { id = response.Data?.PeriodId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("periods/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollPeriodDto>>> UpdatePayrollPeriod(Guid id, [FromBody] UpdatePayrollPeriodDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdatePayrollPeriodAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payroll period: {Id}", id);
            return StatusCode(500, new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("periods/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeletePayrollPeriod(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeletePayrollPeriodAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll period: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }
    private async Task<Guid> GetCurrentEmployeeId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        var employeeId = await _attendanceService.GetEmployeeFromTable(userId);
        return employeeId;
    }

    private Guid GetCurrentOrganizationId()    {
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
}
