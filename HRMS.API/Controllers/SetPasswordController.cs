using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Public controller for setting password via invitation token.
/// No authentication required — this is the public-facing endpoint
/// that invited users access to complete their account setup.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SetPasswordController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    private readonly ILogger<SetPasswordController> _logger;

    public SetPasswordController(
        ISuperAdminService superAdminService,
        ILogger<SetPasswordController> logger)
    {
        _superAdminService = superAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Validate an invitation token and return user info
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<ServiceResponse<SetPasswordValidationDto>>> ValidateToken(
        [FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new ServiceResponse<SetPasswordValidationDto>
                {
                    Success = false,
                    Message = "Token is required"
                });
            }

            var response = await _superAdminService.ValidateInvitationTokenAsync(token);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token");
            return StatusCode(500, new ServiceResponse<SetPasswordValidationDto>
            {
                Success = false,
                Message = "An error occurred while validating the token"
            });
        }
    }

    /// <summary>
    /// Set password for an invited user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ServiceResponse<bool>>> SetPassword(
        [FromBody] SetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var response = await _superAdminService.SetPasswordAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting password");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while setting the password"
            });
        }
    }
}
