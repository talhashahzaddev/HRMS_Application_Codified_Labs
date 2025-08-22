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
    private readonly ILogger<PositionController> _logger;

    public PositionController(IEmployeeService employeeService, ILogger<PositionController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PositionDto>>>> GetPositions()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var positions = await _employeeService.GetPositionsAsync(organizationId);
            
            return Ok(new ApiResponse<List<PositionDto>>
            {
                Success = true,
                Data = positions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving positions");
            return StatusCode(500, new ApiResponse<List<PositionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving positions"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetPosition(Guid id)
    {
        try
        {
            var position = await _employeeService.GetPositionByIdAsync(id);
            if (position == null)
            {
                return NotFound(new ApiResponse<PositionDto>
                {
                    Success = false,
                    Message = "Position not found"
                });
            }

            return Ok(new ApiResponse<PositionDto>
            {
                Success = true,
                Data = position
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving position {PositionId}", id);
            return StatusCode(500, new ApiResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the position"
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
