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
    public async Task<ActionResult<ServiceResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetDashboardStatsAsync(organizationId);
            
            return Ok(new ServiceResponse<DashboardStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return StatusCode(500, new ServiceResponse<DashboardStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving dashboard statistics"
            });
        }
    }

    [HttpGet("recent-activities")]
    public async Task<ActionResult<ServiceResponse<List<RecentActivityDto>>>> GetRecentActivities([FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var activities = await _dashboardService.GetRecentActivitiesAsync(organizationId, limit);
            
            return Ok(new ServiceResponse<List<RecentActivityDto>>
            {
                Success = true,
                Data = activities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activities");
            return StatusCode(500, new ServiceResponse<List<RecentActivityDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recent activities"
            });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ServiceResponse<DashboardSummaryDto>>> GetDashboardSummary()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var summary = await _dashboardService.GetDashboardSummaryAsync(organizationId);
            
            return Ok(new ServiceResponse<DashboardSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard summary");
            return StatusCode(500, new ServiceResponse<DashboardSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving dashboard summary"
            });
        }
    }

    [HttpGet("attendance-stats")]
    public async Task<ActionResult<ServiceResponse<AttendanceStatsDto>>> GetAttendanceStats([FromQuery] string period = "month")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetAttendanceStatsAsync(organizationId, period);
            
            return Ok(new ServiceResponse<AttendanceStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance stats");
            return StatusCode(500, new ServiceResponse<AttendanceStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance statistics"
            });
        }
    }

    [HttpGet("leave-stats")]
    public async Task<ActionResult<ServiceResponse<LeaveStatsDto>>> GetLeaveStats([FromQuery] string period = "month")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetLeaveStatsAsync(organizationId, period);
            
            return Ok(new ServiceResponse<LeaveStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave stats");
            return StatusCode(500, new ServiceResponse<LeaveStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving leave statistics"
            });
        }
    }

    [HttpGet("payroll-stats")]
    public async Task<ActionResult<ServiceResponse<PayrollStatsDto>>> GetPayrollStats([FromQuery] string period = "month")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetPayrollStatsAsync(organizationId, period);
            
            return Ok(new ServiceResponse<PayrollStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll stats");
            return StatusCode(500, new ServiceResponse<PayrollStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll statistics"
            });
        }
    }

    [HttpGet("performance-stats")]
    public async Task<ActionResult<ServiceResponse<PerformanceStatsDto>>> GetPerformanceStats()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetPerformanceStatsAsync(organizationId);
            
            return Ok(new ServiceResponse<PerformanceStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance stats");
            return StatusCode(500, new ServiceResponse<PerformanceStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving performance statistics"
            });
        }
    }



    [HttpGet("employee-growth")]
    public async Task<ActionResult<ServiceResponse<EmployeeGrowthDto>>> GetEmployeeGrowth([FromQuery] string period = "12m")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var growth = await _dashboardService.GetEmployeeGrowthAsync(organizationId, period);
            
            return Ok(new ServiceResponse<EmployeeGrowthDto>
            {
                Success = true,
                Data = growth
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee growth");
            return StatusCode(500, new ServiceResponse<EmployeeGrowthDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee growth data"
            });
        }
    }

    [HttpGet("department-stats")]
    public async Task<ActionResult<ServiceResponse<List<DepartmentStatsDto>>>> GetDepartmentStats()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _dashboardService.GetDepartmentStatsAsync(organizationId);
            
            return Ok(new ServiceResponse<List<DepartmentStatsDto>>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department stats");
            return StatusCode(500, new ServiceResponse<List<DepartmentStatsDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving department statistics"
            });
        }
    }

    [HttpGet("upcoming-events")]
    public async Task<ActionResult<ServiceResponse<List<UpcomingEventDto>>>> GetUpcomingEvents([FromQuery] int limit = 5)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var events = await _dashboardService.GetUpcomingEventsAsync(organizationId, limit);
            
            return Ok(new ServiceResponse<List<UpcomingEventDto>>
            {
                Success = true,
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming events");
            return StatusCode(500, new ServiceResponse<List<UpcomingEventDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving upcoming events"
            });
        }
    }

    // Additional Dashboard endpoints that frontend expects
    [HttpGet("attendance-trends")]
    public async Task<ActionResult<ServiceResponse<object>>> GetAttendanceTrends([FromQuery] string period = "30d")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            // Mock implementation - replace with actual service call
            var trends = new { 
                dates = new[] { "2024-01-01", "2024-01-02", "2024-01-03" },
                presentCounts = new[] { 85, 90, 88 },
                absentCounts = new[] { 15, 10, 12 }
            };
            
            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Data = trends
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance trends");
            return StatusCode(500, new ServiceResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance trends"
            });
        }
    }

    [HttpGet("leave-trends")]
    public async Task<ActionResult<ServiceResponse<object>>> GetLeaveTrends([FromQuery] string period = "12m")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            // Mock implementation
            var trends = new { 
                months = new[] { "Jan", "Feb", "Mar" },
                leaveCounts = new[] { 25, 30, 28 }
            };
            
            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Data = trends
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave trends");
            return StatusCode(500, new ServiceResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving leave trends"
            });
        }
    }

    [HttpGet("payroll-trends")]
    public async Task<ActionResult<ServiceResponse<object>>> GetPayrollTrends([FromQuery] string period = "12m")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            // Mock implementation
            var trends = new { 
                months = new[] { "Jan", "Feb", "Mar" },
                payrollCosts = new[] { 150000, 155000, 152000 }
            };
            
            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Data = trends
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll trends");
            return StatusCode(500, new ServiceResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll trends"
            });
        }
    }

    [HttpGet("top-performers")]
    public async Task<ActionResult<ServiceResponse<List<object>>>> GetTopPerformers([FromQuery] int limit = 5)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            // Mock implementation
            var performers = new List<object>
            {
                new { name = "John Doe", rating = 4.8, department = "Engineering" },
                new { name = "Jane Smith", rating = 4.7, department = "Marketing" }
            };
            
            return Ok(new ServiceResponse<List<object>>
            {
                Success = true,
                Data = performers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top performers");
            return StatusCode(500, new ServiceResponse<List<object>>
            {
                Success = false,
                Message = "An error occurred while retrieving top performers"
            });
        }
    }

    [HttpGet("reminders")]
    public async Task<ActionResult<ServiceResponse<object>>> GetReminders()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            // Mock implementation
            var reminders = new { 
                birthdays = new[] { new { name = "John Doe", date = "2024-01-15" } },
                anniversaries = new[] { new { name = "Jane Smith", date = "2024-01-20", years = 3 } }
            };
            
            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Data = reminders
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminders");
            return StatusCode(500, new ServiceResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving reminders"
            });
        }
    }

    [HttpGet("quick-stats")]
    public async Task<ActionResult<ServiceResponse<object>>> GetQuickStats()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            // Mock implementation
            var stats = new { 
                todayPresent = 85,
                todayAbsent = 15,
                onLeave = 5,
                pendingApprovals = 12
            };
            
            return Ok(new ServiceResponse<object>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quick stats");
            return StatusCode(500, new ServiceResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving quick stats"
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        // Debug: Log all claims in the token
        var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
        _logger.LogInformation("All JWT Claims: {Claims}", string.Join(", ", allClaims));
        
        var orgIdClaim = User.FindFirstValue("organizationId");
        
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token. Available claims: {Claims}", 
                string.Join(", ", allClaims));
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
