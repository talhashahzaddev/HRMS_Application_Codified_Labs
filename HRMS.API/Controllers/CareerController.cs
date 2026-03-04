using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class CareerController : ControllerBase
{
    private readonly IJobsService _jobsService;
    private readonly IOrganizationService _organizationService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<CareerController> _logger;

    public CareerController(
        IJobsService jobsService,
        IOrganizationService organizationService,
        ISettingsService settingsService,
        ILogger<CareerController> logger)
    {
        _jobsService = jobsService;
        _organizationService = organizationService;
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet("openings")]
    public async Task<ActionResult<ServiceResponse<PagedResult<JobOpeningDto>>>> GetExternalJobOpenings(
        [FromQuery] string domain,
        [FromQuery] string? search,
        [FromQuery] string? status = "Open",
        [FromQuery] string? lastDateFrom = null,
        [FromQuery] string? lastDateTo = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return BadRequest(new ServiceResponse<PagedResult<JobOpeningDto>>
                {
                    Success = false,
                    Message = "Domain name is required"
                });
            }

            // Get organization ID by domain
            var orgResult = await _organizationService.GetOrganizationByDomainAsync(domain);
            if (!orgResult.Success || orgResult.Data == null)
            {
                return NotFound(new ServiceResponse<PagedResult<JobOpeningDto>>
                {
                    Success = false,
                    Message = "Organization not found for the provided domain"
                });
            }

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            
            DateTime? from = DateTime.TryParse(lastDateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(lastDateTo, out var parsedTo) ? parsedTo : null;

            var result = await _jobsService.GetExternalJobOpeningsPagedAsync(orgResult.Data.OrganizationId, search, status, from, to, pageNumber, pageSize);
            
            return Ok(new ServiceResponse<PagedResult<JobOpeningDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving external job openings for domain {Domain}", domain);
            return StatusCode(500, new ServiceResponse<PagedResult<JobOpeningDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving job openings"
            });
        }
    }

    [HttpGet("companycareerdetails")]
    public async Task<ActionResult<ServiceResponse<CareerPageDto>>> GetCompanyCareerDetails([FromQuery] string domain)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return BadRequest(new ServiceResponse<CareerPageDto>
                {
                    Success = false,
                    Message = "Domain name is required"
                });
            }

            // Get organization ID by domain
            var orgResult = await _organizationService.GetOrganizationByDomainAsync(domain);
            if (!orgResult.Success || orgResult.Data == null)
            {
                return NotFound(new ServiceResponse<CareerPageDto>
                {
                    Success = false,
                    Message = "Organization not found for the provided domain"
                });
            }

            var result = await _settingsService.GetCareerPageAsync(orgResult.Data.OrganizationId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving career page details for domain {Domain}", domain);
            return StatusCode(500, new ServiceResponse<CareerPageDto>
            {
                Success = false,
                Message = "An error occurred while retrieving career page details"
            });
        }
    }
}
