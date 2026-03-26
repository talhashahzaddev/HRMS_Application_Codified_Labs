using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayRollController : ControllerBase
{
    private readonly IPayRollService _payRollService;
    private readonly ILogger<PayRollController> _logger;

    public PayRollController(IPayRollService payRollService, ILogger<PayRollController> logger)
    {
        _payRollService = payRollService;
        _logger = logger;
    }

    [HttpGet("rules")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<PayRollRuleDto>>>> GetAllRules()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var response = await _payRollService.GetAllRulesAsync(organizationId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while retrieving payroll rules");
            return StatusCode(403, new ServiceResponse<IEnumerable<PayRollRuleDto>>
            {
                Success = false,
                Message = "You do not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll rules");
            return StatusCode(500, new ServiceResponse<IEnumerable<PayRollRuleDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll rules"
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
