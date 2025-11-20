using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
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
    [Authorize(Roles = "Super Admin")]
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
    [Authorize(Roles = "Super Admin")]
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








