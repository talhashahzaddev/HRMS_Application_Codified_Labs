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
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
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

    // Attendance Deduction Rules
    [HttpGet("attendance-deduction-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>>>> GetAllAttendanceDeductionRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllAttendanceDeductionRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance deduction rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("attendance-deduction-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>>>> GetActiveAttendanceDeductionRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveAttendanceDeductionRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active attendance deduction rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("attendance-deduction-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<AttendanceDeductionRuleDto>>> GetAttendanceDeductionRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAttendanceDeductionRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance deduction rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("attendance-deduction-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<AttendanceDeductionRuleDto>>> CreateAttendanceDeductionRule([FromBody] CreateAttendanceDeductionRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateAttendanceDeductionRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetAttendanceDeductionRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance deduction rule");
            return StatusCode(500, new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("attendance-deduction-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<AttendanceDeductionRuleDto>>> UpdateAttendanceDeductionRule(Guid id, [FromBody] UpdateAttendanceDeductionRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateAttendanceDeductionRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance deduction rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("attendance-deduction-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAttendanceDeductionRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteAttendanceDeductionRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance deduction rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Late Arrival Rules
    [HttpGet("late-arrival-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LateArrivalRuleDto>>>> GetAllLateArrivalRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllLateArrivalRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving late arrival rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<LateArrivalRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("late-arrival-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LateArrivalRuleDto>>>> GetActiveLateArrivalRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveLateArrivalRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active late arrival rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<LateArrivalRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("late-arrival-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LateArrivalRuleDto>>> GetLateArrivalRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLateArrivalRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving late arrival rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("late-arrival-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LateArrivalRuleDto>>> CreateLateArrivalRule([FromBody] CreateLateArrivalRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateLateArrivalRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetLateArrivalRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating late arrival rule");
            return StatusCode(500, new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("late-arrival-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LateArrivalRuleDto>>> UpdateLateArrivalRule(Guid id, [FromBody] UpdateLateArrivalRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateLateArrivalRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating late arrival rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("late-arrival-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteLateArrivalRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteLateArrivalRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting late arrival rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Leave Rules
    [HttpGet("leave-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LeaveRuleDto>>>> GetAllLeaveRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllLeaveRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<LeaveRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("leave-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LeaveRuleDto>>>> GetActiveLeaveRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveLeaveRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active leave rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<LeaveRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("leave-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LeaveRuleDto>>> GetLeaveRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLeaveRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<LeaveRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("leave-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LeaveRuleDto>>> CreateLeaveRule([FromBody] CreateLeaveRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateLeaveRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetLeaveRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave rule");
            return StatusCode(500, new ServiceResponse<LeaveRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("leave-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LeaveRuleDto>>> UpdateLeaveRule(Guid id, [FromBody] UpdateLeaveRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateLeaveRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<LeaveRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("leave-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteLeaveRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteLeaveRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Late Attendance
    [HttpGet("late-attendance")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<LateAttendanceDto>>>> GetAllLateAttendance([FromQuery] LateAttendanceFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllLateAttendanceAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving late attendance records");
            return StatusCode(500, new ServiceResponse<PagedResult<LateAttendanceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("late-attendance/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LateAttendanceDto>>> GetLateAttendanceById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLateAttendanceByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving late attendance record by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<LateAttendanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("late-attendance")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LateAttendanceDto>>> CreateLateAttendance([FromBody] CreateLateAttendanceDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateLateAttendanceAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetLateAttendanceById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating late attendance record");
            return StatusCode(500, new ServiceResponse<LateAttendanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("late-attendance/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LateAttendanceDto>>> UpdateLateAttendance(Guid id, [FromBody] UpdateLateAttendanceDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateLateAttendanceAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating late attendance record: {Id}", id);
            return StatusCode(500, new ServiceResponse<LateAttendanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("late-attendance/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteLateAttendance(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteLateAttendanceAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting late attendance record: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Attendance Summary
    [HttpGet("attendance-summaries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>>>> GetAllAttendanceSummaries([FromQuery] PayrollAttendanceSummaryFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllAttendanceSummariesAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance summaries");
            return StatusCode(500, new ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("attendance-summaries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollAttendanceSummaryDto>>> GetAttendanceSummaryById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAttendanceSummaryByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance summary by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("attendance-summaries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollAttendanceSummaryDto>>> CreateAttendanceSummary([FromBody] CreatePayrollAttendanceSummaryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateAttendanceSummaryAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetAttendanceSummaryById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance summary row");
            return StatusCode(500, new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("attendance-summaries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollAttendanceSummaryDto>>> UpdateAttendanceSummary(Guid id, [FromBody] UpdatePayrollAttendanceSummaryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateAttendanceSummaryAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance summary row: {Id}", id);
            return StatusCode(500, new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("attendance-summaries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAttendanceSummary(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteAttendanceSummaryAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance summary row: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Leave Summary
    [HttpGet("leave-summaries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<LeaveSummaryDto>>>> GetAllLeaveSummaries([FromQuery] LeaveSummaryFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLeaveSummariesAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave summaries");
            return StatusCode(500, new ServiceResponse<PagedResult<LeaveSummaryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("leave-summaries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LeaveSummaryDto>>> GetLeaveSummaryById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLeaveSummaryByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave summary by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("leave-summaries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LeaveSummaryDto>>> CreateLeaveSummary([FromBody] CreateLeaveSummaryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateLeaveSummaryAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetLeaveSummaryById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave summary");
            return StatusCode(500, new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("leave-summaries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LeaveSummaryDto>>> UpdateLeaveSummary(Guid id, [FromBody] UpdateLeaveSummaryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateLeaveSummaryAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave summary: {Id}", id);
            return StatusCode(500, new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("leave-summaries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteLeaveSummary(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteLeaveSummaryAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave summary: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }


    // Performance Rules
    [HttpGet("performance-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<PerformanceRuleDto>>>> GetAllPerformanceRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllPerformanceRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<PerformanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("performance-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<PerformanceRuleDto>>>> GetActivePerformanceRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActivePerformanceRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active performance rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<PerformanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("performance-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PerformanceRuleDto>>> GetPerformanceRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetPerformanceRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("performance-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PerformanceRuleDto>>> CreatePerformanceRule([FromBody] CreatePerformanceRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreatePerformanceRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetPerformanceRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance rule");
            return StatusCode(500, new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("performance-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PerformanceRuleDto>>> UpdatePerformanceRule(Guid id, [FromBody] UpdatePerformanceRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdatePerformanceRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("performance-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeletePerformanceRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeletePerformanceRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Bonus Rules
    [HttpGet("bonus-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<BonusRuleDto>>>> GetAllBonusRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllBonusRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bonus rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<BonusRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("bonus-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<BonusRuleDto>>>> GetActiveBonusRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveBonusRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active bonus rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<BonusRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("bonus-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<BonusRuleDto>>> GetBonusRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetBonusRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bonus rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<BonusRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("bonus-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<BonusRuleDto>>> CreateBonusRule([FromBody] CreateBonusRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateBonusRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetBonusRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bonus rule");
            return StatusCode(500, new ServiceResponse<BonusRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("bonus-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<BonusRuleDto>>> UpdateBonusRule(Guid id, [FromBody] UpdateBonusRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateBonusRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bonus rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<BonusRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("bonus-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteBonusRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteBonusRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bonus rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Performance Pay (entries)
    [HttpGet("performance-pay")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PerformancePayDto>>>> GetAllPerformancePay([FromQuery] PerformancePayFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllPerformancePayAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance pay records");
            return StatusCode(500, new ServiceResponse<PagedResult<PerformancePayDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("performance-pay/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PerformancePayDto>>> GetPerformancePayById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetPerformancePayByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance pay by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<PerformancePayDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("performance-pay")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PerformancePayDto>>> CreatePerformancePay([FromBody] CreatePerformancePayDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreatePerformancePayAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetPerformancePayById), new { id = response.Data?.PerformancePayId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance pay");
            return StatusCode(500, new ServiceResponse<PerformancePayDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("performance-pay/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PerformancePayDto>>> UpdatePerformancePay(Guid id, [FromBody] UpdatePerformancePayDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdatePerformancePayAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance pay: {Id}", id);
            return StatusCode(500, new ServiceResponse<PerformancePayDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("performance-pay/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeletePerformancePay(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeletePerformancePayAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance pay: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Bonus Entries
    [HttpGet("bonus-entries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<BonusEntryDto>>>> GetAllBonusEntries([FromQuery] BonusEntryFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllBonusEntriesAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bonus entries");
            return StatusCode(500, new ServiceResponse<PagedResult<BonusEntryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("bonus-entries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<BonusEntryDto>>> GetBonusEntryById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetBonusEntryByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bonus entry by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<BonusEntryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("bonus-entries")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<BonusEntryDto>>> CreateBonusEntry([FromBody] CreateBonusEntryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateBonusEntryAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetBonusEntryById), new { id = response.Data?.BonusId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bonus entry");
            return StatusCode(500, new ServiceResponse<BonusEntryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("bonus-entries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<BonusEntryDto>>> UpdateBonusEntry(Guid id, [FromBody] UpdateBonusEntryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateBonusEntryAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bonus entry: {Id}", id);
            return StatusCode(500, new ServiceResponse<BonusEntryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("bonus-entries/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteBonusEntry(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteBonusEntryAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bonus entry: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Loan Rules
    [HttpGet("loan-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LoanRuleDto>>>> GetAllLoanRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllLoanRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<LoanRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loan-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LoanRuleDto>>>> GetActiveLoanRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveLoanRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active loan rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<LoanRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loan-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanRuleDto>>> GetLoanRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLoanRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<LoanRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("loan-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanRuleDto>>> CreateLoanRule([FromBody] CreateLoanRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateLoanRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetLoanRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan rule");
            return StatusCode(500, new ServiceResponse<LoanRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("loan-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanRuleDto>>> UpdateLoanRule(Guid id, [FromBody] UpdateLoanRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateLoanRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<LoanRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("loan-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteLoanRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteLoanRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loan rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Salary Advance Rules
    [HttpGet("salary-advance-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>>>> GetAllSalaryAdvanceRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllSalaryAdvanceRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary advance rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>>>> GetActiveSalaryAdvanceRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveSalaryAdvanceRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active salary advance rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<SalaryAdvanceRuleDto>>> GetSalaryAdvanceRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSalaryAdvanceRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary advance rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("salary-advance-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<SalaryAdvanceRuleDto>>> CreateSalaryAdvanceRule([FromBody] CreateSalaryAdvanceRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSalaryAdvanceRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetSalaryAdvanceRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary advance rule");
            return StatusCode(500, new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("salary-advance-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<SalaryAdvanceRuleDto>>> UpdateSalaryAdvanceRule(Guid id, [FromBody] UpdateSalaryAdvanceRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSalaryAdvanceRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary advance rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("salary-advance-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSalaryAdvanceRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSalaryAdvanceRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary advance rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Provident Fund Rules
    [HttpGet("provident-fund-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ProvidentFundRuleDto>>>> GetAllProvidentFundRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllProvidentFundRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provident fund rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<ProvidentFundRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("provident-fund-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ProvidentFundRuleDto>>>> GetActiveProvidentFundRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveProvidentFundRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active provident fund rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<ProvidentFundRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("provident-fund-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRuleDto>>> GetProvidentFundRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetProvidentFundRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provident fund rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("provident-fund-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRuleDto>>> CreateProvidentFundRule([FromBody] CreateProvidentFundRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateProvidentFundRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetProvidentFundRuleById), new { id = response.Data?.RuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund rule");
            return StatusCode(500, new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("provident-fund-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRuleDto>>> UpdateProvidentFundRule(Guid id, [FromBody] UpdateProvidentFundRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateProvidentFundRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provident fund rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("provident-fund-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteProvidentFundRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteProvidentFundRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provident fund rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Tax Regimes
    [HttpGet("tax-regimes")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxRegimeDto>>>> GetAllTaxRegimes()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllTaxRegimesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax regimes");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxRegimeDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-regimes/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxRegimeDto>>>> GetActiveTaxRegimes()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveTaxRegimesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tax regimes");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxRegimeDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-regimes/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxRegimeDto>>> GetTaxRegimeById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetTaxRegimeByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax regime by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxRegimeDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("tax-regimes")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxRegimeDto>>> CreateTaxRegime([FromBody] CreateTaxRegimeDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateTaxRegimeAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetTaxRegimeById), new { id = response.Data?.RegimeId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax regime");
            return StatusCode(500, new ServiceResponse<TaxRegimeDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("tax-regimes/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxRegimeDto>>> UpdateTaxRegime(Guid id, [FromBody] UpdateTaxRegimeDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateTaxRegimeAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax regime: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxRegimeDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("tax-regimes/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteTaxRegime(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteTaxRegimeAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax regime: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Tax Categories
    [HttpGet("tax-categories")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxCategoryDto>>>> GetAllTaxCategories()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllTaxCategoriesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax categories");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-categories/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxCategoryDto>>>> GetActiveTaxCategories()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveTaxCategoriesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tax categories");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-categories/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxCategoryDto>>> GetTaxCategoryById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetTaxCategoryByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax category by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxCategoryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("tax-categories")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxCategoryDto>>> CreateTaxCategory([FromBody] CreateTaxCategoryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateTaxCategoryAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetTaxCategoryById), new { id = response.Data?.CategoryId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax category");
            return StatusCode(500, new ServiceResponse<TaxCategoryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("tax-categories/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxCategoryDto>>> UpdateTaxCategory(Guid id, [FromBody] UpdateTaxCategoryDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateTaxCategoryAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax category: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxCategoryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("tax-categories/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteTaxCategory(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteTaxCategoryAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax category: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Tax Slabs
    [HttpGet("tax-slabs")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxSlabDto>>>> GetAllTaxSlabs()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllTaxSlabsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax slabs");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxSlabDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-slabs/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxSlabDto>>>> GetActiveTaxSlabs()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveTaxSlabsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tax slabs");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxSlabDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-slabs/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxSlabDto>>> GetTaxSlabById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetTaxSlabByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax slab by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxSlabDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("tax-slabs")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxSlabDto>>> CreateTaxSlab([FromBody] CreateTaxSlabDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateTaxSlabAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetTaxSlabById), new { id = response.Data?.SlabId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax slab");
            return StatusCode(500, new ServiceResponse<TaxSlabDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("tax-slabs/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxSlabDto>>> UpdateTaxSlab(Guid id, [FromBody] UpdateTaxSlabDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateTaxSlabAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax slab: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxSlabDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("tax-slabs/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteTaxSlab(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteTaxSlabAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax slab: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Tax Component Rules
    [HttpGet("tax-component-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxComponentRuleDto>>>> GetAllTaxComponentRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllTaxComponentRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax component rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxComponentRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-component-rules/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxComponentRuleDto>>>> GetActiveTaxComponentRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveTaxComponentRulesAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tax component rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxComponentRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-component-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxComponentRuleDto>>> GetTaxComponentRuleById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetTaxComponentRuleByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax component rule by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("tax-component-rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxComponentRuleDto>>> CreateTaxComponentRule([FromBody] CreateTaxComponentRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateTaxComponentRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetTaxComponentRuleById), new { id = response.Data?.ComponentRuleId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax component rule");
            return StatusCode(500, new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("tax-component-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxComponentRuleDto>>> UpdateTaxComponentRule(Guid id, [FromBody] UpdateTaxComponentRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateTaxComponentRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax component rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("tax-component-rules/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteTaxComponentRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteTaxComponentRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax component rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Tax Transactions
    [HttpGet("tax-transactions")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxTransactionDto>>>> GetAllTaxTransactions()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllTaxTransactionsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax transactions");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-transactions/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TaxTransactionDto>>>> GetActiveTaxTransactions()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveTaxTransactionsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tax transactions");
            return StatusCode(500, new ServiceResponse<IEnumerable<TaxTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-transactions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxTransactionDto>>> GetTaxTransactionById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetTaxTransactionByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax transaction by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("tax-transactions")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxTransactionDto>>> CreateTaxTransaction([FromBody] CreateTaxTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateTaxTransactionAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetTaxTransactionById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax transaction");
            return StatusCode(500, new ServiceResponse<TaxTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("tax-transactions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<TaxTransactionDto>>> UpdateTaxTransaction(Guid id, [FromBody] UpdateTaxTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateTaxTransactionAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<TaxTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("tax-transactions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteTaxTransaction(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteTaxTransactionAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("provident-fund/enroll")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRequestDto>>> EnrollProvidentFund([FromBody] CreateProvidentFundEnrollmentDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateProvidentFundEnrollmentAsync(organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund enrollment request");
            return StatusCode(500, new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("/api/employee/provident-fund/enroll/{requestId}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRequestDto>>> UpdateProvidentFundEnrollmentRequest(Guid requestId, [FromBody] CreateProvidentFundEnrollmentDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.UpdateProvidentFundEnrollmentRequestAsync(requestId, organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provident fund enrollment request: {RequestId}", requestId);
            return StatusCode(500, new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("provident-fund/update-percentage")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRequestDto>>> UpdateProvidentFundPercentage([FromBody] UpdateProvidentFundPercentageDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.UpdateProvidentFundPercentageAsync(organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund update percentage request");
            return StatusCode(500, new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("/api/employee/provident-fund/withdraw")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<ProvidentFundRequestDto>>> WithdrawProvidentFund([FromBody] CreateProvidentFundWithdrawRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateProvidentFundWithdrawRequestAsync(organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund withdrawal request");
            return StatusCode(500, new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("/api/employee/provident-fund/request/{requestId}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteProvidentFundRequest(Guid requestId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.DeleteProvidentFundRequestAsync(requestId, organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provident fund request: {RequestId}", requestId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("/api/employee/provident-fund/my-pending-request")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<PagedResult<ProvidentFundRequestDto>>>> GetMyPendingProvidentFundRequest([FromQuery] ProvidentFundRequestFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyProvidentFundRequestsAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provident fund requests");
            return StatusCode(500, new ServiceResponse<PagedResult<ProvidentFundRequestDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("/api/employee/provident-fund/my-active-request")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<ProvidentFundAccountDto>>> GetMyActiveProvidentFundRequest()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyActiveProvidentFundAsync(organizationId, employeeId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active provident fund request");
            return StatusCode(500, new ServiceResponse<ProvidentFundAccountDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("/api/employee/provident-fund/my-transactions")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<PagedResult<ProvidentFundTransactionDto>>>> GetMyProvidentFundTransactions([FromQuery] ProvidentFundTransactionFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyProvidentFundTransactionsAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provident fund transaction history");
            return StatusCode(500, new ServiceResponse<PagedResult<ProvidentFundTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }
    //PF Admin Side

    [HttpGet("provident-fund/requests")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<AdminProvidentFundRequestDto>>>> GetAllProvidentFundRequests([FromQuery] AdminProvidentFundRequestFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllProvidentFundRequestsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all provident fund requests");
            return StatusCode(500, new ServiceResponse<PagedResult<AdminProvidentFundRequestDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("provident-fund/accounts")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<AdminProvidentFundAccountDto>>>> GetAllProvidentFundAccounts([FromQuery] AdminProvidentFundAccountFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllProvidentFundAccountsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all provident fund accounts");
            return StatusCode(500, new ServiceResponse<PagedResult<AdminProvidentFundAccountDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("provident-fund/repayments")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<AdminProvidentFundRepaymentDto>>>> GetAllProvidentFundRepayments([FromQuery] AdminProvidentFundRepaymentFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllProvidentFundRepaymentsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all provident fund repayments");
            return StatusCode(500, new ServiceResponse<PagedResult<AdminProvidentFundRepaymentDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("provident-fund/requests/approve")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveProvidentFundRequest([FromBody] ApproveProvidentFundRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var approvedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.ApproveProvidentFundRequestAsync(organizationId, approvedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving provident fund request");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("/api/admin/provident-fund/requests/{requestId}/reject")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectProvidentFundRequest(Guid requestId, [FromBody] RejectProvidentFundRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.RejectProvidentFundRequestAsync(requestId, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting provident fund request: {RequestId}", requestId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("/api/admin/provident-fund/monthly-transaction")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> AddProvidentFundMonthlyTransaction([FromBody] CreateProvidentFundMonthlyTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.AddProvidentFundMonthlyTransactionAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding provident fund monthly transaction");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("/api/admin/provident-fund/manual-enroll")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ProvidentFundEnrollmentDto>>> ManualProvidentFundEnrollment([FromBody] ManualProvidentFundEnrollmentDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.ManualProvidentFundEnrollmentAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual provident fund enrollment");
            return StatusCode(500, new ServiceResponse<ProvidentFundEnrollmentDto> { Success = false, Message = "An error occurred" });
        }
    }


    [HttpPost("salary-advance/request")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<SalaryAdvanceDto>>> CreateSalaryAdvance([FromBody] CreateSalaryAdvanceDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateSalaryAdvanceAsync(organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary advance request");
            return StatusCode(500, new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("salary-advance/my-requests/{id}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<SalaryAdvanceDto>>> UpdateMySalaryAdvance(Guid id, [FromBody] CreateSalaryAdvanceDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.UpdateMySalaryAdvanceAsync(id, organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary advance request: {Id}", id);
            return StatusCode(500, new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("salary-advance/my-requests/{id}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> CancelMySalaryAdvance(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CancelMySalaryAdvanceAsync(id, organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling salary advance request: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance/my-requests")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<PagedResult<MySalaryAdvanceDto>>>> GetMySalaryAdvances([FromQuery] SalaryAdvanceFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySalaryAdvancesAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my salary advance requests");
            return StatusCode(500, new ServiceResponse<PagedResult<MySalaryAdvanceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance/my-requests/{id}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<MySalaryAdvanceDto>>> GetMySalaryAdvanceById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySalaryAdvanceByIdAsync(id, organizationId, employeeId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my salary advance request by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<MySalaryAdvanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance/all")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<AdminSalaryAdvanceDto>>>> GetAllSalaryAdvanceRequests([FromQuery] AdminSalaryAdvanceFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllSalaryAdvanceRequestsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all salary advance requests");
            return StatusCode(500, new ServiceResponse<PagedResult<AdminSalaryAdvanceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance/all/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<AdminSalaryAdvanceDto>>> GetSalaryAdvanceRequestById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSalaryAdvanceRequestByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary advance request by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<AdminSalaryAdvanceDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("salary-advance/{id}/approve")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveSalaryAdvance(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var approvedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.ApproveSalaryAdvanceAsync(id, organizationId, approvedBy);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving salary advance request: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("salary-advance/{id}/reject")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectSalaryAdvance(Guid id, [FromBody] RejectSalaryAdvanceDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var rejectedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.RejectSalaryAdvanceAsync(id, organizationId, rejectedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting salary advance request: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("salary-advance/{id}/disburse")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DisburseSalaryAdvance(Guid id, [FromBody] DisburseSalaryAdvanceDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var disbursedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.DisburseSalaryAdvanceAsync(id, organizationId, disbursedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disbursing salary advance request: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // Loans
    //Employee side
    [HttpPost("loans/request")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<LoanDto>>> CreateLoanRequest([FromBody] CreateLoanRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateLoanRequestAsync(organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetLoanById), new { id = response.Data?.LoanId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan request");
            return StatusCode(500, new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-requests/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<LoanDto>>> GetMyLoanRequestById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetLoanByIdAndEmployeeAsync(id, organizationId, employeeId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving personal loan request by ID");
            return StatusCode(500, new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("loans/my-requests/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<LoanDto>>> UpdateMyLoanRequest(Guid id, [FromBody] CreateLoanRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.UpdateLoanRequestAsync(id, organizationId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personal loan request");
            return StatusCode(500, new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("loans/my-requests/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteMyLoanRequest(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.DeleteLoanRequestAsync(id, organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting personal loan request");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }



    [HttpGet("loans/my-active-loans")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<MyActiveLoanDto>>>> GetMyActiveLoans()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyActiveLoansAsync(organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving personal active loans");
            return StatusCode(500, new ServiceResponse<IEnumerable<MyActiveLoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-pending-loans")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<MyActiveLoanDto>>>> GetMyPendingLoans()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyPendingLoansAsync(organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving personal pending loans");
            return StatusCode(500, new ServiceResponse<IEnumerable<MyActiveLoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-loan-history")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<PagedResult<MyLoanPaymentDto>>>> GetMyLoanHistory([FromQuery] MyLoanHistoryFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetLoanHistoryAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving personal loan history");
            return StatusCode(500, new ServiceResponse<PagedResult<MyLoanPaymentDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-references")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<LoanReferenceDto>>>> GetMyLoanReferences()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyLoanReferencesAsync(organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan references");
            return StatusCode(500, new ServiceResponse<IEnumerable<LoanReferenceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-requests")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<PagedResult<LoanDto>>>> GetMyLoans([FromQuery] LoanFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyLoansAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving personal loan requests");
            return StatusCode(500, new ServiceResponse<PagedResult<LoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overview")]
    [Authorize(Roles = "Super Admin,Employee,Manager,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PayrollOverviewDto>>> GetPayrollOverview()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetPayrollOverviewAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll overview");
            return StatusCode(500, new ServiceResponse<PayrollOverviewDto> { Success = false, Message = "An error occurred" });
        }
    }

    // Admin side

    [HttpGet("loans/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanDto>>> GetLoanById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLoanByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("loans/disburse")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanDto>>> DisburseLoan([FromBody] DisburseLoanDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var createdBy = await GetCurrentEmployeeId();
            var response = await _payRollService.DisburseLoanAsync(organizationId, createdBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disbursing loan");
            return StatusCode(500, new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred" });
        }
    }
 
    [HttpPatch("loans/status")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanDto>>> UpdateLoanStatus([FromBody] UpdateLoanStatusDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var approvedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.UpdateLoanStatusAsync(organizationId, approvedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan status");
            return StatusCode(500, new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("loans/payment")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> AddLoanPayment([FromBody] AddLoanPaymentDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.AddLoanPaymentAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding loan payment");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }
    [HttpPut("loans/payment")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateLoanPayment([FromBody] UpdateLoanPaymentDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateLoanPaymentAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan payment record");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/payment/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<LoanPaymentDto>>> GetLoanPaymentById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetLoanPaymentByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loan payment record by ID");
            return StatusCode(500, new ServiceResponse<LoanPaymentDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/all")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<LoanDto>>>> GetAllLoans([FromQuery] LoanFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllLoansAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all loan requests");
            return StatusCode(500, new ServiceResponse<PagedResult<LoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/disbursed-loans")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<MyActiveLoanDto>>>> GetDisbursedLoans([FromQuery] DisbursedLoanFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetDisbursedLoansAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disbursed loan requests");
            return StatusCode(500, new ServiceResponse<PagedResult<MyActiveLoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/disbursed-active-loans")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<MyActiveLoanDto>>>> GetDisbursedActiveLoans([FromQuery] DisbursedLoanFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetDisbursedActiveLoansAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disbursed active loan requests");
            return StatusCode(500, new ServiceResponse<PagedResult<MyActiveLoanDto>> { Success = false, Message = "An error occurred" });
        }
    }
    [HttpGet("repayments/all")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<RepaymentHistoryDto>>>> GetAllRepayments([FromQuery] RepaymentFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllRepaymentsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repayment history");
            return StatusCode(500, new ServiceResponse<PagedResult<RepaymentHistoryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    // ─── Gratuity Config ─────────────────────────────────────────────────────

    [HttpGet("gratuity-configs")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<GratuityConfigDto>>>> GetAllGratuityConfigs()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllGratuityConfigsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gratuity configs");
            return StatusCode(500, new ServiceResponse<IEnumerable<GratuityConfigDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("gratuity-configs/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Standard User")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<GratuityConfigDto>>>> GetActiveGratuityConfigs()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveGratuityConfigsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active gratuity configs");
            return StatusCode(500, new ServiceResponse<IEnumerable<GratuityConfigDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("gratuity-configs/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<GratuityConfigDto>>> GetGratuityConfigById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetGratuityConfigByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gratuity config by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<GratuityConfigDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("gratuity-configs")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<GratuityConfigDto>>> CreateGratuityConfig([FromBody] CreateGratuityConfigDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateGratuityConfigAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetGratuityConfigById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gratuity config");
            return StatusCode(500, new ServiceResponse<GratuityConfigDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("gratuity-configs/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<GratuityConfigDto>>> UpdateGratuityConfig(Guid id, [FromBody] UpdateGratuityConfigDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateGratuityConfigAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gratuity config: {Id}", id);
            return StatusCode(500, new ServiceResponse<GratuityConfigDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("gratuity-configs/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteGratuityConfig(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteGratuityConfigAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gratuity config: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // ─── Gratuity Transactions ────────────────────────────────────────────────

    [HttpGet("gratuity-transactions")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<GratuityTransactionDto>>>> GetAllGratuityTransactions([FromQuery] GratuityTransactionFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllGratuityTransactionsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gratuity transactions");
            return StatusCode(500, new ServiceResponse<PagedResult<GratuityTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("gratuity-transactions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<GratuityTransactionDto>>> GetGratuityTransactionById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetGratuityTransactionByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gratuity transaction by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("gratuity-transactions")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<GratuityTransactionDto>>> CreateGratuityTransaction([FromBody] CreateGratuityTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var createdBy = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateGratuityTransactionAsync(organizationId, createdBy, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetGratuityTransactionById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gratuity transaction");
            return StatusCode(500, new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("gratuity-transactions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<GratuityTransactionDto>>> UpdateGratuityTransaction(Guid id, [FromBody] UpdateGratuityTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateGratuityTransactionAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gratuity transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("gratuity-transactions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteGratuityTransaction(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteGratuityTransactionAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gratuity transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("gratuity-transactions/{id}/approve")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveGratuityTransaction(Guid id, [FromBody] ApproveGratuityDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var approvedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.ApproveGratuityTransactionAsync(id, organizationId, approvedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving gratuity transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("gratuity-transactions/{id}/mark-paid")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> MarkGratuityPaid(Guid id, [FromBody] MarkGratuityPaidDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var paidBy = await GetCurrentEmployeeId();
            var response = await _payRollService.MarkGratuityPaidAsync(id, organizationId, paidBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking gratuity as paid: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("gratuity-transactions/my-status")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<GratuityTransactionDto>>> GetMyGratuityStatus()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyGratuityStatusAsync(employeeId, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching gratuity status for current employee");
            return StatusCode(500, new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred" });
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
