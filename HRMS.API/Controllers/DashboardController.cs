using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService  _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService  dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger           = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// GET /api/dashboard/my-overview
    ///
    /// Returns a single aggregated payload containing:
    ///  • Leave balances (all types with name + remaining days)
    ///  • Latest payslip URL + net salary
    ///  • Active loans and disbursed salary advances (remaining balance)
    ///  • All pending requests (Leave / ShiftSwap / Overtime / SalaryAdvance / Loan / PF)
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("my-overview")]
    public async Task<ActionResult<ServiceResponse<EmployeeOverviewDto>>> GetMyOverview()
    {
        try
        {
            var userId         = GetCurrentUserId();
            var organizationId = GetCurrentOrganizationId();

            var data = await _dashboardService.GetEmployeeOverviewAsync(userId, organizationId);

            return Ok(new ServiceResponse<EmployeeOverviewDto>
            {
                Success = true,
                Data    = data,
                Message = "Dashboard overview fetched successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to dashboard overview");
            return Unauthorized(new ServiceResponse<EmployeeOverviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard overview");
            return StatusCode(500, new ServiceResponse<EmployeeOverviewDto>
            {
                Success = false,
                Message = "An error occurred while fetching the dashboard overview"
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MANAGER OVERVIEW
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("manager-overview")]
    public async Task<ActionResult<ServiceResponse<ManagerOverviewDto>>> GetManagerOverview()
    {
        try
        {
            var userId         = GetCurrentUserId();
            var organizationId = GetCurrentOrganizationId();

            var data = await _dashboardService.GetManagerOverviewAsync(userId, organizationId);

            return Ok(new ServiceResponse<ManagerOverviewDto>
            {
                Success = true,
                Data    = data,
                Message = "Manager overview fetched successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to manager overview");
            return Unauthorized(new ServiceResponse<ManagerOverviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching manager overview");
            return StatusCode(500, new ServiceResponse<ManagerOverviewDto>
            {
                Success = false,
                Message = "An error occurred while fetching the manager overview"
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers (same pattern used across all other controllers)
    // ─────────────────────────────────────────────────────────────────────────
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }

        return userId;
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
