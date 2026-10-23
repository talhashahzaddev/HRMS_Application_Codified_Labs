using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Platform Admin")]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    private readonly ITransactionService _transactionService;
    private readonly IAuthRepository _authRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        ISuperAdminService superAdminService,
        ITransactionService transactionService,
        IAuthRepository authRepository,
        IAuthService authService,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _transactionService = transactionService;
        _authRepository = authRepository;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("menus")]
    public async Task<ActionResult<ServiceResponse<List<MenuDto>>>> GetMenus()
    {
        var menus = await _authService.GetMenusAsync();
        return Ok(new ServiceResponse<List<MenuDto>>
        {
            Success = true,
            Data = menus,
            Message = "Menus retrieved successfully."
        });
    }

    [HttpGet("global-roles")]
    public async Task<ActionResult<ServiceResponse<List<RoleDto>>>> GetGlobalRoles()
    {
        var roles = await _authRepository.GetGlobalRolesAsync();
        var result = roles.Select(role => new RoleDto
        {
            RoleId = role.RoleId ?? Guid.Empty,
            RoleName = role.RoleName,
            Description = role.Description,
            OrganizationId = role.OrganizationId ?? Guid.Empty,
            OrganizationName = role.OrganizationName,
            IsSystemRole = role.IsSystemRole ?? false,
            CreatedAt = role.CreatedAt ?? DateTime.MinValue,
            PermissionsCount = role.PermissionsCount
        }).ToList();

        return Ok(new ServiceResponse<List<RoleDto>>
        {
            Success = true,
            Data = result,
            Message = "Global roles retrieved successfully."
        });
    }

    [HttpPost("create-global-role")]
    public async Task<ActionResult<ServiceResponse<CreateRoleResponse>>> CreateGlobalRole(
        [FromBody] Create_RoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<CreateRoleResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _authService.Create_RoleAsync(request, null);
            return Ok(new ServiceResponse<CreateRoleResponse>
            {
                Success = true,
                Data = result,
                Message = "Global role created successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<CreateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ServiceResponse<CreateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.NotNullViolation)
        {
            _logger.LogError(ex,
                "Global role creation failed because the database schema does not allow NULL organization IDs.");

            return Conflict(new ServiceResponse<CreateRoleResponse>
            {
                Success = false,
                Message = "The database schema does not support global roles yet. Run HRMS.Script/06_global_roles_migration.sql and try again."
            });
        }
    }

    [HttpDelete("delete-global-role/{roleId:guid}")]
    public async Task<ActionResult<ServiceResponse<DeleteRoleResponse>>> DeleteGlobalRole(Guid roleId)
    {
        try
        {
            var role = await _authRepository.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                return NotFound(new ServiceResponse<DeleteRoleResponse>
                {
                    Success = false,
                    Message = $"Role with ID {roleId} not found"
                });
            }

            var result = await _authService.DeleteRoleAsync(roleId, role.OrganizationId);
            return Ok(new ServiceResponse<DeleteRoleResponse>
            {
                Success = true,
                Data = result,
                Message = "Global role deleted successfully."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<DeleteRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<DeleteRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ServiceResponse<DeleteRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("invitations")]
    public ActionResult<ServiceResponse<PagedResult<UserInvitationDto>>> GetInvitations(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return Ok(new ServiceResponse<PagedResult<UserInvitationDto>>
        {
            Success = true,
            Data = new PagedResult<UserInvitationDto>
            {
                Data = new List<UserInvitationDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            },
            Message = "Invitations retrieved successfully."
        });
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


    // ========== PAYMENT MANAGEMENT (Admin) ==========

    /// <summary>
    /// Get payment dashboard with revenue statistics
    /// </summary>
    [HttpGet("payment-dashboard")]
    public async Task<ActionResult<ServiceResponse<PaymentDashboardDto>>> GetPaymentDashboard()
    {
        try
        {
            var response = await _transactionService.GetPaymentDashboardAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment dashboard");
            return StatusCode(500, new ServiceResponse<PaymentDashboardDto> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get all transactions with filters (admin view)
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ServiceResponse<PagedResult<TransactionDto>>>> GetAllTransactions(
        [FromQuery] string? status, [FromQuery] string? gateway, [FromQuery] Guid? companyId,
        [FromQuery] Guid? planId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? sortBy, [FromQuery] string? sortDirection,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var filter = new TransactionFilterRequest
            {
                Status = status, Gateway = gateway, CompanyId = companyId, PlanId = planId,
                DateFrom = dateFrom, DateTo = dateTo,
                SortBy = sortBy ?? "created_at", SortDirection = sortDirection ?? "desc",
                Page = page, PageSize = Math.Min(pageSize, 100)
            };
            var response = await _transactionService.GetAllTransactionsAsync(filter);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all transactions");
            return StatusCode(500, new ServiceResponse<PagedResult<TransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get detailed transaction information
    /// </summary>
    [HttpGet("transactions/{transactionId:guid}")]
    public async Task<ActionResult<ServiceResponse<TransactionDetailDto>>> GetTransactionDetail(Guid transactionId)
    {
        try
        {
            var response = await _transactionService.GetTransactionDetailAsync(transactionId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction detail {TransactionId}", transactionId);
            return StatusCode(500, new ServiceResponse<TransactionDetailDto> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get all invoices with filters (admin view)
    /// </summary>
    [HttpGet("invoices")]
    public async Task<ActionResult<ServiceResponse<PagedResult<InvoiceDto>>>> GetAllInvoices(
        [FromQuery] string? status, [FromQuery] Guid? companyId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? sortBy, [FromQuery] string? sortDirection,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var filter = new InvoiceFilterRequest
            {
                Status = status, CompanyId = companyId,
                DateFrom = dateFrom, DateTo = dateTo,
                SortBy = sortBy ?? "issued_at", SortDirection = sortDirection ?? "desc",
                Page = page, PageSize = Math.Min(pageSize, 100)
            };
            var response = await _transactionService.GetAllInvoicesAsync(filter);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all invoices");
            return StatusCode(500, new ServiceResponse<PagedResult<InvoiceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Process a refund for a transaction
    /// </summary>
    [HttpPost("refunds")]
    public async Task<ActionResult<ServiceResponse<RefundDto>>> CreateRefund([FromBody] CreateRefundRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ServiceResponse<RefundDto> { Success = false, Message = "Invalid request" });

            var adminId = GetCurrentAdminId();
            var response = await _transactionService.CreateRefundAsync(adminId, request);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund");
            return StatusCode(500, new ServiceResponse<RefundDto> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get revenue analytics (charts data)
    /// </summary>
    [HttpGet("revenue-analytics")]
    public async Task<ActionResult<ServiceResponse<RevenueAnalyticsDto>>> GetRevenueAnalytics(
        [FromQuery] int months = 12)
    {
        try
        {
            var response = await _transactionService.GetRevenueAnalyticsAsync(Math.Min(months, 24));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return StatusCode(500, new ServiceResponse<RevenueAnalyticsDto> { Success = false, Message = "An error occurred" });
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
