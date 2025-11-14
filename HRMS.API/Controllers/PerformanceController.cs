using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using System.Threading.Tasks;

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

    [HttpPost("appraisals")]
    public async Task<ActionResult<ServiceResponse<EmployeeAppraisalDto>>> CreateEmployeeAppraisal([FromBody] CreateAppraisalRequest request)
    {
        try
        {
            var reviewerId = await GetCurrentEmployeeId();
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
            // Get current logged-in user ID
            var currentUserId = await GetCurrentEmployeeId();
            
            // If AssessedBy is not provided, set it to current user ID (the person assessing)
            // If it's self-assessment (employeeId == currentUserId), it will already be set correctly from frontend
            // If it's someone else assessing, AssessedBy should be the current logged-in user
            if (request.AssessedBy == request.EmployeeId)
            {
                // If employee is assessing themselves, use their own ID
                // Otherwise, use the current logged-in user ID (HR/Manager assessing)
                request.AssessedBy = request.EmployeeId = currentUserId;
            }
            
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

    // Appraisal Cycle Management - Enhanced
    [HttpGet("cycles/{cycleId}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
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
    [Authorize(Roles = "Super Admin,HR Manager")]
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

    [HttpDelete("cycles/{cycleId}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAppraisalCycle(Guid cycleId)
    {
        try
        {
            var result = await _performanceService.DeleteAppraisalCycleAsync(cycleId);
            if (!result)
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
            _logger.LogError(ex, "Error deleting appraisal cycle {CycleId}", cycleId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting appraisal cycle"
            });
        }
    }

    [HttpGet("cycles/active")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
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
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
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
    [HttpPost("appraisals/consolidate")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<EmployeeAppraisalDto>>> ConsolidateAppraisal([FromBody] ConsolidateAppraisalRequest request)
    {
        try
        {
            var reviewerId = await GetCurrentEmployeeId();
            request.ReviewerId = reviewerId;
            
            var appraisal = await _performanceService.ConsolidateAppraisalAsync(request);
            return Ok(new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = true,
                Message = "Appraisal consolidated successfully",
                Data = appraisal
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating appraisal");
            return StatusCode(500, new ServiceResponse<EmployeeAppraisalDto>
            {
                Success = false,
                Message = "An error occurred while consolidating appraisal"
            });
        }
    }

    // Reports and Analytics
    [HttpGet("employee/{employeeId}/history")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
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
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
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
        var employeeId=await _performanceService.GetEmployeeFromTable(userId);
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
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
}
