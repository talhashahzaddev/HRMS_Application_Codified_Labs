// OnboardingConfigController.cs
using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/onboarding-config")]
[Authorize]
public class OnboardingConfigController : ControllerBase
{
    private readonly IOnboardingConfigService _service;
    private readonly ILogger<OnboardingConfigController> _logger;

    public OnboardingConfigController(
        IOnboardingConfigService service,
        ILogger<OnboardingConfigController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET /api/onboarding-config
    [HttpGet("getOrganizationConfiguration")]
    [PermissionAuthorize("onboarding_config_view")]
    public async Task<IActionResult> GetMyOrgConfig()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var config = await _service.GetConfigAsync(orgId);
            return Ok(new { success = true, data = config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching onboarding config");
            return StatusCode(500, new { success = false, message = "Failed to fetch configuration." });
        }
    }

    [HttpPut("update/onboardingConfiguration")]
    [PermissionAuthorize("onboarding_config_edit")]
    public async Task<IActionResult> UpdateOnBoardingConfiguration([FromBody] UpdateOnBoardingConfigurationRequest request)
    {
        try
        {
            if (request == null || request.Fields == null || !request.Fields.Any())
                return BadRequest(new { success = false, message = "No configuration fields provided." });
            var orgId = GetCurrentOrganizationId();
            var success = await _service.UpdateConfigAsync(orgId, request);

            return Ok(new
            {
                success,
                message = "Onboarding configuration saved successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to save configuration."
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var claim = User.FindFirstValue("organizationId");
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Organization ID not found in token.");
        return id;
    }
}