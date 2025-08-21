using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetDashboardStatsAsync(organizationId);
            
            return Ok(new ApiResponse<DashboardStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return StatusCode(500, new ApiResponse<DashboardStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving dashboard statistics"
            });
        }
    }

    [HttpGet("activities")]
    public async Task<ActionResult<ApiResponse<List<RecentActivityDto>>>> GetRecentActivities([FromQuery] int count = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var activities = await _dashboardService.GetRecentActivitiesAsync(organizationId, count);
            
            return Ok(new ApiResponse<List<RecentActivityDto>>
            {
                Success = true,
                Data = activities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activities");
            return StatusCode(500, new ApiResponse<List<RecentActivityDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recent activities"
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        return Guid.Parse(orgIdClaim!);
    }
}
