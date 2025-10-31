using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPerformanceService performanceService, ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService;
        _logger = logger;
    }

    [HttpGet("cycles")]
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

    [HttpPost("cycles")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<AppraisalCycleDto>>> CreateAppraisalCycle([FromBody] CreateAppraisalCycleRequest request)
    {
        try
        {
            //i change this function time stamp problem performance.fn_create_appraisal_cycle
            var organizationId = GetCurrentOrganizationId();
            var cycle = await _performanceService.CreateAppraisalCycleAsync(
                request.CycleName, request.StartDate, request.EndDate, organizationId, request.Description);

            return Ok(new ServiceResponse<AppraisalCycleDto>
            {
                Success = true,
                Message = "Appraisal cycle created successfully",
                Data = cycle
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

    [HttpGet("cycles/{cycleId}/appraisals")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeAppraisalDto>>>> GetEmployeeAppraisals(Guid cycleId, [FromQuery] Guid? employeeId = null)
    {
        try
        {
            var appraisals = await _performanceService.GetEmployeeAppraisalsAsync(cycleId, employeeId);

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

    [HttpPost("appraisals")]
    public async Task<ActionResult<ServiceResponse<EmployeeAppraisalDto>>> CreateEmployeeAppraisal([FromBody] CreateAppraisalRequest request)
    {
        try
        {
            var reviewerId = GetCurrentEmployeeId();
            var appraisal = await _performanceService.CreateEmployeeAppraisalAsync(request, reviewerId);

            return Ok(new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = true,
                Message = "Employee appraisal created successfully",
                Data = appraisal
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee appraisal");
            return StatusCode(500, new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = false,
                Message = "An error occurred while creating employee appraisal"
            });
        }
    }

    [HttpPost("skills")]
    public async Task<ActionResult<ServiceResponse<SkillSetDto>>> CreateSkillSet([FromBody] CreateSkillSetRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var skill = await _performanceService.CreateSkillSetAsync(request, organizationId);

            return Ok(new ServiceResponse<SkillSetDto>
            {
                Success = true,
                Message = "Skill created successfully",
                Data = skill
            });
        }
        catch (InvalidOperationException ex)
        {
            // Unique constraint violation handled here
            return Conflict(new ServiceResponse<SkillSetDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill set");
            return StatusCode(500, new ServiceResponse<SkillSetDto>
            {
                Success = false,
                Message = "An unexpected error occurred while creating the skill"
            });
        }
    }

    [HttpGet("skills")]
    public async Task<ActionResult<ServiceResponse<List<SkillSetDto>>>> GetSkillSets()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var skills = await _performanceService.GetSkillSetsAsync(organizationId);

            return Ok(new ServiceResponse<List<SkillSetDto>>
            {
                Success = true,
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skill sets");
            return StatusCode(500, new ServiceResponse<List<SkillSetDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving skill sets"
            });
        }
    }

    [HttpPost("create-Employee-skills")]
    public async Task<IActionResult> CreateEmployeeSkill([FromBody] CreateEmployeeSkillRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _performanceService.CreateEmployeeSkillAsync(request);

            if (result == null)
                return StatusCode(500, new { success = false, message = "Failed to add employee skill." });

            return Ok(new
            {
                success = true,
                message = "Employee skill added successfully.",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating employee skill");
            return StatusCode(500, new
            {
                success = false,
                message = "An unexpected error occurred while creating the employee skill."
            });
        }
    }

    [HttpGet("employees/{employeeId}/skills")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeSkillDto>>>> GetEmployeeSkills(Guid employeeId)
    {
        try
        {
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

    [HttpGet("summary")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<PerformanceSummaryDto>>> GetPerformanceSummary([FromQuery] Guid? cycleId = null)
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



    [HttpPost("Goals")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<EmployeeGoalDto>>> CreateGoal([FromBody] CreateEmployeeGoalRequest goal)
    {
        try
        {
            if (goal == null)
                return BadRequest("Invalid goal data");
            var goalId = await _performanceService.CreateEmployeeGoalAsync(goal);
            return Ok(new ServiceResponse<EmployeeGoalDto>
            {
                Success = true,
                Data = goalId
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Creating Goals");
            return StatusCode(500, new ServiceResponse<EmployeeGoalDto>
            {
                Success = false,
                Message = "An error occurred while Creating Goals"
            });

        }


    }


    [HttpGet("Goals/Employee/{employeeId}")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeGoalDto>>>> GetGoalsByEmployee(Guid employeeId)
    {
        try
        {
            var goals = await _performanceService.GetEmployeeGoalsAsync(employeeId);
            return Ok(new ServiceResponse<List<EmployeeGoalDto>>
            {
                Success = true,
                Data = goals
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching goals for employee {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<List<EmployeeGoalDto>>
            {
                Success = false,
                Message = "An error occurred while fetching goals"
            });
        }
    }


    [HttpGet("Goals")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeGoalDto>>>> GetAllGoals()
    {
        try
        {
            var goals = await _performanceService.GetAllEmployeeGoalsAsync();
            return Ok(new ServiceResponse<List<EmployeeGoalDto>> { Success = true, Data = goals });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all goals");
            return StatusCode(500, new ServiceResponse<List<EmployeeGoalDto>>
            {
                Success = false,
                Message = "An error occurred while fetching goals"
            });
        }
    }

    //Complete Code 

    [HttpPut("Goals/{goalId}/complete")]
    public async Task<ActionResult<ServiceResponse<bool>>> MarkGoalAsCompleted(Guid goalId)
    {
        try
        {
            var result = await _performanceService.GoalAsCompletedAsync(goalId);

            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Goal not found or could not be updated"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Goal marked as completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking goal {GoalId} as completed", goalId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating goal status"
            });
        }
    }



    [HttpPost("Kra")]

    public async Task<ActionResult<ServiceResponse<KRADto>>> CreateKra([FromQuery] CreateKRARequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _performanceService.CreateKRAAsync(organizationId, request);

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



    [HttpGet("Kra")]

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error While Getting all KRas");
            return StatusCode(500, new ServiceResponse<List<TopPerformerDto>>
            {
                Success = false,
                Message = "Error Getting KRass"
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

            return Ok(new ServiceResponse<KRADto>
            {
                Success = true,
                Data = result,
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



    [HttpGet("top-performers")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
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

    [HttpPost("SelfAssessment")]
    [Authorize(Roles = "Employee,Manager,HR Manager")]
    public async Task<ActionResult<ServiceResponse<SelfAssessmentDto>>> CreateSelfAssessment([FromBody] CreateSelfAssessmentRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Invalid self-assessment data");

            var result = await _performanceService.CreateSelfAssessmentAsync(request);

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
                Message = "An error occurred while saving self-assessment"
            });
        }
    }

    [HttpGet("SelfAssessment/{employeeId}/{cycleId}")]
    [Authorize(Roles = "Employee,Manager,HR Manager")]
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

    [HttpPost("SelfAssessment/Submit")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<ServiceResponse<string>>> SubmitSelfAssessment([FromBody] CreateSelfAssessmentRequest request)
    {
        try
        {
            var result = await _performanceService.SubmitSelfAssessmentAsync(request.EmployeeId, request.CycleId);

            return Ok(new ServiceResponse<string>
            {
                Success = true,
                Message = "Self-assessment submitted successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting self-assessment");
            return StatusCode(500, new ServiceResponse<string>
            {
                Success = false,
                Message = "An error occurred while submitting self-assessment"
            });
        }
    }

    [HttpGet("Manager/Self-Assessments/{managerId}/{cycleId}")]
    [Authorize(Roles = "Manager,HR Manager")]
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
    [Authorize(Roles = "Manager,HR Manager")]
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

    [HttpPost("Manager/Review")]
    [Authorize(Roles = "Manager,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ManagerReviewDto>>> SubmitManagerReview([FromBody] ManagerReviewRequest request)
    {
        try
        {
            var result = await _performanceService.SubmitManagerReviewAsync(request);
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

    private Guid GetCurrentEmployeeId()
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
}

public class CreateAppraisalCycleRequest
{
    public string CycleName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
}
