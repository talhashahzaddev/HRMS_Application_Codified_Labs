using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(
        IPerformanceService performanceService,
        IEmployeeService employeeService,
        ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService;
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet("get-allcycles")]
    public async Task<ActionResult<ServiceResponse<List<AppraisalCycleDto>>>> GetAppraisalCycles()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var cycles = await _performanceService.GetAppraisalCyclesAsync(organizationId);

            return Ok(new ServiceResponse<List<AppraisalCycleDto>>
            {
                Success = true,
                Data = cycles
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving appraisal cycles");
            return BadRequest(new ServiceResponse<List<AppraisalCycleDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access retrieving appraisal cycles");
            return StatusCode(403, new ServiceResponse<List<AppraisalCycleDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appraisal cycles");
            return StatusCode(500, new ServiceResponse<List<AppraisalCycleDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving appraisal cycles"
            });
        }
    }

    [HttpPost("create-cycles")]
    [PermissionAuthorize("APPRAISAL_CYCLE_BUTTON")]

    public async Task<ActionResult<ServiceResponse<AppraisalCycleDto>>> CreateAppraisalCycle([FromBody] CreateAppraisalCycleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AppraisalCycleDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            //i change this function time stamp problem performance.fn_create_appraisal_cycle
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();
            var cycle = await _performanceService.CreateAppraisalCycleAsync(request, organizationId, currentEmployeeId);

            return Ok(new ServiceResponse<AppraisalCycleDto>
            {
                Success = true,
                Message = "Appraisal cycle created successfully",
                Data = cycle
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating appraisal cycle");
            return BadRequest(new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating appraisal cycle");
            return BadRequest(new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appraisal cycle");
            return StatusCode(500, new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = "An error occurred while creating appraisal cycle"
            });
        }
    }


    [HttpDelete("delete-cycle/{cycleId}")]
    [PermissionAuthorize("APPRAISAL_CYCLE_DELETE_BUTTON")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAppraisalCycle(Guid cycleId)
    {
        try
        {
            if (cycleId == Guid.Empty)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid appraisal cycle ID"
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();

            var deleted = await _performanceService.DeleteAppraisalCycleAsync(cycleId);

            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Appraisal cycle not found or could not be deleted"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Appraisal cycle deleted successfully",
                Data = true
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while deleting appraisal cycle");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error deleting appraisal cycle");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appraisal cycle");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting appraisal cycle"
            });
        }
    }




    [HttpGet("cycles/{cycleId}/appraisals")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeAppraisalDto>>>> GetEmployeeAppraisals(Guid cycleId, [FromQuery] Guid? employeeId = null)
    {
        var employeeIdToUse = await GetCurrentEmployeeId();
        try
        {
            var appraisals = await _performanceService.GetEmployeeAppraisalsAsync(cycleId, employeeIdToUse);

            return Ok(new ServiceResponse<List<EmployeeAppraisalDto>>
            {
                Success = true,
                Data = appraisals
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee appraisals for cycle {CycleId}", cycleId);
            return StatusCode(500, new ServiceResponse<List<EmployeeAppraisalDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee appraisals"
            });
        }
    }

    [HttpGet("appraisals")]
    public async Task<ActionResult<ServiceResponse<PagedResult<EmployeeAppraisalDto>>>> GetEmployeeAppraisals(
        [FromQuery] Guid? appraisalCycleId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? department = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? managerId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var filter = new AppraisalFilterRequest
            {
                AppraisalCycleId = appraisalCycleId,
                Status = status,
                Department = department,
                EmployeeId = employeeId,
                ManagerId = managerId,
                Search = search
            };

            var result = await _performanceService.GetEmployeeAppraisalsPagedAsync(organizationId, filter, page, limit);

            return Ok(new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving employee appraisals");
            return BadRequest(new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee appraisals");
            return StatusCode(500, new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee appraisals"
            });
        }
    }

    [HttpGet("appraisals/my-team-employees")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeListItemDto>>>> GetMyTeamEmployees()
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            if (currentEmployeeId == Guid.Empty)
            {
                return Ok(new ServiceResponse<List<EmployeeListItemDto>>
                {
                    Success = true,
                    Data = new List<EmployeeListItemDto>(),
                    Message = "No employee record found for this user"
                });
            }

            var employees = await _employeeService.GetEmployeesByManagerAsync(currentEmployeeId, organizationId);

            return Ok(new ServiceResponse<IReadOnlyList<EmployeeListItemDto>>
            {
                Success = true,
                Data = employees
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for my team employees");
            return StatusCode(403, new ServiceResponse<IReadOnlyList<EmployeeListItemDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my team employees");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<EmployeeListItemDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving team employees"
            });
        }
    }

    [HttpGet("appraisals/my-created-appraisals")]
    public async Task<ActionResult<ServiceResponse<PagedResult<EmployeeAppraisalDto>>>> GetMyCreatedAppraisals(
        [FromQuery] Guid? appraisalCycleId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            if (currentEmployeeId == Guid.Empty)
            {
                return Ok(new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
                {
                    Success = true,
                    Data = new PagedResult<EmployeeAppraisalDto>
                    {
                        Data = new List<EmployeeAppraisalDto>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = limit
                    },
                    Message = "No employee record found for this user"
                });
            }

            var filter = new AppraisalFilterRequest
            {
                AppraisalCycleId = appraisalCycleId,
                Status = status,
                EmployeeId = employeeId,
                ManagerId = currentEmployeeId, // Filter by current manager's employee ID
                Search = search
            };

            var result = await _performanceService.GetEmployeeAppraisalsPagedAsync(organizationId, filter, page, limit);

            return Ok(new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving my created appraisals");
            return BadRequest(new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for my created appraisals");
            return StatusCode(403, new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my created appraisals");
            return StatusCode(500, new ServiceResponse<PagedResult<EmployeeAppraisalDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving appraisals"
            });
        }
    }

    [HttpPost("appraisals")]
    public async Task<ActionResult<ServiceResponse<EmployeeAppraisalDto>>> CreateEmployeeAppraisal(
    [FromBody] CreateAppraisalRequest request)
    {
        try
        {
            var reviewerId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            var appraisal =
                await _performanceService.CreateEmployeeAppraisalAsync(
                    request, reviewerId, organizationId);

            return Ok(new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = true,
                Message = "Employee appraisal created successfully",
                Data = appraisal
            });
        }
        catch (InvalidOperationException ex)
        {
            // ⚠️ BUSINESS WARNING (KRA already submitted)
            return StatusCode(400, new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee appraisal");

            return StatusCode(500, new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = false,
                Message = "An error occurred while creating employee appraisal",
                Data = null
            });
        }
    }

    [HttpPost("skills")]
    [PermissionAuthorize("CREATE_SKILLS_BUTTON")]


    public async Task<ActionResult<ServiceResponse<SkillSetDto>>> CreateSkillSet([FromBody] CreateSkillSetRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var skill = await _performanceService.CreateSkillSetAsync(request, organizationId);
            return Ok(new ServiceResponse<SkillSetDto> { Success = true, Message = "Skill created successfully", Data = skill });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ServiceResponse<SkillSetDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill set");
            return StatusCode(500, new ServiceResponse<SkillSetDto> { Success = false, Message = "An unexpected error occurred" });
        }
    }

    [HttpGet("skills")]
    public async Task<ActionResult<ServiceResponse<List<SkillSetDto>>>> GetSkillSets([FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var filter = new SkillSetFilterRequest { Search = search, Category = category, IsActive = isActive };
            var result = await _performanceService.GetSkillSetsPagedAsync(organizationId, filter, page, limit);
            return Ok(new ServiceResponse<PagedResult<SkillSetDto>> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skill sets");
            return StatusCode(500, new ServiceResponse<object> { Success = false, Message = "An error occurred while retrieving skill sets" });
        }
    }

    [HttpGet("skills/{skillId}")]
    public async Task<ActionResult<ServiceResponse<SkillWithEmployeesDto>>> GetSkillWithEmployees(Guid skillId)
    {
        try
        {
            var result = await _performanceService.GetSkillWithEmployeesAsync(skillId);
            if (result == null)
                return NotFound(new ServiceResponse<SkillWithEmployeesDto> { Success = false, Message = "Skill not found" });
            return Ok(new ServiceResponse<SkillWithEmployeesDto> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skill {SkillId}", skillId);
            return StatusCode(500, new ServiceResponse<SkillWithEmployeesDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPatch("skills/{skillId}/toggle-status")]
    public async Task<ActionResult<ServiceResponse<SkillSetDto>>> ToggleSkillStatus(Guid skillId)
    {
        try
        {
            var result = await _performanceService.ToggleSkillStatusAsync(skillId);
            return Ok(new ServiceResponse<SkillSetDto> { Success = true, Data = result, Message = "Skill status updated" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<SkillSetDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling skill status {SkillId}", skillId);
            return StatusCode(500, new ServiceResponse<SkillSetDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("skills/{skillId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSkill(Guid skillId)
    {
        try
        {
            var result = await _performanceService.DeleteSkillSetAsync(skillId);
            return Ok(new ServiceResponse<bool> { Success = true, Data = result, Message = "Skill deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill {SkillId}", skillId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPost("my-skills")]
    [PermissionAuthorize("ADD_SKILLS_BUTTON")]

    public async Task<IActionResult> AddMySkill([FromBody] CreateEmployeeSkillRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var currentUserId = await GetCurrentEmployeeId();
            request.EmployeeId = currentUserId;
            request.AssessedBy = null; // Self-added, no assessor yet
            request.LastAssessed = DateTime.UtcNow;

            var result = await _performanceService.CreateEmployeeSkillAsync(request);
            return Ok(new { success = true, message = "Skill added successfully.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding skill for current employee");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpGet("my-skills")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeSkillDto>>>> GetMySkills()
    {
        try
        {
            var employeeId = await GetCurrentEmployeeId();
            var skills = await _performanceService.GetEmployeeSkillsAsync(employeeId);
            return Ok(new ServiceResponse<List<EmployeeSkillDto>> { Success = true, Data = skills });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current employee skills");
            return StatusCode(500, new ServiceResponse<List<EmployeeSkillDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("my-skills/{employeeSkillId}")]
    public async Task<IActionResult> UpdateMySkill(Guid employeeSkillId, [FromBody] UpdateEmployeeSkillRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var currentUserId = await GetCurrentEmployeeId();
            // Employee assesses themselves
            request.AssessedBy = currentUserId;
            if (!request.LastAssessed.HasValue) request.LastAssessed = DateTime.UtcNow;

            var result = await _performanceService.UpdateEmployeeSkillAsync(employeeSkillId, request);
            return Ok(new { success = true, message = "Skill updated successfully.", data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill {EmployeeSkillId}", employeeSkillId);
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpDelete("my-skills/{employeeSkillId}")]
    public async Task<IActionResult> DeleteMySkill(Guid employeeSkillId)
    {
        try
        {
            var result = await _performanceService.DeleteEmployeeSkillAsync(employeeSkillId);
            return Ok(new { success = result, message = result ? "Skill removed successfully." : "Skill not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill {EmployeeSkillId}", employeeSkillId);
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpGet("employee-skills/team-summary")]
    [PermissionAuthorize("skill_matrix_pending_skill_assessments")]
    public async Task<ActionResult<ServiceResponse<PagedResult<EmployeeSkillDto>>>> GetTeamEmployeeSkillsSummary([FromQuery] Guid? employeeId = null, [FromQuery] Guid? skillSetId = null, [FromQuery] string? department = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();
            
            var filter = new EmployeeSkillFilterRequest
            {
                EmployeeId = employeeId,
                SkillSetId = skillSetId,
                Department = department,
                Search = search,
                ReportingManagerId = currentEmployeeId,
                OnlySelfAdded = true // Fetch only skills that haven't been assessed by others yet
            };
            
            // We use GetEmployeeSkillsByManagerPagedAsync to get individual skills instead of summary
            var result = await _performanceService.GetEmployeeSkillsByManagerPagedAsync(currentEmployeeId, filter, page, limit);
            return Ok(new ServiceResponse<PagedResult<EmployeeSkillDto>> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team employee skills");
            return StatusCode(500, new ServiceResponse<PagedResult<EmployeeSkillDto>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("employee-skills/detail/{employeeId}")]
    public async Task<ActionResult<ServiceResponse<EmployeeSkillFullDetailDto>>> GetEmployeeSkillDetail(Guid employeeId)
    {
        try
        {
            var result = await _performanceService.GetEmployeeSkillFullDetailAsync(employeeId);
            return Ok(new ServiceResponse<EmployeeSkillFullDetailDto> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee skill detail {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<EmployeeSkillFullDetailDto> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("employee-skills/{employeeSkillId}/assess")]
    public async Task<IActionResult> AssessEmployeeSkill(Guid employeeSkillId, [FromBody] UpdateEmployeeSkillRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var currentUserId = await GetCurrentEmployeeId();
            request.AssessedBy = currentUserId;
            if (!request.LastAssessed.HasValue) request.LastAssessed = DateTime.UtcNow;

            var result = await _performanceService.UpdateEmployeeSkillAsync(employeeSkillId, request);
            return Ok(new { success = true, message = "Skill assessed successfully.", data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing skill {EmployeeSkillId}", employeeSkillId);
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpPut("employee-skills/{employeeSkillId}")]
    public async Task<IActionResult> UpdateEmployeeSkill(Guid employeeSkillId, [FromBody] UpdateEmployeeSkillRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Get current logged-in user ID
            var currentUserId = await GetCurrentEmployeeId();

            // Set AssessedBy to current logged-in user (the person assessing/rating)
            //if (!request.AssessedBy.HasValue || request.AssessedBy == Guid.Empty)
            //{
            request.AssessedBy = currentUserId;
            //}

            // Set LastAssessed to current time if not provided
            if (!request.LastAssessed.HasValue)
            {
                request.LastAssessed = DateTime.UtcNow;
            }

            var result = await _performanceService.UpdateEmployeeSkillAsync(employeeSkillId, request);

            if (result == null)
                return StatusCode(500, new { success = false, message = "Failed to update employee skill." });

            return Ok(new
            {
                success = true,
                message = "Employee skill updated successfully.",
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Employee skill not found: {EmployeeSkillId}", employeeSkillId);
            return NotFound(new
            {
                success = false,
                message = $"Employee skill with ID {employeeSkillId} not found."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for updating employee skill: {EmployeeSkillId}", employeeSkillId);
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating employee skill");
            return StatusCode(500, new
            {
                success = false,
                message = "An unexpected error occurred while updating the employee skill."
            });
        }
    }

    [HttpGet("employees/{employeeId}/skills")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeSkillDto>>>> GetEmployeeSkills(Guid employeeId)
    {
        try
        {
            var employeeTableId = await GetCurrentEmployeeId();
            var skills = await _performanceService.GetEmployeeSkillsAsync(employeeTableId);

            return Ok(new ServiceResponse<List<EmployeeSkillDto>>
            {
                Success = true,
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee skills for {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<List<EmployeeSkillDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee skills"
            });
        }
    }
    [HttpGet("Otheremployees/{employeeId}/skills")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeSkillDto>>>> GetOtherEmployeeSkills(Guid employeeId)
    {
        try
        {
            //var employeeTableId = await GetCurrentEmployeeId();
            var skills = await _performanceService.GetEmployeeSkillsAsync(employeeId);

            return Ok(new ServiceResponse<List<EmployeeSkillDto>>
            {
                Success = true,
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee skills for {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<List<EmployeeSkillDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee skills"
            });
        }
    }

    [HttpGet("employee-skills/my-team")]
    public async Task<ActionResult<ServiceResponse<PagedResult<EmployeeSkillDto>>>> GetMyTeamEmployeeSkills(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? skillSetId = null,
        [FromQuery] int? proficiencyLevel = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();

            if (currentEmployeeId == Guid.Empty)
            {
                return Ok(new ServiceResponse<PagedResult<EmployeeSkillDto>>
                {
                    Success = true,
                    Data = new PagedResult<EmployeeSkillDto>
                    {
                        Data = new List<EmployeeSkillDto>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = limit
                    },
                    Message = "No employee record found for this user"
                });
            }

            var filter = new EmployeeSkillFilterRequest
            {
                EmployeeId = employeeId,
                SkillSetId = skillSetId,
                ProficiencyLevel = proficiencyLevel,
                Search = search
            };

            var result = await _performanceService.GetEmployeeSkillsByManagerPagedAsync(currentEmployeeId, filter, page, limit);

            return Ok(new ServiceResponse<PagedResult<EmployeeSkillDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving my team employee skills");
            return BadRequest(new ServiceResponse<PagedResult<EmployeeSkillDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for my team employee skills");
            return StatusCode(403, new ServiceResponse<PagedResult<EmployeeSkillDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my team employee skills");
            return StatusCode(500, new ServiceResponse<PagedResult<EmployeeSkillDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee skills"
            });
        }
    }

    [HttpGet("employee-skills")]
    [PermissionAuthorize("skill_matrix_all_employees_skills")]
    public async Task<ActionResult<ServiceResponse<PagedResult<EmployeeSkillSummaryDto>>>> GetAllEmployeeSkills(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? skillSetId = null,
        [FromQuery] string? department = null,
        [FromQuery] int? proficiencyLevel = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var filter = new EmployeeSkillFilterRequest
            {
                EmployeeId = employeeId,
                SkillSetId = skillSetId,
                Department = department,
                ProficiencyLevel = proficiencyLevel,
                Search = search
            };

            var result = await _performanceService.GetEmployeeSkillsSummaryPagedAsync(organizationId, filter, page, limit);

            return Ok(new ServiceResponse<PagedResult<EmployeeSkillSummaryDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving all employee skills");
            return BadRequest(new ServiceResponse<PagedResult<EmployeeSkillSummaryDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all employee skills");
            return StatusCode(500, new ServiceResponse<PagedResult<EmployeeSkillSummaryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee skills"
            });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ServiceResponse<MYPerformanceSummary>>> GetPerformanceSummary(Guid? cycleId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var summary = await _performanceService.GetPerformanceSummaryAsync(organizationId, cycleId);

            return Ok(new ServiceResponse<PerformanceSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving performance summary");
            return BadRequest(new ServiceResponse<PerformanceSummaryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance summary");
            return StatusCode(500, new ServiceResponse<PerformanceSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving performance summary"
            });
        }
    }

    [HttpGet("My/summary")]
    public async Task<ActionResult<ServiceResponse<MYPerformanceSummary>>> GetMyPerformanceSummary()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmp = await GetCurrentEmployeeId();
            var summary = await _performanceService.GetMyPerformanceSummaryAsync(organizationId, currentEmp);

            return Ok(new ServiceResponse<MYPerformanceSummary>
            {
                Success = true,
                Data = summary
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving performance summary");
            return BadRequest(new ServiceResponse<MYPerformanceSummary>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance summary");
            return StatusCode(500, new ServiceResponse<MYPerformanceSummary>
            {
                Success = false,
                Message = "An error occurred while retrieving performance summary"
            });
        }
    }


    [HttpGet("team-performance-overview")]
    public async Task<ActionResult<ServiceResponse<TeamPerformanceOverviewDto>>> GetTeamPerformanceOverview()
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            if (currentEmployeeId == Guid.Empty)
            {
                return Ok(new ServiceResponse<TeamPerformanceOverviewDto>
                {
                    Success = true,
                    Data = new TeamPerformanceOverviewDto { Employees = new List<TeamPerformanceEmployeeDto>() },
                    Message = "No employee record found for this user"
                });
            }

            var result = await _performanceService.GetTeamPerformanceOverviewAsync(currentEmployeeId, organizationId);

            return Ok(new ServiceResponse<TeamPerformanceOverviewDto>
            {
                Success = true,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for team performance overview");
            return StatusCode(403, new ServiceResponse<TeamPerformanceOverviewDto>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team performance overview");
            return StatusCode(500, new ServiceResponse<TeamPerformanceOverviewDto>
            {
                Success = false,
                Message = "An error occurred while retrieving team performance overview"
            });
        }
    }



    [HttpPost("create-Kra")]
   [PermissionAuthorize("CREATE_KRA_BUTTON")]

    public async Task<ActionResult<ServiceResponse<KRADto>>> CreateKra([FromQuery] CreateKRARequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var CurrentEmpId = await GetCurrentEmployeeId();
            var result = await _performanceService.CreateKRAAsync(organizationId, CurrentEmpId, request);

            return Ok(new ServiceResponse<KRADto>
            {
                Success = true,
                Data = result,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error While creating KRas");
            return StatusCode(500, new ServiceResponse<List<TopPerformerDto>>
            {
                Success = false,
                Message = "Error While creating KRass"
            });
        }
    }

    [HttpGet("get-allKra")]
    public async Task<ActionResult<ServiceResponse<PagedResult<KRADto>>>> GetKra([FromQuery] string? search = null, [FromQuery] int page = 1,
    [FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _performanceService.GetKRAsAsync(organizationId, page, limit, search);

            return Ok(new ServiceResponse<PagedResult<KRADto>>
            {
                Success = true,
                Data = result,
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while retrieving KRAs");
            return BadRequest(new ServiceResponse<PagedResult<KRADto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error While Getting all KRas");
            return StatusCode(500, new ServiceResponse<PagedResult<KRADto>>
            {
                Success = false,
                Message = "An error occurred while retrieving KRAs"
            });
        }
    }


    [HttpGet("KraById/{kraId}")]
    public async Task<ActionResult<ServiceResponse<KRADto>>> GetKraById(Guid kraId)
    {
        try
        {
            //var organizationId = GetCurrentOrganizationId();
            var result = await _performanceService.GetKRAByIdAsync(kraId);

            if (result == null)
            {
                return NotFound(new ServiceResponse<KRADto>
                {
                    Success = false,
                    Message = "KRA not found"
                });
            }

            return Ok(new ServiceResponse<KRADto>
            {
                Success = true,
                Data = result,
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while retrieving KRA by id");
            return BadRequest(new ServiceResponse<KRADto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error While Getting KRas By Id");
            return StatusCode(500, new ServiceResponse<KRADto>
            {
                Success = false,
                Message = "Error Getting KRass Id"
            });
        }
    }


    [HttpPut("KraUpdate")]
    [PermissionAuthorize("KRA_EDIT_BUTTON")]

    public async Task<ActionResult<ServiceResponse<KRADto>>> UpdateKra([FromQuery] UpdateKRARequest request, Guid kraId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _performanceService.UpdateKRAAsync(kraId, request);

            return Ok(new ServiceResponse<KRADto>
            {
                Success = true,
                Data = result,
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "KRA not found while updating: {KRAId}", kraId);
            return NotFound(new ServiceResponse<KRADto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating KRA");
            return BadRequest(new ServiceResponse<KRADto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error While creating KRas");
            return StatusCode(500, new ServiceResponse<KRADto>
            {
                Success = false,
                Message = "Error While creating KRass"
            });
        }
    }


    [HttpPatch("KRAStatus/{kraId}")]
    public async Task<ActionResult<ServiceResponse<string>>> UpdateKRAStatus(Guid kraId, [FromQuery] bool isActive)
    {
        try
        {
            var result = await _performanceService.UpdateKRAStatusAsync(kraId, isActive);

            if (!result)
                return NotFound(new ServiceResponse<string> { Success = false, Message = "KRA not found" });

            return Ok(new ServiceResponse<string>
            {
                Success = true,
                Message = $"KRA status updated to {(isActive ? "Active" : "Inactive")} successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "KRA not found while updating status: {KRAId}", kraId);
            return NotFound(new ServiceResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating KRA status");
            return StatusCode(500, new ServiceResponse<string>
            {
                Success = false,
                Message = "Error updating KRA status"
            });
        }
    }


    [HttpPost("create-goal")]
    [PermissionAuthorize("CREATE_GOAL_BUTTON")]
    public async Task<ActionResult<ServiceResponse<GoalDto>>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmpId = await GetCurrentEmployeeId();

            var result = await _performanceService.CreateGoalAsync(organizationId, currentEmpId, request);

            return Ok(new ServiceResponse<GoalDto>
            {
                Success = true,
                Data = result,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating Goal");
            return StatusCode(500, new ServiceResponse<GoalDto>
            {
                Success = false,
                Message = "Error while creating Goal"
            });
        }
    }

    [HttpGet("goals")]
    public async Task<ActionResult<ServiceResponse<List<GoalDto>>>> GetAllGoals()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetAllGoalsAsync(organizationId, currentEmployeeId);
            return Ok(new ServiceResponse<List<GoalDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching Goals");
            return StatusCode(500, new ServiceResponse<List<GoalDto>>
            {
                Success = false,
                Message = "Error while fetching Goals"
            });
        }
    }

    [HttpGet("employee/Assigned/goals")]
    public async Task<ActionResult<ServiceResponse<List<GoalDto>>>> GetEmployeeAssignedGoals()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetEmployeeAssignedGoalsAsync(organizationId, currentEmployeeId);
            return Ok(new ServiceResponse<List<GoalDto>>
            {
                Success = true,
                Data = result
            
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching Goals");
            return StatusCode(500, new ServiceResponse<List<GoalDto>>
            {
                Success = false,
                Message = "Error while fetching Goals"
            });
        }
    }

    [HttpGet("goals/{goalId}")]
    public async Task<ActionResult<ServiceResponse<GoalDto>>> GetGoalById(Guid goalId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _performanceService.GetGoalByIdAsync(organizationId, goalId);
            if (result == null)
                return NotFound(new ServiceResponse<GoalDto> { Success = false, Message = "Goal not found" });

            return Ok(new ServiceResponse<GoalDto> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching Goal {GoalId}", goalId);
            return StatusCode(500, new ServiceResponse<GoalDto>
            {
                Success = false,
                Message = "Error while fetching Goal"
            });
        }
    }

    [HttpPut("update-goal")]
    public async Task<ActionResult<ServiceResponse<GoalDto>>> UpdateGoal([FromBody] UpdateGoalRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _performanceService.UpdateGoalAsync(organizationId, request);
            if (result == null)
                return NotFound(new ServiceResponse<GoalDto> { Success = false, Message = "Goal not found" });

            return Ok(new ServiceResponse<GoalDto> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating Goal {GoalId}", request.GoalId);
            return StatusCode(500, new ServiceResponse<GoalDto>
            {
                Success = false,
                Message = "Error while updating Goal"
            });
        }
    }

    [HttpPatch("GoalStatus/{goalId}")]
    public async Task<ActionResult<ServiceResponse<string>>> UpdateGoalStatus(
    Guid goalId,
    [FromQuery] string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Status is required"
                });
            }

            var result = await _performanceService.UpdateGoalStatusAsync(goalId, status);

            if (!result)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Goal not found"
                });
            }

            return Ok(new ServiceResponse<string>
            {
                Success = true,
                Message = $"Goal status updated to {status} successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Goal not found while updating status: {GoalId}", goalId);

            return NotFound(new ServiceResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating Goal status");

            return StatusCode(500, new ServiceResponse<string>
            {
                Success = false,
                Message = "Error updating Goal status"
            });
        }
    }


    [HttpDelete("delete-goal/{goalId}")]
    [PermissionAuthorize("Delete Goal Button")]

    public async Task<ActionResult<ServiceResponse<bool>>> DeleteGoal(Guid goalId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            await _performanceService.DeleteGoalAsync(organizationId, goalId);
            return Ok(new ServiceResponse<bool> { Success = true, Data = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting Goal {GoalId}", goalId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "Error while deleting Goal"
            });
        }
    }

    [HttpPost("goals/assign-employee")]
    [PermissionAuthorize("ASSIGN_GOAL_BUTTON")]

    public async Task<ActionResult<ServiceResponse<bool>>> AssignGoalToEmployee(
     [FromQuery] Guid goalId,
     [FromQuery] Guid assignedTo)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var assignedBy = await GetCurrentEmployeeId();
            var result = await _performanceService.AssignGoalAsync(organizationId, goalId, assignedTo, assignedBy);

            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Goal not found or could not be assigned"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Data = result,
                Message = "Goal assigned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while assigning Goal {GoalId}", goalId);

            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "Error while assigning Goal"
            });
        }
    }

    [HttpGet("top-performers")]
    public async Task<ActionResult<ServiceResponse<List<TopPerformerDto>>>> GetTopPerformers([FromQuery] int count = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var topPerformers = await _performanceService.GetTopPerformersAsync(organizationId, count);

            return Ok(new ServiceResponse<List<TopPerformerDto>>
            {
                Success = true,
                Data = topPerformers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top performers");
            return StatusCode(500, new ServiceResponse<List<TopPerformerDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving top performers"
            });
        }
    }


    [HttpPost("create-selfassessment")]
    public async Task<ActionResult<ServiceResponse<SelfAssessmentDto>>> CreateSelfAssessment([FromBody] CreateSelfAssessmentRequest request)
    {
        if (request == null)
        {
            return BadRequest(new ServiceResponse<SelfAssessmentDto>
            {
                Success = false,
                Message = "Invalid self-assessment data"
            });
        }

        try
        {
            // Get current context
            var currentEmployeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();



            // Call service to create self-assessment
            var result = await _performanceService.CreateSelfAssessmentAsync(request, organizationId, currentEmployeeId);

            return Ok(new ServiceResponse<SelfAssessmentDto>
            {
                Success = true,
                Data = result,
                Message = "Self-assessment saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving self-assessment");

            return StatusCode(500, new ServiceResponse<SelfAssessmentDto>
            {
                Success = false,
                Message = "An error occurred while saving the self-assessment"
            });
        }
    }
    //mm

    [HttpGet("selfassessment/my")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetMySelfAssessments()
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            var result = await _performanceService.GetSelfAssessmentsByEmployeeAsync(currentEmployeeId, organizationId);

            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = result,
                Message = "Self-assessments retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving self-assessments");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving self-assessments"
            });
        }
    }

    //mm

    [HttpGet("selfassessment/team")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetTeamSelfAssessments()
    {
        try
        {
            // Get current manager/HR context
            var currentManagerId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            // Call service to get self-assessments of employees reporting to this manager
            var result = await _performanceService.GetSelfAssessmentsForManagerAsync(currentManagerId, organizationId);

            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = result,
                Message = "Team self-assessments retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team self-assessments");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving team self-assessments"
            });
        }
    }

    [HttpGet("SelfAssessment/{employeeId}/{cycleId}")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetSelfAssessment(Guid employeeId, Guid cycleId)
    {
        try
        {
            var assessments = await _performanceService.GetSelfAssessmentsAsync(employeeId, cycleId);
            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = assessments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching self-assessment");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while fetching self-assessment"
            });
        }
    }

    [HttpGet("SelfAssessment/my-assessments")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetMySelfAssessments(
        [FromQuery] Guid? cycleId = null,
        [FromQuery] Guid? kraId = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();
            var assessments = await _performanceService.GetMySelfAssessmentsAsync(currentEmployeeId, cycleId, kraId, search);
            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = assessments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my self-assessments");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while fetching self-assessments"
            });
        }
    }

    [HttpGet("appraisals/my-appraisals")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<EmployeeAppraisalForEmployeeDto>>>> GetMyAppraisals(
        [FromQuery] Guid? cycleId = null,
        [FromQuery] Guid? kraId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();

            // Check if employee ID is valid (not empty)
            if (currentEmployeeId == Guid.Empty)
            {
                _logger.LogWarning("User does not have an associated employee record");
                return Ok(new ServiceResponse<IEnumerable<EmployeeAppraisalForEmployeeDto>>
                {
                    Success = true,
                    Data = new List<EmployeeAppraisalForEmployeeDto>(),
                    Message = "No employee record found for this user"
                });
            }

            var appraisals = await _performanceService.GetEmployeeAppraisalsForEmployeeAsync(currentEmployeeId, cycleId, kraId, search, status);
            return Ok(new ServiceResponse<IEnumerable<EmployeeAppraisalForEmployeeDto>>
            {
                Success = true,
                Data = appraisals
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for my appraisals");
            return StatusCode(403, new ServiceResponse<IEnumerable<EmployeeAppraisalForEmployeeDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource. Please ensure you have an employee record associated with your account."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my appraisals");
            return StatusCode(500, new ServiceResponse<IEnumerable<EmployeeAppraisalForEmployeeDto>>
            {
                Success = false,
                Message = "An error occurred while fetching appraisals"
            });
        }
    }

    //[HttpPost("SelfAssessment/Submit")]
    //[Authorize(Roles = "Employee,Manager,HR Manager,Super Admin")]
    //public async Task<ActionResult<ServiceResponse<string>>> SubmitSelfAssessment([FromBody] CreateSelfAssessmentRequest request)
    //{
    //    try
    //    {
    //        var result = await _performanceService.SubmitSelfAssessmentAsync(request.EmployeeId, request.CycleId);

    //        return Ok(new ServiceResponse<string>
    //        {
    //            Success = true,
    //            Message = "Self-assessment submitted successfully",
    //            Data = result
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error submitting self-assessment");
    //        return StatusCode(500, new ServiceResponse<string>
    //        {
    //            Success = false,
    //            Message = "An error occurred while submitting self-assessment"
    //        });
    //    }
    //}

    [HttpGet("Manager/Self-Assessments/{managerId}/{cycleId}")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetTeamSelfAssessments(Guid managerId, Guid cycleId)
    {
        try
        {
            var data = await _performanceService.GetTeamSelfAssessmentsAsync(managerId, cycleId);
            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching team self-assessments");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while fetching team self-assessments"
            });
        }
    }

    [HttpGet("Manager/Self-Assessment/{employeeId}/{cycleId}")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetEmployeeSelfAssessment(Guid employeeId, Guid cycleId)
    {
        try
        {
            var data = await _performanceService.GetSelfAssessmentsAsync(employeeId, cycleId);
            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employee self-assessment");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while fetching employee self-assessment"
            });
        }
    }

    [HttpGet("SelfAssessment/all-employee-assessments")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetAllEmployeeSelfAssessments(
        [FromQuery] Guid? cycleId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? kraId = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var assessments = await _performanceService.GetAllEmployeeSelfAssessmentsAsync(organizationId, cycleId, employeeId, kraId, search);
            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = assessments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all employee self-assessments");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while fetching employee self-assessments"
            });
        }
    }

    [HttpGet("SelfAssessment/my-team-assessments")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<SelfAssessmentDto>>>> GetMyTeamSelfAssessments(
        [FromQuery] Guid? cycleId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? kraId = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();

            if (currentEmployeeId == Guid.Empty)
            {
                return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
                {
                    Success = true,
                    Data = new List<SelfAssessmentDto>(),
                    Message = "No employee record found for this user"
                });
            }

            var assessments = await _performanceService.GetMyTeamSelfAssessmentsAsync(currentEmployeeId, cycleId, employeeId, kraId, search);
            return Ok(new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = true,
                Data = assessments
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for my team self-assessments");
            return StatusCode(403, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my team self-assessments");
            return StatusCode(500, new ServiceResponse<IEnumerable<SelfAssessmentDto>>
            {
                Success = false,
                Message = "An error occurred while fetching team self-assessments"
            });
        }
    }

    // Appraisal Cycle Management - Enhanced
    [HttpGet("cycles/{cycleId}")]
    public async Task<ActionResult<ServiceResponse<AppraisalCycleDto>>> GetAppraisalCycleById(Guid cycleId)
    {
        try
        {
            var cycle = await _performanceService.GetAppraisalCycleByIdAsync(cycleId);
            if (cycle == null)
            {
                return NotFound(new ServiceResponse<AppraisalCycleDto>
                {
                    Success = false,
                    Message = "Appraisal cycle not found"
                });
            }

            return Ok(new ServiceResponse<AppraisalCycleDto>
            {
                Success = true,
                Data = cycle
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appraisal cycle {CycleId}", cycleId);
            return StatusCode(500, new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = "An error occurred while retrieving appraisal cycle"
            });
        }
    }

    [HttpPut("cycles/{cycleId}")]
    public async Task<ActionResult<ServiceResponse<AppraisalCycleDto>>> UpdateAppraisalCycle(Guid cycleId, [FromBody] UpdateAppraisalCycleRequest request)
    {
        try
        {
            var cycle = await _performanceService.UpdateAppraisalCycleAsync(cycleId, request);
            return Ok(new ServiceResponse<AppraisalCycleDto>
            {
                Success = true,
                Message = "Appraisal cycle updated successfully",
                Data = cycle
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = "Appraisal cycle not found"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error deleting appraisal cycle");
            return BadRequest(new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appraisal cycle {CycleId}", cycleId);
            return StatusCode(500, new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = "An error occurred while updating appraisal cycle"
            });
        }
    }


    [HttpGet("cycles/active")]
    public async Task<ActionResult<ServiceResponse<AppraisalCycleDto>>> GetActiveAppraisalCycle()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var cycle = await _performanceService.GetActiveAppraisalCycleAsync(organizationId);

            if (cycle == null)
            {
                return NotFound(new ServiceResponse<AppraisalCycleDto>
                {
                    Success = false,
                    Message = "No active appraisal cycle found"
                });
            }

            return Ok(new ServiceResponse<AppraisalCycleDto>
            {
                Success = true,
                Data = cycle
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active appraisal cycle");
            return StatusCode(500, new ServiceResponse<AppraisalCycleDto>
            {
                Success = false,
                Message = "An error occurred while retrieving active appraisal cycle"
            });
        }
    }

    // KRA Management - Enhanced
    [HttpDelete("Kra/{kraId}")]
    [PermissionAuthorize("KRA_DELETE_BUTTON")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteKRA(Guid kraId)
    {
        try
        {
            var result = await _performanceService.DeleteKRAAsync(kraId);
            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "KRA not found or could not be deleted"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "KRA deleted successfully",
                Data = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "KRA not found while deleting: {KRAId}", kraId);
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KRA {KRAId}", kraId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting KRA"
            });
        }
    }

    // Appraisal Consolidation
    //[HttpPost("appraisals/consolidate")]
    //[Authorize(Roles = "Super Admin,HR Manager,Manager")]
    //public async Task<ActionResult<ServiceResponse<EmployeeAppraisalDto>>> ConsolidateAppraisal([FromBody] ConsolidateAppraisalRequest request)
    //{
    //    try
    //    {
    //        var reviewerId = await GetCurrentEmployeeId();
    //        request.ReviewerId = reviewerId;

    //        var appraisal = await _performanceService.ConsolidateAppraisalAsync(request);
    //        return Ok(new ServiceResponse<EmployeeAppraisalDto>
    //        {
    //            Success = true,
    //            Message = "Appraisal consolidated successfully",
    //            Data = appraisal
    //        });
    //    }
    //    catch (InvalidOperationException ex)
    //    {
    //        return BadRequest(new ServiceResponse<EmployeeAppraisalDto>
    //        {
    //            Success = false,
    //            Message = ex.Message
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error consolidating appraisal");
    //        return StatusCode(500, new ServiceResponse<EmployeeAppraisalDto>
    //        {
    //            Success = false,
    //            Message = "An error occurred while consolidating appraisal"
    //        });
    //    }
    //}

    // Reports and Analytics
    [HttpGet("employee/{employeeId}/history")]
    public async Task<ActionResult<ServiceResponse<EmployeePerformanceHistoryDto>>> GetEmployeePerformanceHistory(Guid employeeId)
    {
        try
        {
            var currentEmployeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();

            // Employees can only view their own history
            var userRoles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            if (userRoles.Contains("Employee") && currentEmployeeId != employeeId)
            {
                return Forbid();
            }

            var history = await _performanceService.GetEmployeePerformanceHistoryAsync(employeeId);
            return Ok(new ServiceResponse<EmployeePerformanceHistoryDto>
            {
                Success = true,
                Data = history
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<EmployeePerformanceHistoryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance history for employee {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<EmployeePerformanceHistoryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving performance history"
            });
        }
    }

    [HttpGet("appraisals/{appraisalId}")]
    public async Task<ActionResult<ServiceResponse<EmployeeAppraisalDto>>> GetEmployeeAppraisalById(Guid appraisalId)
    {
        try
        {
            var appraisal = await _performanceService.GetEmployeeAppraisalByIdAsync(appraisalId);
            if (appraisal == null)
            {
                return NotFound(new ServiceResponse<EmployeeAppraisalDto>
                {
                    Success = false,
                    Message = "Appraisal not found"
                });
            }

            // Employees can only view their own appraisals
            var currentEmployeeId = await GetCurrentEmployeeId();
            var userRoles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            if (userRoles.Contains("Employee") && currentEmployeeId != appraisal.EmployeeId)
            {
                return Forbid();
            }

            return Ok(new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = true,
                Data = appraisal
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appraisal {AppraisalId}", appraisalId);
            return StatusCode(500, new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = false,
                Message = "An error occurred while retrieving appraisal"
            });
        }
    }

    [HttpPost("Manager/Review")]
    public async Task<ActionResult<ServiceResponse<ManagerReviewDto>>> SubmitManagerReview([FromBody] ManagerReviewRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var result = await _performanceService.SubmitManagerReviewAsync(request, organizationId, employeeId);


            return Ok(new ServiceResponse<ManagerReviewDto>
            {
                Success = true,
                Data = result,
                Message = "Manager review submitted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting manager review");
            return StatusCode(500, new ServiceResponse<ManagerReviewDto>
            {
                Success = false,
                Message = "An error occurred while submitting manager review"
            });
        }
    }

    // Get reviews created by manager (for their employees)
    [HttpGet("getall/created/Manager/Reviews")]
    [PermissionAuthorize("appraisal_manager_appraisals_review")]
    public async Task<ActionResult<ServiceResponse<List<ManagerReviewDto>>>> GetManagerCreatedReviews()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var managerId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetManagerReviewsAsync(organizationId, managerId);

            return Ok(new ServiceResponse<List<ManagerReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "Manager reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving manager reviews");
            return StatusCode(500, new ServiceResponse<List<ManagerReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving manager reviews"
            });
        }
    }

    //Getting all managers review from manager table for showing all managers review that they give to there employee
    [HttpGet("getAllManagers/Reviews")]
    public async Task<ActionResult<ServiceResponse<List<ManagerReviewDto>>>> GetAllManagerReviews([FromQuery] SearchManagersDto search)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var managerId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetAllManagerReviewsAsync(organizationId, managerId, search);

            return Ok(new ServiceResponse<List<ManagerReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "Manager reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving manager reviews");
            return StatusCode(500, new ServiceResponse<List<ManagerReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving manager reviews"
            });
        }
    }

    // Get reviews received by employee (from their manager)
    [HttpGet("employee/manager-reviews")]
    public async Task<ActionResult<ServiceResponse<List<ManagerReviewDto>>>> GetEmployeeReviews()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetEmployeeReviewsAsync(organizationId, employeeId);

            return Ok(new ServiceResponse<List<ManagerReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "Employee reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee reviews");
            return StatusCode(500, new ServiceResponse<List<ManagerReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee reviews"
            });
        }
    }

    // Submit HR Review
    [HttpPost("Create/HR/Appraisal")]
    public async Task<ActionResult<ServiceResponse<HrReviewDto>>> SubmitHrReview([FromBody] HrReviewRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId(); // HR employee ID

            var result = await _performanceService.SubmitHrReviewAsync(request, organizationId, currentEmployeeId);
            return Ok(new ServiceResponse<HrReviewDto>
            {
                Success = true,
                Data = result,
                Message = "HR review submitted successfully"
            });
        }

        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating appraisal cycle");
            return BadRequest(new ServiceResponse<HrReviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating appraisal cycle");
            return BadRequest(new ServiceResponse<HrReviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting HR review");
            return StatusCode(500, new ServiceResponse<HrReviewDto>
            {
                Success = false,
                Message = "An error occurred while submitting HR review"
            });
        }
    }

    // Get all HR reviews given to other employees  by this hr (for HR dashboard)
    [HttpGet("HR/Reviews")]
    public async Task<ActionResult<ServiceResponse<List<HrReviewDto>>>> GetHrReviews()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currenEmpid = await GetCurrentEmployeeId();

            var result = await _performanceService.GetHrReviewsAsync(organizationId, currenEmpid);

            return Ok(new ServiceResponse<List<HrReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "HR reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving HR reviews");
            return StatusCode(500, new ServiceResponse<List<HrReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving HR reviews"
            });
        }
    }

    // Get HR reviews for a specific employee
    [HttpGet("HR/Reviews/Employee/{employeeId}")]
    public async Task<ActionResult<ServiceResponse<List<HrReviewDto>>>> GetHrReviewsByEmployee(Guid employeeId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            var result = await _performanceService.GetHrReviewsByEmployeeAsync(organizationId, employeeId);

            return Ok(new ServiceResponse<List<HrReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "Employee HR reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee HR reviews");
            return StatusCode(500, new ServiceResponse<List<HrReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee HR reviews"
            });
        }
    }


    // Get employee's own HR reviews
    [HttpGet("Employee/HR/Reviews")]
    public async Task<ActionResult<ServiceResponse<List<HrReviewDto>>>> GetMyHrReviews()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetHrReviewsByEmployeeAsync(organizationId, employeeId);

            return Ok(new ServiceResponse<List<HrReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "Your HR reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee HR reviews");
            return StatusCode(500, new ServiceResponse<List<HrReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving your HR reviews"
            });
        }
    }


    //Get all records of Hr.appraisals mm
    [HttpGet("GetAllHr/Appraisals")]
    public async Task<ActionResult<ServiceResponse<List<HrReviewDto>>>> GetallAppraisal([FromQuery] SearchAppraisalDto? search)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();

            var result = await _performanceService.GetAllAppraisaAsync(organizationId, employeeId, search);

            return Ok(new ServiceResponse<List<HrReviewDto>>
            {
                Success = true,
                Data = result,
                Message = "Your HR reviews retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee HR reviews");
            return StatusCode(500, new ServiceResponse<List<HrReviewDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving your HR reviews"
            });
        }
    }




    [HttpDelete("appraisals/{appraisalId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteEmployeeAppraisal(Guid appraisalId)
    {
        try
        {
            var result = await _performanceService.DeleteEmployeeAppraisalAsync(appraisalId);
            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Appraisal not found or could not be deleted"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Appraisal deleted successfully",
                Data = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Appraisal not found while deleting: {AppraisalId}", appraisalId);
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appraisal {AppraisalId}", appraisalId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting appraisal"
            });
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
        var employeeId = await _performanceService.GetEmployeeFromTable(userId);
        return employeeId;
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
}

public class CreateAppraisalCycleRequest
{
    public string CycleName { get; set; } = string.Empty;
    public string? CycleType { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public DateTime? ReviewStartDate { get; set; }
    public DateTime? ReviewEndDate { get; set; }

    public string? Status { get; set; } // upcoming, inprogress, concluded

    public bool IsSelfAssessmentEnable { get; set; } = true;
    public bool ManagerReview { get; set; } = true;   // renamed from IsReviewEnable
    public bool IsAppraisalEnable { get; set; }

    public string? Description { get; set; }
}