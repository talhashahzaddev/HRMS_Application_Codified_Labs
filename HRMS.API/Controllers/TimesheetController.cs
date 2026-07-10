using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimesheetController : ControllerBase
{
    private readonly ITimesheetService _service;
    private readonly ITimesheetWorkflowRepository _workflowRepo;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TimesheetController> _logger;

    public TimesheetController(ITimesheetService service, ITimesheetWorkflowRepository workflowRepo, IEmployeeRepository employeeRepository, IAuthRepository authRepository, IMemoryCache memoryCache, ILogger<TimesheetController> logger)
    {
        _service = service;
        _workflowRepo = workflowRepo;
        _employeeRepository = employeeRepository;
        _authRepository = authRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    // ═══ Config ═══════════════════════════════════════════════════════════
    [HttpGet("config")]
    [PermissionAuthorize("timesheet_config_view")]
    public async Task<ActionResult<ServiceResponse<TimesheetConfigDto>>> GetConfig()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetConfigAsync(orgId);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet config");
            return StatusCode(500, new ServiceResponse<TimesheetConfigDto> { Success = false, Message = "An error occurred while retrieving config" });
        }
    }

    [HttpPut("config")]
    [PermissionAuthorize("timesheet_config_edit")]
    public async Task<ActionResult<ServiceResponse<TimesheetConfigDto>>> UpdateConfig([FromBody] TimesheetConfigDto dto)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.UpdateConfigAsync(orgId, dto, Guid.Empty);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating timesheet config");
            return StatusCode(500, new ServiceResponse<TimesheetConfigDto> { Success = false, Message = "An error occurred while updating config" });
        }
    }

    // ═══ Periods ═══════════════════════════════════════════════════════════
    [HttpGet("periods")]
    [PermissionAuthorize("timesheet_periods_view")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetPeriodDto>>>> GetPeriods([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetPeriodsAsync(orgId, from, to);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting periods");
            return StatusCode(500, new ServiceResponse<List<TimesheetPeriodDto>> { Success = false, Message = "An error occurred while retrieving periods" });
        }
    }

    [HttpPost("periods")]
    [PermissionAuthorize("timesheet_periods_create")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreatePeriod([FromBody] CreateTimesheetPeriodDto dto)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var createdBy = await GetCurrentEmployeeIdAsync();
            var result = await _service.CreatePeriodAsync(orgId, dto, createdBy);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating period");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred while creating period" });
        }
    }

    [HttpGet("periods/{id:guid}")]
    [PermissionAuthorize("timesheet_periods_view")]
    public async Task<ActionResult<ServiceResponse<TimesheetPeriodDto>>> GetPeriod(Guid id)
    {
        try
        {
            var result = await _service.GetPeriodAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting period {Id}", id);
            return StatusCode(500, new ServiceResponse<TimesheetPeriodDto> { Success = false, Message = "An error occurred while retrieving period" });
        }
    }

    [HttpPost("periods/{id:guid}/transition")]
    [PermissionAuthorize("timesheet_periods_transition")]
    public async Task<ActionResult<ServiceResponse<bool>>> TransitionStatus(Guid id, [FromBody] TimesheetTransitionDto dto)
    {
        try
        {
            var actorId = await GetCurrentEmployeeIdAsync();
            var result = await _service.TransitionStatusAsync(id, dto.NewStatus, actorId, dto.Reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transitioning period {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while transitioning period" });
        }
    }

    [HttpGet("periods/{id:guid}/blockers")]
    [PermissionAuthorize("timesheet_periods_view")]
    public async Task<ActionResult<ServiceResponse<TimesheetLockBlockersDto>>> GetBlockers(Guid id)
    {
        try
        {
            var result = await _service.GetBlockersAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blockers for period {Id}", id);
            return StatusCode(500, new ServiceResponse<TimesheetLockBlockersDto> { Success = false, Message = "An error occurred while retrieving blockers" });
        }
    }

    // ═══ Employee Days ════════════════════════════════════════════════════
    [HttpGet("periods/{periodId:guid}/days")]
    [PermissionAuthorize("timesheet_periods_view")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeTimesheetDto>>>> GetEmployeeDays(Guid periodId, [FromQuery] Guid? employeeId)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();

            // Permission check: if user doesn't have "View All Employees" permission,
            // restrict to only their own data
            if (!employeeId.HasValue)
            {
                var hasViewAll = User.HasPermission("timesheet_view_all_employees", _authRepository, _memoryCache);
                if (!hasViewAll)
                {
                    // User doesn't have permission to view all - get their own employee ID
                    employeeId = await GetCurrentEmployeeIdAsync();
                }
            }

            var result = await _service.GetEmployeeDaysAsync(periodId, orgId, employeeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee days for period {PeriodId}", periodId);
            return StatusCode(500, new ServiceResponse<List<EmployeeTimesheetDto>> { Success = false, Message = "An error occurred while retrieving employee days" });
        }
    }

    // ═══ Corrections ═══════════════════════════════════════════════════════
    [HttpGet("corrections/pending")]
    [PermissionAuthorize("timesheet_approvals_view")]
    public async Task<ActionResult<ServiceResponse<List<PendingCorrectionDto>>>> GetPendingCorrections([FromQuery] Guid timesheetId)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetPendingCorrectionsAsync(timesheetId, orgId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending corrections for timesheet {TimesheetId}", timesheetId);
            return StatusCode(500, new ServiceResponse<List<PendingCorrectionDto>> { Success = false, Message = "An error occurred while retrieving pending corrections" });
        }
    }

    [HttpPost("corrections")]
    [PermissionAuthorize("timesheet_corrections_submit")]
    public async Task<ActionResult<ServiceResponse<Guid>>> SubmitCorrection([FromBody] TimesheetCorrectionDto dto)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            var orgId = GetCurrentOrganizationId();
            var result = await _service.SubmitCorrectionAsync(dto.TimesheetId, employeeId, orgId, dto);
            if (!result.Success && result.ErrorCode == TimesheetErrorCodes.OverrideLocked)
                return StatusCode(403, result);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting correction");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred while submitting correction" });
        }
    }

    [HttpPut("corrections/{id:guid}/approve")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult> ApproveCorrection(Guid id)
    {
        try
        {
            var managerId = await GetCurrentEmployeeIdAsync();
            var result = await _service.ApproveCorrectionAsync(id, managerId);
            if (!result.Success && result.ErrorCode == TimesheetErrorCodes.AlreadyActioned)
                return Conflict(result);
            if (!result.Success && result.ErrorCode == TimesheetErrorCodes.SelfApproval)
                return BadRequest(result);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving correction {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while approving correction" });
        }
    }

    [HttpPut("corrections/{id:guid}/reject")]
    [PermissionAuthorize("timesheet_approvals_reject")]
    public async Task<ActionResult> RejectCorrection(Guid id, [FromBody] TimesheetTransitionDto dto)
    {
        try
        {
            var managerId = await GetCurrentEmployeeIdAsync();
            var result = await _service.RejectCorrectionAsync(id, managerId, dto.Reason ?? "Rejected");
            if (!result.Success && result.ErrorCode == TimesheetErrorCodes.AlreadyActioned)
                return Conflict(result);
            if (!result.Success && result.ErrorCode == TimesheetErrorCodes.SelfApproval)
                return BadRequest(result);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting correction {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while rejecting correction" });
        }
    }

    [HttpPost("corrections/approve-bulk")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult> ApproveBulk([FromBody] TimesheetBulkApproveDto dto)
    {
        try
        {
            var managerId = await GetCurrentEmployeeIdAsync();
            var serviceResult = await _service.ApproveBulkCorrectionsAsync(dto, managerId);
            if (!serviceResult.Success)
            {
                // Return the raw BulkApproveResult — the frontend expects this exact shape
                var bulkResult = serviceResult.Data ?? new BulkApproveResult
                {
                    Success = false,
                    Message = serviceResult.Message ?? "Bulk approval failed. No changes were applied."
                };
                return BadRequest(bulkResult);
            }
            return Ok(serviceResult.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving bulk corrections");
            return StatusCode(500, new ServiceResponse<int> { Success = false, Message = "An error occurred while approving corrections" });
        }
    }

    // ═══ Overrides ═════════════════════════════════════════════════════════
    [HttpPost("overrides")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApplyOverride([FromBody] TimesheetOverrideDto dto, [FromQuery] Guid timesheetId)
    {
        try
        {
            var managerId = await GetCurrentEmployeeIdAsync();
            var orgId = GetCurrentOrganizationId();
            var result = await _service.ApplyOverrideAsync(timesheetId, managerId, orgId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying override");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while applying override" });
        }
    }

    // ═══ Finalization ══════════════════════════════════════════════════════
    [HttpPost("periods/{id:guid}/finalize")]
    [PermissionAuthorize("timesheet_periods_transition")]
    public async Task<ActionResult<ServiceResponse<int>>> Finalize(Guid id, [FromQuery] Guid? employeeId)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var actorId = await GetCurrentEmployeeIdAsync();
            var result = await _service.FinalizeBatchAsync(id, orgId, actorId, employeeId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing period {Id}", id);
            return StatusCode(500, new ServiceResponse<int> { Success = false, Message = "An error occurred while finalizing period" });
        }
    }

    // ═══ Payroll Bridge ════════════════════════════════════════════════════
    [HttpGet("payroll/finalized-periods")]
    [PermissionAuthorize("timesheet_payroll_export_view")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetPeriodLinkDto>>>> GetFinalizedForPayroll()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetFinalizedForPayrollAsync(orgId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting finalized periods for payroll");
            return StatusCode(500, new ServiceResponse<List<TimesheetPeriodLinkDto>> { Success = false, Message = "An error occurred while retrieving finalized periods" });
        }
    }

    [HttpGet("payroll/summary/{timesheetId:guid}")]
    [PermissionAuthorize("timesheet_payroll_export_view")]
    public async Task<ActionResult<ServiceResponse<TimesheetPayrollSummaryDto>>> GetPayrollSummary(Guid timesheetId)
    {
        try
        {
            var result = await _service.GetPayrollSummaryAsync(timesheetId);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll summary for {TimesheetId}", timesheetId);
            return StatusCode(500, new ServiceResponse<TimesheetPayrollSummaryDto> { Success = false, Message = "An error occurred while retrieving payroll summary" });
        }
    }

    [HttpPost("payroll/lock")]
    [PermissionAuthorize("timesheet_payroll_export_lock")]
    public async Task<ActionResult<ServiceResponse<bool>>> LockForPayroll([FromBody] LockTimesheetRequestDto dto)
    {
        try
        {
            var actorId = await GetCurrentEmployeeIdAsync();
            var result = await _service.LockForPayrollAsync(dto.TimesheetId, actorId, dto.PayrollPeriodId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking timesheet for payroll");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while locking timesheet for payroll" });
        }
    }

    // ═══ Audit Log ═════════════════════════════════════════════════════════
    [HttpGet("periods/{id:guid}/audit")]
    [PermissionAuthorize("timesheet_audit_view")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetAuditEntryDto>>>> GetAuditLog(Guid id)
    {
        try
        {
            var result = await _service.GetAuditLogAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log for period {Id}", id);
            return StatusCode(500, new ServiceResponse<List<TimesheetAuditEntryDto>> { Success = false, Message = "An error occurred while retrieving audit log" });
        }
    }

    // ═══ Workflow & Approval ═══════════════════════════════════════════════
    [HttpGet("workflows")]
    [PermissionAuthorize("timesheet_workflow_view")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetApprovalWorkflowDto>>>> GetWorkflows()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var workflows = (await _workflowRepo.GetWorkflowsAsync(orgId)).ToList();
            return Ok(new ServiceResponse<List<TimesheetApprovalWorkflowDto>> { Success = true, Data = workflows });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            return StatusCode(500, new ServiceResponse<List<TimesheetApprovalWorkflowDto>> { Success = false, Message = "An error occurred while retrieving workflows" });
        }
    }

    [HttpPost("workflows")]
    [PermissionAuthorize("timesheet_workflow_create")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreateWorkflow([FromBody] TimesheetApprovalWorkflowDto dto)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var id = await _workflowRepo.CreateWorkflowAsync(orgId, dto);
            return Ok(new ServiceResponse<Guid> { Success = true, Data = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred while creating workflow" });
        }
    }

    [HttpGet("approvals/queue")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetApprovalProgressDto>>>> GetMyApprovalQueue()
    {
        try
        {
            var approverId = await GetCurrentEmployeeIdAsync();
            var orgId = GetCurrentOrganizationId();
            var items = (await _workflowRepo.GetMyApprovalQueueAsync(approverId, orgId)).ToList();
            return Ok(new ServiceResponse<List<TimesheetApprovalProgressDto>> { Success = true, Data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval queue");
            return StatusCode(500, new ServiceResponse<List<TimesheetApprovalProgressDto>> { Success = false, Message = "An error occurred while retrieving approval queue" });
        }
    }

    [HttpPost("approvals/{progressId:guid}/approve")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveStep(Guid progressId)
    {
        try
        {
            var ok = await _workflowRepo.ApproveStepAsync(progressId);
            return ok ? Ok(new ServiceResponse<bool> { Success = true, Data = true })
                      : BadRequest(new ServiceResponse<bool> { Success = false, Message = "Step not found or already processed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving step {Id}", progressId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while approving step" });
        }
    }

    [HttpPost("approvals/{progressId:guid}/reject")]
    [PermissionAuthorize("timesheet_approvals_reject")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectStep(Guid progressId, [FromBody] TimesheetTransitionDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new ServiceResponse<bool> { Success = false, Message = "Rejection reason is required." });
            var ok = await _workflowRepo.RejectStepAsync(progressId, dto.Reason);
            return ok ? Ok(new ServiceResponse<bool> { Success = true, Data = true })
                      : BadRequest(new ServiceResponse<bool> { Success = false, Message = "Step not found or already processed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting step {Id}", progressId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while rejecting step" });
        }
    }

    // ═══ Reopen Requests ═══════════════════════════════════════════════════
    [HttpPost("periods/{id:guid}/request-reopen")]
    [PermissionAuthorize("timesheet_periods_view")]
    public async Task<ActionResult<ServiceResponse<Guid>>> RequestReopen(Guid id, [FromBody] TimesheetTransitionDto dto)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            var reopenId = await _workflowRepo.CreateReopenRequestAsync(id, employeeId, dto.Reason ?? "Reopen requested");
            if (!reopenId.HasValue)
                return BadRequest(new ServiceResponse<Guid> { Success = false, Message = "Failed to create reopen request." });
            return Ok(new ServiceResponse<Guid> { Success = true, Data = reopenId.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting reopen for period {Id}", id);
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred while requesting reopen" });
        }
    }

    [HttpGet("reopen-requests/pending")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetReopenRequestDto>>>> GetPendingReopenRequests()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var items = (await _workflowRepo.GetPendingReopenRequestsAsync(orgId)).ToList();
            return Ok(new ServiceResponse<List<TimesheetReopenRequestDto>> { Success = true, Data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending reopen requests");
            return StatusCode(500, new ServiceResponse<List<TimesheetReopenRequestDto>> { Success = false, Message = "An error occurred while retrieving reopen requests" });
        }
    }

    [HttpPost("reopen-requests/{id:guid}/approve")]
    [PermissionAuthorize("timesheet_approvals_approve")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveReopenRequest(Guid id)
    {
        try
        {
            var reviewerId = await GetCurrentEmployeeIdAsync();
            var ok = await _workflowRepo.ApproveReopenRequestAsync(id, reviewerId);
            return ok ? Ok(new ServiceResponse<bool> { Success = true, Data = true })
                      : BadRequest(new ServiceResponse<bool> { Success = false, Message = "Reopen request not found or already processed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving reopen request {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while approving reopen request" });
        }
    }

    [HttpPost("reopen-requests/{id:guid}/reject")]
    [PermissionAuthorize("timesheet_approvals_reject")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectReopenRequest(Guid id, [FromBody] TimesheetTransitionDto dto)
    {
        try
        {
            var reviewerId = await GetCurrentEmployeeIdAsync();
            var ok = await _workflowRepo.RejectReopenRequestAsync(id, reviewerId, dto.Reason ?? "Rejected");
            return ok ? Ok(new ServiceResponse<bool> { Success = true, Data = true })
                      : BadRequest(new ServiceResponse<bool> { Success = false, Message = "Reopen request not found or already processed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting reopen request {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while rejecting reopen request" });
        }
    }

    // ═══ Helpers ═══════════════════════════════════════════════════════════
    private async Task<Guid> GetCurrentEmployeeIdAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID format");
        var employeeId = await _employeeRepository.GetEmployeeIdAsync(userId);
        if (employeeId == null)
            throw new UnauthorizedAccessException("Employee profile not found");
        return employeeId.Value;
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new UnauthorizedAccessException("Organization ID not found in token");
        if (!Guid.TryParse(orgIdClaim, out var orgId))
            throw new UnauthorizedAccessException("Invalid organization ID format");
        return orgId;
    }


    // ═══ Phase 2-5: Projects, Allocations, CompTime, Dashboard, Delegation, RateCards, Bulk Operations ═══

    [HttpGet("projects")]
    [PermissionAuthorize("projects_view")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetProjectDto>>>> GetProjects()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetProjectsAsync(orgId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<List<TimesheetProjectDto>> { Success = false, Message = "An error occurred while retrieving projects" }); }
    }

    [HttpPost("projects")]
    [PermissionAuthorize("projects_create")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreateProject([FromBody] TimesheetProjectDto dto)
    {
        try
        {
            dto.OrganizationId = GetCurrentOrganizationId();
            var result = await _service.CreateProjectAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred while creating project" }); }
    }

    [HttpGet("projects/{projectId:guid}/tasks")]
    [PermissionAuthorize("projects_view")]
    public async Task<ActionResult<ServiceResponse<List<TimesheetTaskDto>>>> GetProjectTasks(Guid projectId)
    {
        try
        {
            var result = await _service.GetProjectTasksAsync(projectId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<List<TimesheetTaskDto>> { Success = false, Message = "An error occurred while retrieving tasks" }); }
    }

    [HttpPost("projects/{projectId:guid}/tasks")]
    [PermissionAuthorize("projects_create")]
    public async Task<ActionResult<ServiceResponse<TimesheetTaskDto>>> CreateTask(Guid projectId, [FromBody] TimesheetTaskDto dto)
    {
        try
        {
            var result = await _service.CreateTaskAsync(projectId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<TimesheetTaskDto> { Success = false, Message = "An error occurred while creating the task" }); }
    }

    [HttpGet("periods/{timesheetId:guid}/allocations")]
    [PermissionAuthorize("timesheet_allocations_view")]
    public async Task<ActionResult<ServiceResponse<List<TimeAllocationDto>>>> GetTimeAllocations(Guid timesheetId, [FromQuery] Guid employeeId)
    {
        try
        {
            var result = await _service.GetTimeAllocationsAsync(timesheetId, employeeId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<List<TimeAllocationDto>> { Success = false, Message = "An error occurred while retrieving time allocations" }); }
    }

    [HttpPut("periods/{timesheetId:guid}/allocations")]
    [PermissionAuthorize("timesheet_allocations_edit")]
    public async Task<ActionResult<ServiceResponse<bool>>> SaveTimeAllocations(Guid timesheetId, [FromBody] SaveTimeAllocationDto dto)
    {
        try
        {
            dto.TimesheetId = timesheetId;
            var result = await _service.SaveTimeAllocationsAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred while saving time allocations" }); }
    }

    [HttpGet("comp-time")]
    [PermissionAuthorize("comp_time_view")]
    public async Task<ActionResult<ServiceResponse<CompTimeBalanceDto>>> GetCompTimeBalance([FromQuery] Guid employeeId, [FromQuery] int year)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetCompTimeBalanceAsync(employeeId, orgId, year);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<CompTimeBalanceDto> { Success = false, Message = "An error occurred while retrieving comp time balance" }); }
    }

    [HttpGet("dashboard")]
    [PermissionAuthorize("dashboard_view")]
    public async Task<ActionResult<ServiceResponse<TimesheetDashboardDto>>> GetDashboard([FromQuery] Guid? periodId)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetDashboardAsync(orgId, periodId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<TimesheetDashboardDto> { Success = false, Message = "An error occurred while retrieving dashboard" }); }
    }

    [HttpGet("delegations/active")]
    [PermissionAuthorize("delegation_view")]
    public async Task<ActionResult<ServiceResponse<TimesheetDelegationDto?>>> GetActiveDelegation([FromQuery] Guid approverId)
    {
        try
        {
            var result = await _service.GetActiveDelegationAsync(approverId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<TimesheetDelegationDto?> { Success = false, Message = "An error occurred while retrieving active delegation" }); }
    }

    [HttpPost("delegations")]
    [PermissionAuthorize("delegation_create")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreateDelegation([FromBody] CreateTimesheetDelegationDto dto)
    {
        try
        {
            var delegatedFrom = await GetCurrentEmployeeIdAsync();
            var result = await _service.CreateDelegationAsync(dto, delegatedFrom);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred while creating delegation" }); }
    }

    [HttpGet("rate-cards")]
    [PermissionAuthorize("rate_cards_view")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeRateCardDto>>>> GetRateCards()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.GetRateCardsAsync(orgId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<List<EmployeeRateCardDto>> { Success = false, Message = "An error occurred while retrieving rate cards" }); }
    }

    [HttpPost("periods/bulk-copy")]
    [PermissionAuthorize("timesheet_periods_create")]
    public async Task<ActionResult<ServiceResponse<bool>>> BulkCopyFromPrevious([FromBody] BulkCopyRequestDto dto)
    {
        try
        {
            var result = await _service.BulkCopyFromPreviousAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred during bulk copy" }); }
    }

    [HttpPost("periods/autofill")]
    [PermissionAuthorize("timesheet_periods_create")]
    public async Task<ActionResult<ServiceResponse<bool>>> AutoFillFromSchedule([FromBody] BulkAutoFillRequestDto dto)
    {
        try
        {
            var result = await _service.AutoFillFromScheduleAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred during auto-fill" }); }
    }
}