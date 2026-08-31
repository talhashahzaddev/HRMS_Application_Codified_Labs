using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PositionController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IAuthService _authService;
    private readonly ILogger<PositionController> _logger;

    public PositionController(IEmployeeService employeeService, IAuthService authService, ILogger<PositionController> logger)
    {
        _employeeService = employeeService;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<PositionDto>>>> GetPositions()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var positions = await _employeeService.GetPositionsAsync(organizationId);
            
            return Ok(new ServiceResponse<IReadOnlyList<PositionDto>>
            {
                Success = true,
                Data = positions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving positions");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<PositionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving positions"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<PositionDto>>> GetPosition(Guid id)
    {
        try
        {
            var position = await _employeeService.GetPositionByIdAsync(id);
            if (position == null)
            {
                return NotFound(new ServiceResponse<PositionDto>
                {
                    Success = false,
                    Message = "Position not found"
                });
            }

            return Ok(new ServiceResponse<PositionDto>
            {
                Success = true,
                Data = position
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving position: {PositionId}", id);
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving position {PositionId}", id);
            return StatusCode(500, new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the position"
            });
        }
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ServiceResponse<List<RoleDto>>>> GetRoles()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var roles = await _authService.GetOrganizationRolesAsync(organizationId);
            
            return Ok(new ServiceResponse<IReadOnlyList<RoleDto>>
            {
                Success = true,
                Data = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<RoleDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving roles"
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
