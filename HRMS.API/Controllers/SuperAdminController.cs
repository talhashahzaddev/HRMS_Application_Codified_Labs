using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Platform Admin")]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(ISuperAdminService superAdminService, ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Get platform-level dashboard statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ServiceResponse<PlatformDashboardDto>>> GetDashboard()
    {
        try
        {
            var response = await _superAdminService.GetPlatformDashboardAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform dashboard");
            return StatusCode(500, new ServiceResponse<PlatformDashboardDto>
            {
                Success = false,
                Message = "An error occurred while loading the dashboard"
            });
        }
    }

    /// <summary>
    /// Get paginated list of all organizations with filtering/sorting
    /// </summary>
    [HttpGet("organizations")]
    public async Task<ActionResult<ServiceResponse<PagedResult<OrganizationListItemDto>>>> GetOrganizations(
        [FromQuery] string? search,
        [FromQuery] string? subscriptionStatus,
        [FromQuery] Guid? planId,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var filter = new OrganizationFilterRequest
            {
                Search = search,
                SubscriptionStatus = subscriptionStatus,
                PlanId = planId,
                SortBy = sortBy ?? "createdat",
                SortDirection = sortDirection ?? "desc",
                Page = page,
                PageSize = Math.Min(pageSize, 50)
            };

            var response = await _superAdminService.GetOrganizationsAsync(filter);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations list");
            return StatusCode(500, new ServiceResponse<PagedResult<OrganizationListItemDto>>
            {
                Success = false,
                Message = "An error occurred while loading organizations"
            });
        }
    }

    /// <summary>
    /// Get detailed view of a specific organization
    /// </summary>
    [HttpGet("organizations/{organizationId:guid}")]
    public async Task<ActionResult<ServiceResponse<OrganizationDetailDto>>> GetOrganizationDetail(Guid organizationId)
    {
        try
        {
            var response = await _superAdminService.GetOrganizationDetailAsync(organizationId);
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization detail {OrganizationId}", organizationId);
            return StatusCode(500, new ServiceResponse<OrganizationDetailDto>
            {
                Success = false,
                Message = "An error occurred while loading organization details"
            });
        }
    }

    /// <summary>
    /// Get subscription history for an organization
    /// </summary>
    [HttpGet("organizations/{organizationId:guid}/subscription-history")]
    public async Task<ActionResult<ServiceResponse<List<SubscriptionHistoryDto>>>> GetSubscriptionHistory(Guid organizationId)
    {
        try
        {
            var response = await _superAdminService.GetSubscriptionHistoryAsync(organizationId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history for {OrganizationId}", organizationId);
            return StatusCode(500, new ServiceResponse<List<SubscriptionHistoryDto>>
            {
                Success = false,
                Message = "An error occurred while loading subscription history"
            });
        }
    }

    /// <summary>
    /// Manage an organization's subscription (upgrade/downgrade/extend/cancel)
    /// </summary>
    [HttpPut("organizations/{organizationId:guid}/subscription")]
    public async Task<ActionResult<ServiceResponse<bool>>> ManageSubscription(
        Guid organizationId,
        [FromBody] ManageSubscriptionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            var adminId = GetCurrentAdminId();
            var response = await _superAdminService.ManageSubscriptionAsync(organizationId, request, adminId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing subscription for {OrganizationId}", organizationId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while managing the subscription"
            });
        }
    }

    private Guid GetCurrentAdminId()
    {
        var adminIdClaim = User.FindFirstValue("adminId");
        if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
        {
            throw new UnauthorizedAccessException("Admin ID not found in token");
        }
        return adminId;
    }
}
