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
