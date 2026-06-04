using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet("organization")]
    public async Task<ActionResult<ServiceResponse<OrganizationSettingsDto>>> GetOrganizationSettings()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var settings = await _settingsService.GetOrganizationSettingsAsync(organizationId);
            
            if (!settings.Success)
            {
                return BadRequest(settings);
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization settings");
            return StatusCode(500, new ServiceResponse<OrganizationSettingsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving organization settings"
            });
        }
    }

    [HttpPut("currency")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateCurrency([FromBody] UpdateCurrencyRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _settingsService.UpdateCurrencyAsync(organizationId, request.Currency);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating currency"
            });
        }
    }


    [HttpPut("localization")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateLocalization(
    [FromBody] UpdateLocalizationSettingsRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            var result = await _settingsService.UpdateLocalizationAsync(
                organizationId,
                request.Currency,
                request.TimeZone,
                request.Culture
            );

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating localization settings");

            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating localization settings"
            });
        }
    }

    [HttpPut("career-page")]
    [PermissionAuthorize("career_management_edit")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateCareerPage([FromBody] UpdateCareerPageRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _settingsService.UpdateCareerPageAsync(organizationId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating career page settings");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating career page settings"
            });
        }
    }

    [HttpGet("career-page")]
    [PermissionAuthorize("career_management_view")]
    public async Task<ActionResult<ServiceResponse<CareerPageDto>>> GetCareerPage()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _settingsService.GetCareerPageAsync(organizationId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving career page details for organization");
            return StatusCode(500, new ServiceResponse<CareerPageDto>
            {
                Success = false,
                Message = "An error occurred while retrieving career page details"
            });
        }
    }

    [HttpGet("policies")]
    public async Task<ActionResult<ServiceResponse<List<CompanyPolicyDto>>>> GetPolicies()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _settingsService.GetPoliciesAsync(organizationId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company policies");
            return StatusCode(500, new ServiceResponse<List<CompanyPolicyDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving company policies"
            });
        }
    }

    [HttpGet("policies/{policyId}")]
    public async Task<ActionResult<ServiceResponse<CompanyPolicyDto>>> GetPolicyById(Guid policyId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _settingsService.GetPolicyByIdAsync(organizationId, policyId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving policy details");
            return StatusCode(500, new ServiceResponse<CompanyPolicyDto>
            {
                Success = false,
                Message = "An error occurred while retrieving policy details"
            });
        }
    }

    [HttpPost("policies")]
    [PermissionAuthorize("Create_Policy")]

    public async Task<ActionResult<ServiceResponse<CompanyPolicyDto>>> CreatePolicy([FromBody] CreateCompanyPolicyRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var createdBy = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "System";
            var result = await _settingsService.CreatePolicyAsync(organizationId, request, createdBy);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company policy");
            return StatusCode(500, new ServiceResponse<CompanyPolicyDto>
            {
                Success = false,
                Message = "An error occurred while creating company policy"
            });
        }
    }

    [HttpPut("policies/{policyId}")]
    public async Task<ActionResult<ServiceResponse<CompanyPolicyDto>>> UpdatePolicy(Guid policyId, [FromBody] UpdateCompanyPolicyRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var updatedBy = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "System";
            var result = await _settingsService.UpdatePolicyAsync(organizationId, policyId, request, updatedBy);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company policy");
            return StatusCode(500, new ServiceResponse<CompanyPolicyDto>
            {
                Success = false,
                Message = "An error occurred while updating company policy"
            });
        }
    }

    [HttpDelete("policies/{policyId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeletePolicy(Guid policyId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _settingsService.DeletePolicyAsync(organizationId, policyId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company policy");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting company policy"
            });
        }
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
