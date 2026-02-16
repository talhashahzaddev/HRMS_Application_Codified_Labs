using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobsService _jobsService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IJobsService jobsService,
        IAttendanceService attendanceService,
        ILogger<JobsController> logger)
    {
        _jobsService = jobsService;
        _attendanceService = attendanceService;
        _logger = logger;
    }

    // ==================== Stage Master ====================
    [HttpGet("stages")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<List<StageMasterDto>>>> GetAllStages()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stages = await _jobsService.GetAllStageMastersAsync(organizationId);
            return Ok(new ServiceResponse<List<StageMasterDto>>
            {
                Success = true,
                Data = stages?.ToList() ?? new List<StageMasterDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting stages");
            return Unauthorized(new ServiceResponse<List<StageMasterDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stages");
            return StatusCode(500, new ServiceResponse<List<StageMasterDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving stages"
            });
        }
    }

    [HttpGet("stages/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<StageMasterDto>>> GetStageById(Guid id)
    {
        try
        {
            var stage = await _jobsService.GetStageMasterByIdAsync(id);
            if (stage == null)
            {
                return NotFound(new ServiceResponse<StageMasterDto>
                {
                    Success = false,
                    Message = "Stage not found"
                });
            }
            return Ok(new ServiceResponse<StageMasterDto>
            {
                Success = true,
                Data = stage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stage {StageId}", id);
            return StatusCode(500, new ServiceResponse<StageMasterDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the stage"
            });
        }
    }

    [HttpPost("stages")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<StageMasterDto>>> CreateStage([FromBody] CreateStageMasterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<StageMasterDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var stage = await _jobsService.CreateStageMasterAsync(organizationId, request);
            return CreatedAtAction(nameof(GetStageById), new { id = stage.StageId }, new ServiceResponse<StageMasterDto>
            {
                Success = true,
                Message = "Stage created successfully",
                Data = stage
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating stage");
            return Unauthorized(new ServiceResponse<StageMasterDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating stage");
            return BadRequest(new ServiceResponse<StageMasterDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating stage");
            return BadRequest(new ServiceResponse<StageMasterDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stage");
            return StatusCode(500, new ServiceResponse<StageMasterDto>
            {
                Success = false,
                Message = "An error occurred while creating stage"
            });
        }
    }

    [HttpPut("stages/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<StageMasterDto>>> UpdateStage(Guid id, [FromBody] UpdateStageMasterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<StageMasterDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var stage = await _jobsService.UpdateStageMasterAsync(id, request);
            if (stage == null)
            {
                return NotFound(new ServiceResponse<StageMasterDto>
                {
                    Success = false,
                    Message = "Stage not found"
                });
            }
            return Ok(new ServiceResponse<StageMasterDto>
            {
                Success = true,
                Message = "Stage updated successfully",
                Data = stage
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating stage");
            return BadRequest(new ServiceResponse<StageMasterDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating stage");
            return BadRequest(new ServiceResponse<StageMasterDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stage {StageId}", id);
            return StatusCode(500, new ServiceResponse<StageMasterDto>
            {
                Success = false,
                Message = "An error occurred while updating stage"
            });
        }
    }

    [HttpDelete("stages/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteStage(Guid id)
    {
        try
        {
            var deleted = await _jobsService.DeleteStageMasterAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Stage not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Stage deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stage {StageId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting stage"
            });
        }
    }

    // ==================== Job Openings ====================
    [HttpGet("openings")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<PagedResult<JobOpeningDto>>>> GetAllJobOpenings(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? lastDateFrom,
        [FromQuery] string? lastDateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            // Normalize empty/invalid date params to null so repository skips those filters
            DateTime? from = DateTime.TryParse(lastDateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(lastDateTo, out var parsedTo) ? parsedTo : null;
            var organizationId = GetCurrentOrganizationId();
            var result = await _jobsService.GetJobOpeningsPagedAsync(organizationId, search, status, from, to, pageNumber, pageSize);
            return Ok(new ServiceResponse<PagedResult<JobOpeningDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting job openings");
            return Unauthorized(new ServiceResponse<PagedResult<JobOpeningDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job openings");
            return StatusCode(500, new ServiceResponse<PagedResult<JobOpeningDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving job openings"
            });
        }
    }

    [HttpGet("openings/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<JobOpeningDto>>> GetJobOpeningById(Guid id)
    {
        try
        {
            var opening = await _jobsService.GetJobOpeningByIdAsync(id);
            if (opening == null)
            {
                return NotFound(new ServiceResponse<JobOpeningDto>
                {
                    Success = false,
                    Message = "Job opening not found"
                });
            }
            return Ok(new ServiceResponse<JobOpeningDto>
            {
                Success = true,
                Data = opening
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job opening {JobId}", id);
            return StatusCode(500, new ServiceResponse<JobOpeningDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the job opening"
            });
        }
    }

    [HttpPost("openings")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<JobOpeningDto>>> CreateJobOpening([FromBody] CreateJobOpeningRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<JobOpeningDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var createdBy = await GetCurrentEmployeeId();
            var opening = await _jobsService.CreateJobOpeningAsync(organizationId, createdBy, request);
            return CreatedAtAction(nameof(GetJobOpeningById), new { id = opening.JobId }, new ServiceResponse<JobOpeningDto>
            {
                Success = true,
                Message = "Job opening created successfully",
                Data = opening
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating job opening");
            return Unauthorized(new ServiceResponse<JobOpeningDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating job opening");
            return BadRequest(new ServiceResponse<JobOpeningDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating job opening");
            return BadRequest(new ServiceResponse<JobOpeningDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job opening");
            return StatusCode(500, new ServiceResponse<JobOpeningDto>
            {
                Success = false,
                Message = "An error occurred while creating job opening"
            });
        }
    }

    [HttpPut("openings/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<JobOpeningDto>>> UpdateJobOpening(Guid id, [FromBody] UpdateJobOpeningRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<JobOpeningDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var opening = await _jobsService.UpdateJobOpeningAsync(id, request);
            if (opening == null)
            {
                return NotFound(new ServiceResponse<JobOpeningDto>
                {
                    Success = false,
                    Message = "Job opening not found"
                });
            }
            return Ok(new ServiceResponse<JobOpeningDto>
            {
                Success = true,
                Message = "Job opening updated successfully",
                Data = opening
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating job opening");
            return BadRequest(new ServiceResponse<JobOpeningDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job opening {JobId}", id);
            return StatusCode(500, new ServiceResponse<JobOpeningDto>
            {
                Success = false,
                Message = "An error occurred while updating job opening"
            });
        }
    }

    [HttpDelete("openings/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteJobOpening(Guid id)
    {
        try
        {
            var deleted = await _jobsService.DeleteJobOpeningAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Job opening not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Job opening deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job opening {JobId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting job opening"
            });
        }
    }

    // ==================== Job Applications ====================
    [HttpGet("applications")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<List<JobApplicationDto>>>> GetAllJobApplications()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var applications = await _jobsService.GetAllJobApplicationsAsync(organizationId);
            return Ok(new ServiceResponse<List<JobApplicationDto>>
            {
                Success = true,
                Data = applications?.ToList() ?? new List<JobApplicationDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting job applications");
            return Unauthorized(new ServiceResponse<List<JobApplicationDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job applications");
            return StatusCode(500, new ServiceResponse<List<JobApplicationDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving job applications"
            });
        }
    }

    [HttpGet("applications/by-job/{jobId}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<List<JobApplicationDto>>>> GetJobApplicationsByJobId(Guid jobId)
    {
        try
        {
            var applications = await _jobsService.GetJobApplicationsByJobIdAsync(jobId);
            return Ok(new ServiceResponse<List<JobApplicationDto>>
            {
                Success = true,
                Data = applications?.ToList() ?? new List<JobApplicationDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting job applications by job");
            return Unauthorized(new ServiceResponse<List<JobApplicationDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job applications for job {JobId}", jobId);
            return StatusCode(500, new ServiceResponse<List<JobApplicationDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving job applications"
            });
        }
    }

    [HttpGet("applications/me")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<List<JobApplicationDto>>>> GetMyJobApplications()
    {
        try
        {
            var employeeId = await GetCurrentEmployeeId();
            var applications = await _jobsService.GetJobApplicationsByEmployeeIdAsync(employeeId);
            return Ok(new ServiceResponse<List<JobApplicationDto>>
            {
                Success = true,
                Data = applications?.ToList() ?? new List<JobApplicationDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting my job applications");
            return Unauthorized(new ServiceResponse<List<JobApplicationDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my job applications");
            return StatusCode(500, new ServiceResponse<List<JobApplicationDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving job applications"
            });
        }
    }

    [HttpGet("applications/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<JobApplicationDto>>> GetJobApplicationById(Guid id)
    {
        try
        {
            var application = await _jobsService.GetJobApplicationByIdAsync(id);
            if (application == null)
            {
                return NotFound(new ServiceResponse<JobApplicationDto>
                {
                    Success = false,
                    Message = "Job application not found"
                });
            }
            return Ok(new ServiceResponse<JobApplicationDto>
            {
                Success = true,
                Data = application
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job application {JobApplyId}", id);
            return StatusCode(500, new ServiceResponse<JobApplicationDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the job application"
            });
        }
    }

    [HttpPost("applications")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<JobApplicationDto>>> CreateJobApplication([FromBody] CreateJobApplicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<JobApplicationDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var application = await _jobsService.CreateJobApplicationAsync(organizationId, employeeId, request);
            return CreatedAtAction(nameof(GetJobApplicationById), new { id = application.JobApplyId }, new ServiceResponse<JobApplicationDto>
            {
                Success = true,
                Message = "Job application created successfully",
                Data = application
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating job application");
            return Unauthorized(new ServiceResponse<JobApplicationDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating job application");
            return BadRequest(new ServiceResponse<JobApplicationDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating job application");
            return BadRequest(new ServiceResponse<JobApplicationDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job application");
            return StatusCode(500, new ServiceResponse<JobApplicationDto>
            {
                Success = false,
                Message = "An error occurred while creating job application"
            });
        }
    }

    [HttpPut("applications/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<JobApplicationDto>>> UpdateJobApplication(Guid id, [FromBody] UpdateJobApplicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<JobApplicationDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var application = await _jobsService.UpdateJobApplicationAsync(id, request);
            if (application == null)
            {
                return NotFound(new ServiceResponse<JobApplicationDto>
                {
                    Success = false,
                    Message = "Job application not found"
                });
            }
            return Ok(new ServiceResponse<JobApplicationDto>
            {
                Success = true,
                Message = "Job application updated successfully",
                Data = application
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating job application");
            return BadRequest(new ServiceResponse<JobApplicationDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job application {JobApplyId}", id);
            return StatusCode(500, new ServiceResponse<JobApplicationDto>
            {
                Success = false,
                Message = "An error occurred while updating job application"
            });
        }
    }

    [HttpDelete("applications/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteJobApplication(Guid id)
    {
        try
        {
            var deleted = await _jobsService.DeleteJobApplicationAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Job application not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Job application deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job application {JobApplyId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting job application"
            });
        }
    }

    // ==================== Application Stages ====================
    [HttpGet("application-stages")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<List<ApplicationStageDto>>>> GetAllApplicationStages()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stages = await _jobsService.GetAllApplicationStagesAsync(organizationId);
            return Ok(new ServiceResponse<List<ApplicationStageDto>>
            {
                Success = true,
                Data = stages?.ToList() ?? new List<ApplicationStageDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting application stages");
            return Unauthorized(new ServiceResponse<List<ApplicationStageDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application stages");
            return StatusCode(500, new ServiceResponse<List<ApplicationStageDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving application stages"
            });
        }
    }

    [HttpGet("application-stages/by-application/{jobApplyId}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<List<ApplicationStageDto>>>> GetApplicationStagesByJobApplyId(Guid jobApplyId)
    {
        try
        {
            var stages = await _jobsService.GetApplicationStagesByJobApplyIdAsync(jobApplyId);
            return Ok(new ServiceResponse<List<ApplicationStageDto>>
            {
                Success = true,
                Data = stages?.ToList() ?? new List<ApplicationStageDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting application stages by job apply");
            return Unauthorized(new ServiceResponse<List<ApplicationStageDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application stages for job apply {JobApplyId}", jobApplyId);
            return StatusCode(500, new ServiceResponse<List<ApplicationStageDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving application stages"
            });
        }
    }

    [HttpGet("application-stages/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<ApplicationStageDto>>> GetApplicationStageById(Guid id)
    {
        try
        {
            var stage = await _jobsService.GetApplicationStageByIdAsync(id);
            if (stage == null)
            {
                return NotFound(new ServiceResponse<ApplicationStageDto>
                {
                    Success = false,
                    Message = "Application stage not found"
                });
            }
            return Ok(new ServiceResponse<ApplicationStageDto>
            {
                Success = true,
                Data = stage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application stage {ApplicationStageId}", id);
            return StatusCode(500, new ServiceResponse<ApplicationStageDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the application stage"
            });
        }
    }

    [HttpPost("application-stages")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<ApplicationStageDto>>> CreateApplicationStage([FromBody] CreateApplicationStageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<ApplicationStageDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var updatedBy = await GetCurrentEmployeeId();
            var stage = await _jobsService.CreateApplicationStageAsync(updatedBy, request);
            return CreatedAtAction(nameof(GetApplicationStageById), new { id = stage.ApplicationStageId }, new ServiceResponse<ApplicationStageDto>
            {
                Success = true,
                Message = "Application stage created successfully. Job application status updated to stage name.",
                Data = stage
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating application stage");
            return Unauthorized(new ServiceResponse<ApplicationStageDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating application stage");
            return BadRequest(new ServiceResponse<ApplicationStageDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating application stage");
            return BadRequest(new ServiceResponse<ApplicationStageDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application stage");
            return StatusCode(500, new ServiceResponse<ApplicationStageDto>
            {
                Success = false,
                Message = "An error occurred while creating application stage"
            });
        }
    }

    [HttpPut("application-stages/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<ApplicationStageDto>>> UpdateApplicationStage(Guid id, [FromBody] UpdateApplicationStageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<ApplicationStageDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var updatedBy = await GetCurrentEmployeeId();
            var stage = await _jobsService.UpdateApplicationStageAsync(id, updatedBy, request);
            if (stage == null)
            {
                return NotFound(new ServiceResponse<ApplicationStageDto>
                {
                    Success = false,
                    Message = "Application stage not found"
                });
            }
            return Ok(new ServiceResponse<ApplicationStageDto>
            {
                Success = true,
                Message = "Application stage updated successfully",
                Data = stage
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating application stage");
            return BadRequest(new ServiceResponse<ApplicationStageDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application stage {ApplicationStageId}", id);
            return StatusCode(500, new ServiceResponse<ApplicationStageDto>
            {
                Success = false,
                Message = "An error occurred while updating application stage"
            });
        }
    }

    [HttpDelete("application-stages/{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteApplicationStage(Guid id)
    {
        try
        {
            var deleted = await _jobsService.DeleteApplicationStageAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Application stage not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Application stage deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application stage {ApplicationStageId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting application stage"
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
        var employeeId = await _attendanceService.GetEmployeeFromTable(userId);
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
