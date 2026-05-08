using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatformAuthController : ControllerBase
{
    private readonly IPlatformAdminAuthService _authService;
    private readonly ILogger<PlatformAuthController> _logger;

    public PlatformAuthController(IPlatformAdminAuthService authService, ILogger<PlatformAuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ServiceResponse<PlatformAdminAuthResponse>>> Login([FromBody] PlatformAdminLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<PlatformAdminAuthResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var response = await _authService.AuthenticateAsync(request);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Platform admin login failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<PlatformAdminAuthResponse>
            {
                Success = false,
                Message = "An error occurred during authentication"
            });
        }
    }
}
