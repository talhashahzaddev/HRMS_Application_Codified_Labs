// OnboardingConfigController.cs
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

    // GET /api/onboarding-config                  → used by onboarding form (employee)
    // GET /api/onboarding-config/org/{orgId}      → used by super admin UI
    // PUT /api/onboarding-config/org/{orgId}      → save config (super admin only)

    /// <summary>
    /// Employee calls this to get the field config for THEIR org.
    /// Reads organizationId from the JWT token.
    /// </summary>
    [HttpGet]
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

    /// <summary>
    ///Getting organization configurations all 
    /// </summary
    [HttpGet("Get/onBoardingConfigurations")]

    public async Task<IActionResult> GetOnBoardingConfiguration()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var config = await _service.GetConfigAsync(orgId);
            return Ok(new { success = true, data = config });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to fetch configuration." });
        }
    }

    /// <summary>
    /// Super Admin saves/updates config for any org.
    ///// </summary>
    //[HttpPut("org")]
    //[Authorize(Roles = "Super Admin")]
    //public async Task<IActionResult> SaveOrgConfig(Guid orgId, [FromBody] OnboardingFieldConfigRequest request)
    //{
    //    try
    //    {
    //        if (request?.Sections == null || request.Sections.Count == 0)
    //            return BadRequest(new { success = false, message = "No configuration sections provided." });

    //        var success = await _service.SaveConfigAsync(orgId, request);
    //        return Ok(new { success, message = "Onboarding configuration saved successfully." });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error saving org config for {OrgId}", orgId);
    //        return StatusCode(500, new { success = false, message = "Failed to save configuration." });
    //    }
    //}

    private Guid GetCurrentOrganizationId()
    {
        var claim = User.FindFirstValue("organizationId");
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Organization ID not found in token.");
        return id;
    }
}