using Hangfire;
using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
[Authorize]
public class PayRollController : ControllerBase
{
    private readonly IPayRollService _payRollService;
    private readonly ILogger<PayRollController> _logger;
    private readonly IAttendanceService _attendanceService;

    public PayRollController(IPayRollService payRollService, ILogger<PayRollController> logger, IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
        _payRollService = payRollService;
        _logger = logger;
    }

    [HttpGet("rules")]
    [PermissionAuthorize("payroll_rules_view")]
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
    [PermissionAuthorize("overtime_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<OvertimeRuleDto>>>> GetAllOvertimeRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<OvertimeRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overtime-rules/active")]
    [PermissionAuthorize("overtime_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<OvertimeRuleDto>>>> GetActiveOvertimeRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<OvertimeRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("overtime-rules/{id}")]
    [PermissionAuthorize("overtime_rule_view")]
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
    [PermissionAuthorize("overtime_rule_add")]
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
    [PermissionAuthorize("overtime_rule_edit")]
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
    [PermissionAuthorize("overtime_rule_delete")]
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
    [PermissionAuthorize("overtime_rule_edit")]
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
    [PermissionAuthorize("overtime_entry_view")]
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
    [PermissionAuthorize("overtime_entry_view")]
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
    [PermissionAuthorize("overtime_entry_add")]
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
    [PermissionAuthorize("overtime_entry_edit")]
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
    [PermissionAuthorize("overtime_entry_delete")]
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
    [PermissionAuthorize("payroll_period_view")]
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
    [PermissionAuthorize("payroll_period_view_details")]
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
    [PermissionAuthorize("payroll_period_add")]
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
    [PermissionAuthorize("payroll_period_edit")]
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
    [PermissionAuthorize("payroll_period_delete")]
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
    [PermissionAuthorize("attendance_deduction_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<AttendanceDeductionRuleDto>>>> GetAllAttendanceDeductionRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<AttendanceDeductionRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("attendance-deduction-rules/active")]
    [PermissionAuthorize("attendance_deduction_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<AttendanceDeductionRuleDto>>>> GetActiveAttendanceDeductionRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<AttendanceDeductionRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("attendance-deduction-rules/{id}")]
    [PermissionAuthorize("attendance_deduction_rule_view")]
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
    [PermissionAuthorize("attendance_deduction_rule_add")]
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
    [PermissionAuthorize("attendance_deduction_rule_edit")]
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
    [PermissionAuthorize("attendance_deduction_rule_delete")]
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
    [PermissionAuthorize("late_arrival_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LateArrivalRuleDto>>>> GetAllLateArrivalRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LateArrivalRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("late-arrival-rules/active")]
    [PermissionAuthorize("late_arrival_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LateArrivalRuleDto>>>> GetActiveLateArrivalRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LateArrivalRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("late-arrival-rules/{id}")]
    [PermissionAuthorize("late_arrival_rule_view")]
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
    [PermissionAuthorize("late_arrival_rule_add")]
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
    [PermissionAuthorize("late_arrival_rule_edit")]
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
    [PermissionAuthorize("late_arrival_rule_delete")]
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
    [PermissionAuthorize("leave_deduction_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LeaveRuleDto>>>> GetAllLeaveRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LeaveRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("leave-rules/active")]
    [PermissionAuthorize("leave_deduction_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LeaveRuleDto>>>> GetActiveLeaveRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LeaveRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("leave-rules/{id}")]
    [PermissionAuthorize("leave_deduction_rule_view")]
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
    [PermissionAuthorize("leave_deduction_rule_add")]
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
    [PermissionAuthorize("leave_deduction_rule_edit")]
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
    [PermissionAuthorize("leave_deduction_rule_delete")]
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
    [PermissionAuthorize("late_attendance_view")]
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
    [PermissionAuthorize("late_attendance_view")]
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
    [PermissionAuthorize("late_attendance_add")]
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
    [PermissionAuthorize("late_attendance_edit")]
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
    [PermissionAuthorize("late_attendance_delete")]
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
    [PermissionAuthorize("attendance_summary_view")]
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
    [PermissionAuthorize("attendance_summary_view")]
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
    [PermissionAuthorize("attendance_summary_add")]
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
    [PermissionAuthorize("attendance_summary_edit")]
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
    [PermissionAuthorize("attendance_summary_delete")]
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
    [PermissionAuthorize("leave_summary_view")]
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
    [PermissionAuthorize("leave_summary_view")]
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
    [PermissionAuthorize("leave_summary_add")]
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
    [PermissionAuthorize("leave_summary_edit")]
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
    [PermissionAuthorize("leave_summary_delete")]
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
    [PermissionAuthorize("performance_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<PerformanceRuleDto>>>> GetAllPerformanceRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<PerformanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("performance-rules/active")]
    [PermissionAuthorize("performance_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<PerformanceRuleDto>>>> GetActivePerformanceRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<PerformanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("performance-rules/{id}")]
    [PermissionAuthorize("performance_rule_view")]
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
    [PermissionAuthorize("performance_rule_add")]
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
    [PermissionAuthorize("performance_rule_edit")]
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
    [PermissionAuthorize("performance_rule_delete")]
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
    [PermissionAuthorize("bonus_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<BonusRuleDto>>>> GetAllBonusRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<BonusRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("bonus-rules/active")]
    [PermissionAuthorize("bonus_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<BonusRuleDto>>>> GetActiveBonusRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<BonusRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("bonus-rules/{id}")]
    [PermissionAuthorize("bonus_rule_view")]
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
    [PermissionAuthorize("bonus_rule_add")]
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
    [PermissionAuthorize("bonus_rule_edit")]
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
    [PermissionAuthorize("bonus_rule_delete")]
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
    [PermissionAuthorize("performance_pay_view")]
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
    [PermissionAuthorize("performance_pay_view")]
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
    [PermissionAuthorize("performance_pay_add")]
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
    [PermissionAuthorize("performance_pay_edit")]
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
    [PermissionAuthorize("performance_pay_delete")]
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
    [PermissionAuthorize("bonus_entry_view")]
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
    [PermissionAuthorize("bonus_entry_view")]
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
    [PermissionAuthorize("bonus_entry_add")]
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
    [PermissionAuthorize("bonus_entry_edit")]
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
    [PermissionAuthorize("bonus_entry_delete")]
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
    [PermissionAuthorize("loan_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LoanRuleDto>>>> GetAllLoanRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LoanRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loan-rules/active")]
    [PermissionAuthorize("loan_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LoanRuleDto>>>> GetActiveLoanRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LoanRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loan-rules/{id}")]
    [PermissionAuthorize("loan_rule_view")]
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
    [PermissionAuthorize("loan_rule_add")]
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
    [PermissionAuthorize("loan_rule_edit")]
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
    [PermissionAuthorize("loan_rule_delete")]
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
    [PermissionAuthorize("salary_advance_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SalaryAdvanceRuleDto>>>> GetAllSalaryAdvanceRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SalaryAdvanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance-rules/active")]
    [PermissionAuthorize("salary_advance_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SalaryAdvanceRuleDto>>>> GetActiveSalaryAdvanceRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SalaryAdvanceRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance-rules/{id}")]
    [PermissionAuthorize("salary_advance_rule_view")]
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
    [PermissionAuthorize("salary_advance_rule_add")]
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
    [PermissionAuthorize("salary_advance_rule_edit")]
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
    [PermissionAuthorize("salary_advance_rule_delete")]
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
    [PermissionAuthorize("provident_fund_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<ProvidentFundRuleDto>>>> GetAllProvidentFundRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<ProvidentFundRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("provident-fund-rules/active")]
    [PermissionAuthorize("provident_fund_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<ProvidentFundRuleDto>>>> GetActiveProvidentFundRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<ProvidentFundRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("provident-fund-rules/{id}")]
    [PermissionAuthorize("provident_fund_rule_view")]
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
    [PermissionAuthorize("provident_fund_rule_add")]
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
    [PermissionAuthorize("provident_fund_rule_edit")]
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
    [PermissionAuthorize("provident_fund_rule_delete")]
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
    [PermissionAuthorize("tax_regime_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxRegimeDto>>>> GetAllTaxRegimes()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxRegimeDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-regimes/active")]
    [PermissionAuthorize("tax_regime_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxRegimeDto>>>> GetActiveTaxRegimes()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxRegimeDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-regimes/{id}")]
    [PermissionAuthorize("tax_regime_view")]
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
    [PermissionAuthorize("tax_regime_add")]
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
    [PermissionAuthorize("tax_regime_edit")]
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
    [PermissionAuthorize("tax_regime_delete")]
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
    [PermissionAuthorize("tax_category_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxCategoryDto>>>> GetAllTaxCategories()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxCategoryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-categories/active")]
    [PermissionAuthorize("tax_category_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxCategoryDto>>>> GetActiveTaxCategories()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxCategoryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-categories/{id}")]
    [PermissionAuthorize("tax_category_view")]
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

    [HttpGet("tax-categories/regime/{regimeId}")]
    [PermissionAuthorize("tax_category_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxCategoryDto>>>> GetTaxCategoriesByRegimeId(Guid regimeId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetTaxCategoriesByRegimeIdAsync(regimeId, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax categories by regime ID: {RegimeId}", regimeId);
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxCategoryDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("tax-categories")]
    [PermissionAuthorize("tax_category_add")]
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
    [PermissionAuthorize("tax_category_edit")]
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
    [PermissionAuthorize("tax_category_delete")]
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
    [PermissionAuthorize("tax_slab_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxSlabDto>>>> GetAllTaxSlabs()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxSlabDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-slabs/active")]
    [PermissionAuthorize("tax_slab_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxSlabDto>>>> GetActiveTaxSlabs()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxSlabDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-slabs/{id}")]
    [PermissionAuthorize("tax_slab_view")]
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
    [PermissionAuthorize("tax_slab_add")]
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
    [PermissionAuthorize("tax_slab_edit")]
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
    [PermissionAuthorize("tax_slab_delete")]
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
    [PermissionAuthorize("tax_component_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxComponentRuleDto>>>> GetAllTaxComponentRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxComponentRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-component-rules/active")]
    [PermissionAuthorize("tax_component_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxComponentRuleDto>>>> GetActiveTaxComponentRules()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxComponentRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-component-rules/{id}")]
    [PermissionAuthorize("tax_component_rule_view")]
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
    [PermissionAuthorize("tax_component_rule_add")]
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
    [PermissionAuthorize("tax_component_rule_edit")]
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
    [PermissionAuthorize("tax_component_rule_delete")]
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
    [PermissionAuthorize("get_transaction_ledger")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxTransactionDto>>>> GetAllTaxTransactions()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-transactions/active")]
    [PermissionAuthorize("get_transaction_ledger")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<TaxTransactionDto>>>> GetActiveTaxTransactions()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<TaxTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("tax-transactions/{id}")]
    [PermissionAuthorize("get_transaction_ledger")]
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
    [PermissionAuthorize("add_transaction_ledger")]
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
    [PermissionAuthorize("edit_transaction_ledger")]
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
    [PermissionAuthorize("delete_transaction_ledger")]
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
    [PermissionAuthorize("pf_employee_enroll")]
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
    [PermissionAuthorize("pf_employee_edit_enroll")]
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
    [PermissionAuthorize("pf_employee_update_percentage")]
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
    [PermissionAuthorize("pf_employee_withdraw")]
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
    [PermissionAuthorize("pf_employee_delete_request")]
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
    [PermissionAuthorize("pf_employee_pending_requests")]
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
    [PermissionAuthorize("pf_employee_active")]
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
    [PermissionAuthorize("pf_employee_transactions")]
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
    [PermissionAuthorize("pf_admin_view")]
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
    [PermissionAuthorize("pf_admin_view")]
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
    [PermissionAuthorize("pf_admin_view")]
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
    [PermissionAuthorize("pf_admin_edit")]
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
    [PermissionAuthorize("pf_admin_delete")]
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
    [PermissionAuthorize("pf_admin_add")]
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
    [PermissionAuthorize("pf_admin_add")]
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
    [PermissionAuthorize("salary_advance_employee_request")]
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
    [PermissionAuthorize("salary_advance_employee_edit")]
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
    [PermissionAuthorize("salary_advance_employee_delete")]
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
    [PermissionAuthorize("salary_advance_employee_list")]
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

    [HttpGet("salary-advance/summary")]
    [PermissionAuthorize("salary_advance_employee_summary")]
    public async Task<ActionResult<ServiceResponse<SalaryAdvanceSummaryDto>>> GetMySalaryAdvanceSummary()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySalaryAdvanceSummaryAsync(organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary advance summary");
            return StatusCode(500, new ServiceResponse<SalaryAdvanceSummaryDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("salary-advance/my-requests/{id}")]
    [PermissionAuthorize("salary_advance_employee_view")]
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
    [PermissionAuthorize("salary_advance_admin_list")]
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
    [PermissionAuthorize("salary_advance_admin_view")]
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
    [PermissionAuthorize("salary_advance_admin_approve")]
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
    [PermissionAuthorize("salary_advance_admin_reject")]
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
    [PermissionAuthorize("salary_advance_admin_disburse")]
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
    [PermissionAuthorize("loan_employee_request")]
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
    [PermissionAuthorize("loan_employee_view")]
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
    [PermissionAuthorize("loan_employee_edit")]
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
    [PermissionAuthorize("loan_employee_delete")]
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
    [PermissionAuthorize("loan_employee_active")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<MyActiveLoanDto>>>> GetMyActiveLoans()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<MyActiveLoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-pending-loans")]
    [PermissionAuthorize("loan_employee_pending")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<MyActiveLoanDto>>>> GetMyPendingLoans()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<MyActiveLoanDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-loan-history")]
    [PermissionAuthorize("loan_employee_history")]
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
    [PermissionAuthorize("loan_employee_references")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<LoanReferenceDto>>>> GetMyLoanReferences()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<LoanReferenceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("loans/my-requests")]
    [PermissionAuthorize("loan_employee_list")]
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
    [PermissionAuthorize("my_benefits")]
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
    [PermissionAuthorize("loan_admin_view")]
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
    [PermissionAuthorize("loan_admin_edit")]
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
    [PermissionAuthorize("loan_admin_edit")]
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
    [PermissionAuthorize("loan_admin_add")]
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

    [HttpPost("calculate-payroll")]
    [PermissionAuthorize("payroll_calculation")]
    public async Task<ActionResult<ServiceResponse<PayrollCalculationResultDto>>> CalculatePayroll([FromBody] CalculatePayrollDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CalculatePayrollAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payroll");
            return StatusCode(500, new ServiceResponse<PayrollCalculationResultDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("payroll-results")]
    [PermissionAuthorize("payroll_result")]
    public async Task<ActionResult<ServiceResponse<PagedResult<PayrollResultItemDto>>>> GetPayrollResults([FromQuery] PayrollResultsFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetPayrollResultsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payroll results");
            return StatusCode(500, new ServiceResponse<PagedResult<PayrollResultItemDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("loans/payment")]
    [PermissionAuthorize("loan_admin_edit")]
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
    [PermissionAuthorize("loan_admin_view")]
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
    [PermissionAuthorize("loan_admin_view")]
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
    [PermissionAuthorize("loan_admin_view")]
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
    [PermissionAuthorize("loan_admin_view")]
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
    [PermissionAuthorize("loan_admin_view")]
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

    // ─── Social Security Config ──────────────────────────────────────────────

    [HttpGet("social-security/jurisdictions")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SocialSecurityJurisdictionDto>>>> GetSocialSecurityJurisdictions()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityJurisdictionsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security jurisdictions");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SocialSecurityJurisdictionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/jurisdictions")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityJurisdictionDto>>> CreateSocialSecurityJurisdiction([FromBody] CreateSocialSecurityJurisdictionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSocialSecurityJurisdictionAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security jurisdiction");
            return StatusCode(500, new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("social-security/jurisdictions/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityJurisdictionDto>>> UpdateSocialSecurityJurisdiction(Guid id, [FromBody] UpdateSocialSecurityJurisdictionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSocialSecurityJurisdictionAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security jurisdiction: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security/jurisdictions/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityJurisdiction(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSocialSecurityJurisdictionAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security jurisdiction: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/authorities")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SocialSecurityAuthorityDto>>>> GetSocialSecurityAuthorities([FromQuery] Guid? jurisdictionId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityAuthoritiesAsync(organizationId, jurisdictionId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security authorities");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SocialSecurityAuthorityDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/authorities")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityAuthorityDto>>> CreateSocialSecurityAuthority([FromBody] CreateSocialSecurityAuthorityDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSocialSecurityAuthorityAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security authority");
            return StatusCode(500, new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("social-security/authorities/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityAuthorityDto>>> UpdateSocialSecurityAuthority(Guid id, [FromBody] UpdateSocialSecurityAuthorityDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSocialSecurityAuthorityAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security authority: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security/authorities/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityAuthority(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSocialSecurityAuthorityAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security authority: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/schemes")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SocialSecuritySchemeDto>>>> GetSocialSecuritySchemes([FromQuery] Guid? jurisdictionId, [FromQuery] Guid? authorityId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecuritySchemesAsync(organizationId, jurisdictionId, authorityId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security schemes");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SocialSecuritySchemeDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/schemes")]
    public async Task<ActionResult<ServiceResponse<SocialSecuritySchemeDto>>> CreateSocialSecurityScheme([FromBody] CreateSocialSecuritySchemeDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSocialSecuritySchemeAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security scheme");
            return StatusCode(500, new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("social-security/schemes/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecuritySchemeDto>>> UpdateSocialSecurityScheme(Guid id, [FromBody] UpdateSocialSecuritySchemeDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSocialSecuritySchemeAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security scheme: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security/schemes/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityScheme(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSocialSecuritySchemeAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security scheme: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/rules")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SocialSecurityRuleDto>>>> GetSocialSecurityRules([FromQuery] Guid? schemeId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityRulesAsync(organizationId, schemeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security rules");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SocialSecurityRuleDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/rules")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityRuleDto>>> CreateSocialSecurityRule([FromBody] CreateSocialSecurityRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSocialSecurityRuleAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security rule");
            return StatusCode(500, new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("social-security/rules/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityRuleDto>>> UpdateSocialSecurityRule(Guid id, [FromBody] UpdateSocialSecurityRuleDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSocialSecurityRuleAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security/rules/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityRule(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSocialSecurityRuleAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security rule: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security-configs")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityConfigDto>>>> GetAllSocialSecurityConfigs([FromQuery] SocialSecurityConfigFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllSocialSecurityConfigsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security configs");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityConfigDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security-configs/active")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SocialSecurityConfigDto>>>> GetActiveSocialSecurityConfigs()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetActiveSocialSecurityConfigsAsync(organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active social security configs");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SocialSecurityConfigDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security-configs/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityConfigDto>>> GetSocialSecurityConfigById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityConfigByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security config by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security-configs")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityConfigDto>>> CreateSocialSecurityConfig([FromBody] CreateSocialSecurityConfigDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSocialSecurityConfigAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetSocialSecurityConfigById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security config");
            return StatusCode(500, new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("social-security-configs/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityConfigDto>>> UpdateSocialSecurityConfig(Guid id, [FromBody] UpdateSocialSecurityConfigDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSocialSecurityConfigAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security config: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security-configs/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityConfig(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSocialSecurityConfigAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security config: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // ─── Social Security Transactions ────────────────────────────────────────

    [HttpGet("social-security-transactions")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityTransactionDto>>>> GetAllSocialSecurityTransactions([FromQuery] SocialSecurityTransactionFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllSocialSecurityTransactionsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security transactions");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security-transactions/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityTransactionDto>>> GetSocialSecurityTransactionById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityTransactionByIdAsync(id, organizationId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social security transaction by ID: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security-transactions")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityTransactionDto>>> CreateSocialSecurityTransaction([FromBody] CreateSocialSecurityTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.CreateSocialSecurityTransactionAsync(organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetSocialSecurityTransactionById), new { id = response.Data?.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security transaction");
            return StatusCode(500, new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("social-security-transactions/{id}")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityTransactionDto>>> UpdateSocialSecurityTransaction(Guid id, [FromBody] UpdateSocialSecurityTransactionDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.UpdateSocialSecurityTransactionAsync(id, organizationId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security-transactions/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityTransaction(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.DeleteSocialSecurityTransactionAsync(id, organizationId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security transaction: {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security-transactions/my")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityTransactionDto>>>> GetMySocialSecurityTransactions([FromQuery] MySocialSecurityTransactionFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySocialSecurityTransactionsAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my social security transactions");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityTransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    // ─── Gratuity Config ─────────────────────────────────────────────────────

    [HttpGet("gratuity-configs")]
    [PermissionAuthorize("gratuity_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<GratuityConfigDto>>>> GetAllGratuityConfigs()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<GratuityConfigDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("gratuity-configs/active")]
    [PermissionAuthorize("gratuity_rule_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<GratuityConfigDto>>>> GetActiveGratuityConfigs()
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
            return StatusCode(500, new ServiceResponse<IReadOnlyList<GratuityConfigDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("gratuity-configs/{id}")]
    [PermissionAuthorize("gratuity_rule_view")]
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
    [PermissionAuthorize("gratuity_rule_add")]
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
    [PermissionAuthorize("gratuity_rule_edit")]
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
    [PermissionAuthorize("gratuity_rule_delete")]
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
    [PermissionAuthorize("gratuity_admin_view")]
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
    [PermissionAuthorize("gratuity_admin_view")]
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
    [PermissionAuthorize("gratuity_admin_add")]
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
    [PermissionAuthorize("gratuity_admin_edit")]
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
    [PermissionAuthorize("gratuity_admin_delete")]
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
    [PermissionAuthorize("gratuity_admin_edit")]
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
    [PermissionAuthorize("gratuity_admin_edit")]
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
    [PermissionAuthorize("gratuity_employee")]
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

    // ── Compliance payslips (payroll.payrollresults + PDF) ───────────────────────

    [HttpGet("my-payslips")]
    [PermissionAuthorize("my_payslip")]
    public async Task<ActionResult<ServiceResponse<PagedResult<MyPayslipDto>>>> GetMyPayslips([FromQuery] MyPayslipFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMyPayslipsAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my payslips");
            return StatusCode(500, new ServiceResponse<PagedResult<MyPayslipDto>> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpGet("compliance-payslips")]
    [PermissionAuthorize("compliance_payslip_view")]
    public async Task<ActionResult<ServiceResponse<PagedResult<CompliancePayslipRowDto>>>> GetCompliancePayslips([FromQuery] CompliancePayslipFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetCompliancePayslipsAsync(organizationId, filter);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while listing compliance payslips");
            return StatusCode(403, new ServiceResponse<PagedResult<CompliancePayslipRowDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing compliance payslips");
            return StatusCode(500, new ServiceResponse<PagedResult<CompliancePayslipRowDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("compliance-payslips/stats")]
    [PermissionAuthorize("compliance_payslip_view")]
    public async Task<ActionResult<ServiceResponse<CompliancePayslipStatsDto>>> GetCompliancePayslipStats([FromQuery] CompliancePayslipFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetCompliancePayslipStatsAsync(organizationId, filter);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while loading compliance payslip stats");
            return StatusCode(403, new ServiceResponse<CompliancePayslipStatsDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading compliance payslip stats");
            return StatusCode(500, new ServiceResponse<CompliancePayslipStatsDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("compliance-payslips/{resultId:guid}/source")]
    [PermissionAuthorize("compliance_payslip_view")]
    public async Task<ActionResult<ServiceResponse<PayrollResultItemDto>>> GetCompliancePayslipSource(Guid resultId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetPayrollPayslipSourceAsync(organizationId, resultId);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            if (response.Data == null)
            {
                return NotFound(new ServiceResponse<PayrollResultItemDto?> { Success = false, Message = "Payroll result not found" });
            }

            return Ok(new ServiceResponse<PayrollResultItemDto> { Success = true, Data = response.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading compliance payslip source {ResultId}", resultId);
            return StatusCode(500, new ServiceResponse<PayrollResultItemDto?> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("compliance-payslips/{resultId:guid}/pdf")]
    [PermissionAuthorize("compliance_payslip_view")]
    public async Task<IActionResult> GetCompliancePayslipPdf(Guid resultId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var pdf = await _payRollService.GetCompliancePayslipPdfBytesAsync(organizationId, resultId);
            if (pdf == null || pdf.Length == 0)
            {
                return NotFound();
            }

            var meta = await _payRollService.GetPayrollPayslipSourceAsync(organizationId, resultId);
            var row = meta.Data;
            var periodLabel = row == null || string.IsNullOrWhiteSpace(row.PeriodName) ? resultId.ToString("D") : row.PeriodName;
            var safeCode = row == null || string.IsNullOrWhiteSpace(row.EmployeeCode)
                ? "employee"
                : row.EmployeeCode.Replace(Path.DirectorySeparatorChar, '-').Replace(Path.AltDirectorySeparatorChar, '-');
            var fileName = $"payslip-{safeCode}-{periodLabel}".Replace(' ', '_');
            return File(pdf, "application/pdf", $"{fileName}.pdf");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building payslip PDF {ResultId}", resultId);
            return StatusCode(500);
        }
    }

    [HttpPost("/api/payroll/upload-send-payslips")]
    [PermissionAuthorize("mail_upload_payslip")]
    public ActionResult<ServiceResponse<UploadSendPayslipsResponseDto>> UploadSendPayslips([FromBody] UploadSendPayslipsRequestDto dto)
    {
        try
        {
            if (dto.PayrollPeriodId == Guid.Empty || dto.EmployeeIds == null || dto.EmployeeIds.Count == 0)
            {
                return BadRequest(new ServiceResponse<UploadSendPayslipsResponseDto>
                {
                    Success = false,
                    Message = "Payroll period and at least one employee are required."
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var sentByUserId = GetCurrentUserId();
            var jobId = Guid.NewGuid().ToString("D");
            var employeeIds = dto.EmployeeIds.Where(id => id != Guid.Empty).Distinct().ToList();

            var emailSubject = string.IsNullOrWhiteSpace(dto.Subject) ? null : dto.Subject.Trim();
            var emailMessage = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim();

            BackgroundJob.Enqueue<IBulkPayslipJobService>(service =>
                service.ProcessBulkUploadAndSendAsync(
                    jobId,
                    organizationId,
                    dto.PayrollPeriodId,
                    employeeIds,
                    sentByUserId,
                    dto.ConnectionId,
                    emailSubject,
                    emailMessage));

            return Accepted(new ServiceResponse<UploadSendPayslipsResponseDto>
            {
                Success = true,
                Message = "Bulk payslip upload and email job has been queued.",
                Data = new UploadSendPayslipsResponseDto
                {
                    JobId = jobId,
                    Message = "Connect to SignalR hub /hubs/payslip-bulk and call JoinJob with this job id for live progress."
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while queueing bulk payslip send");
            return StatusCode(403, new ServiceResponse<UploadSendPayslipsResponseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing bulk payslip upload and send");
            return StatusCode(500, new ServiceResponse<UploadSendPayslipsResponseDto>
            {
                Success = false,
                Message = "An error occurred while starting the bulk payslip job."
            });
        }
    }

    [HttpPost("compliance-payslips/bulk-generate-pdfs")]
    [PermissionAuthorize("payslip_generation")]
    public async Task<ActionResult<ServiceResponse<BulkPayslipPdfResponseDto>>> BulkGenerateCompliancePayslipPdfs([FromBody] BulkPayslipPdfRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.BulkGeneratePayslipPdfsAsync(organizationId, dto);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk-generating compliance payslip PDFs");
            return StatusCode(500, new ServiceResponse<BulkPayslipPdfResponseDto> { Success = false, Message = "An error occurred" });
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

    private Guid GetCurrentUserId()
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

        return userId;
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

    // ============================================================================
    //  SOCIAL SECURITY — Employee lifecycle endpoints
    //  (enrollments, requests, claims, documents)
    // ============================================================================

    [HttpGet("social-security/my/enrollment")]
    public async Task<ActionResult<ServiceResponse<SocialSecurityEnrollmentDto>>> GetMySocialSecurityEnrollment()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySocialSecurityEnrollmentAsync(organizationId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my social security enrollment");
            return StatusCode(500, new ServiceResponse<SocialSecurityEnrollmentDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/my/enrollment-requests")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>>>> GetMySocialSecurityRequests([FromQuery] MySocialSecurityRequestFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySocialSecurityRequestsAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my social security requests");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/my/enrollment-requests")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreateMySocialSecurityRequest([FromBody] CreateSocialSecurityEnrollmentRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateSocialSecurityEnrollmentRequestAsync(organizationId, employeeId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating my social security request");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/enrollment-requests")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>>>> GetSocialSecurityEnrollmentRequests([FromQuery] SocialSecurityEnrollmentRequestFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityEnrollmentRequestsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security enrollment requests");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/enrollments")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityEnrollmentRosterDto>>>> GetSocialSecurityEnrollments([FromQuery] SocialSecurityEnrollmentFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityEnrollmentsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security enrollments");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityEnrollmentRosterDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/enrollment-requests/{requestId:guid}/approve")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveSocialSecurityEnrollmentRequest(Guid requestId, [FromBody] ApproveSocialSecurityEnrollmentRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var approvedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.ApproveSocialSecurityEnrollmentRequestAsync(organizationId, requestId, approvedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving social security enrollment request {RequestId}", requestId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/enrollment-requests/{requestId:guid}/reject")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectSocialSecurityEnrollmentRequest(Guid requestId, [FromBody] RejectSocialSecurityEnrollmentRequestDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var rejectedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.RejectSocialSecurityEnrollmentRequestAsync(organizationId, requestId, rejectedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting social security enrollment request {RequestId}", requestId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // ── Claims ──────────────────────────────────────────────────────────────────

    [HttpGet("social-security/my/claims")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityClaimDto>>>> GetMySocialSecurityClaims([FromQuery] MySocialSecurityClaimFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.GetMySocialSecurityClaimsAsync(organizationId, employeeId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my social security claims");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityClaimDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/my/claims")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreateMySocialSecurityClaim([FromBody] CreateSocialSecurityClaimDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.CreateMySocialSecurityClaimAsync(organizationId, employeeId, employeeId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating my social security claim");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("social-security/claims")]
    public async Task<ActionResult<ServiceResponse<PagedResult<SocialSecurityClaimDto>>>> GetSocialSecurityClaims([FromQuery] SocialSecurityClaimFilterDto filter)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityClaimsAsync(organizationId, filter);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security claims");
            return StatusCode(500, new ServiceResponse<PagedResult<SocialSecurityClaimDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/claims/{claimId:guid}/approve")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveSocialSecurityClaim(Guid claimId, [FromBody] ApproveSocialSecurityClaimDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var approvedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.ApproveSocialSecurityClaimAsync(organizationId, claimId, approvedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving social security claim {ClaimId}", claimId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/claims/{claimId:guid}/reject")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectSocialSecurityClaim(Guid claimId, [FromBody] RejectSocialSecurityClaimDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var rejectedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.RejectSocialSecurityClaimAsync(organizationId, claimId, rejectedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting social security claim {ClaimId}", claimId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/claims/{claimId:guid}/mark-paid")]
    public async Task<ActionResult<ServiceResponse<bool>>> MarkSocialSecurityClaimPaid(Guid claimId, [FromBody] MarkSocialSecurityClaimPaidDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.MarkSocialSecurityClaimPaidAsync(organizationId, claimId, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking social security claim paid {ClaimId}", claimId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    // ── Documents ───────────────────────────────────────────────────────────────

    [HttpGet("social-security/requests/{requestId:guid}/documents")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SocialSecurityRequestDocumentDto>>>> GetSocialSecurityRequestDocuments(Guid requestId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetSocialSecurityRequestDocumentsAsync(organizationId, requestId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security request documents {RequestId}", requestId);
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SocialSecurityRequestDocumentDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("social-security/documents/{documentId:guid}/verify")]
    public async Task<ActionResult<ServiceResponse<bool>>> VerifySocialSecurityRequestDocument(Guid documentId, [FromBody] VerifySocialSecurityRequestDocumentDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var verifiedBy = await GetCurrentEmployeeId();
            var response = await _payRollService.VerifySocialSecurityRequestDocumentAsync(organizationId, documentId, verifiedBy, dto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying social security request document {DocumentId}", documentId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("social-security/documents/{documentId:guid}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSocialSecurityRequestDocument(Guid documentId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var response = await _payRollService.DeleteSocialSecurityRequestDocumentAsync(organizationId, documentId, employeeId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security request document {DocumentId}", documentId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }
}
