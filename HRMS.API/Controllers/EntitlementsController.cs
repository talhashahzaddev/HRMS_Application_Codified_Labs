using System.Security.Claims;
using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EntitlementsController : ControllerBase
{
    private readonly IEntitlementService _entitlementService;
    private readonly ILogger<EntitlementsController> _logger;

    public EntitlementsController(
        IEntitlementService entitlementService,
        ILogger<EntitlementsController> logger)
    {
        _entitlementService = entitlementService;
        _logger = logger;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<TenantEntitlementsDto>>> GetMyEntitlements()
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new ServiceResponse<TenantEntitlementsDto>
            {
                Success = false,
                Message = "A tenant identity is required."
            });
        }

        var entitlements = await _entitlementService.GetTenantEntitlementsAsync(tenantId);
        return Ok(new ServiceResponse<TenantEntitlementsDto>
        {
            Success = true,
            Data = entitlements
        });
    }

    [HttpGet("plans")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<EntitlementPlanDto>>>> GetPlans(
        [FromQuery] bool includeArchived = false)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        var plans = await _entitlementService.GetPlansAsync(includeArchived);
        return Ok(new ServiceResponse<IReadOnlyList<EntitlementPlanDto>>
        {
            Success = true,
            Data = plans
        });
    }

    [HttpGet("features")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<EntitlementFeatureDto>>>> GetFeatures()
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        var features = await _entitlementService.GetFeatureCatalogAsync();
        return Ok(new ServiceResponse<IReadOnlyList<EntitlementFeatureDto>>
        {
            Success = true,
            Data = features
        });
    }

    [HttpGet("comparison")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<EntitlementPlanDto>>>> GetPlanComparison()
    {
        var plans = await _entitlementService.GetPlansAsync(includeArchived: false);
        return Ok(new ServiceResponse<IReadOnlyList<EntitlementPlanDto>>
        {
            Success = true,
            Data = plans.Where(plan => plan.IsPublic).ToArray()
        });
    }

    [HttpGet("tenants/{tenantId:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<TenantEntitlementsDto>>> GetTenantEntitlements(Guid tenantId)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        var entitlements = await _entitlementService.GetTenantEntitlementsAsync(tenantId);
        return Ok(new ServiceResponse<TenantEntitlementsDto>
        {
            Success = true,
            Data = entitlements
        });
    }

    [HttpPost("plans")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreatePlan([FromBody] CreatePlanEntitlementsRequest request)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        try
        {
            var planId = await _entitlementService.CreatePlanAsync(request, GetActorId());
            return Ok(new ServiceResponse<Guid> { Success = true, Data = planId, Message = "Plan created." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<Guid> { Success = false, Message = ex.Message });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "A plan with code {PlanCode} already exists.", request.Code);
            return Conflict(new ServiceResponse<Guid> { Success = false, Message = "A plan with this code already exists." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create a subscription plan.");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "Failed to create plan." });
        }
    }

    [HttpPut("plans/{planId:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdatePlan(
        Guid planId,
        [FromBody] UpdatePlanEntitlementsRequest request)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        try
        {
            var updated = await _entitlementService.UpdatePlanAsync(planId, request, GetActorId());
            if (!updated)
            {
                return Conflict(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "The plan changed since it was loaded or is archived. Reload and retry."
                });
            }

            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Plan updated." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update plan {PlanId}.", planId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "Failed to update plan." });
        }
    }

    [HttpPost("plans/{planId:guid}/duplicate")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> DuplicatePlan(
        Guid planId,
        [FromBody] DuplicateEntitlementPlanRequest request)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        try
        {
            var duplicated = await _entitlementService.DuplicatePlanAsync(planId, request.Name, request.Code, GetActorId());
            return duplicated
                ? Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Plan duplicated." })
                : NotFound(new ServiceResponse<bool> { Success = false, Message = "Plan not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "A duplicate plan code was requested for plan {PlanId}.", planId);
            return Conflict(new ServiceResponse<bool> { Success = false, Message = "A plan with this code already exists." });
        }
    }

    [HttpPost("plans/{planId:guid}/archive")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> ArchivePlan(Guid planId)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        var archived = await _entitlementService.ArchivePlanAsync(planId, GetActorId());
        return archived
            ? Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Plan archived." })
            : NotFound(new ServiceResponse<bool> { Success = false, Message = "Plan is missing or already archived." });
    }

    [HttpPost("plans/{planId:guid}/reactivate")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> ReactivatePlan(Guid planId)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        var reactivated = await _entitlementService.ReactivatePlanAsync(planId, GetActorId());
        return reactivated
            ? Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Plan reactivated." })
            : NotFound(new ServiceResponse<bool> { Success = false, Message = "Plan is missing or already active." });
    }

    [HttpPut("tenants/{tenantId:guid}/overrides")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> SaveTenantOverrides(
        Guid tenantId,
        [FromBody] TenantOverridesRequest request)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        try
        {
            await _entitlementService.SaveTenantOverridesAsync(tenantId, request, GetActorId());
            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Tenant overrides saved." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    [HttpPut("tenants/{tenantId:guid}/custom-plan")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> SaveTenantCustomPlan(
        Guid tenantId,
        [FromBody] TenantCustomPlanRequest request)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        try
        {
            await _entitlementService.SaveTenantCustomPlanAsync(tenantId, request, GetActorId());
            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Custom tenant plan saved." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    [HttpPut("tenants/{tenantId:guid}/subscription")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> AssignTenantPlan(
        Guid tenantId,
        [FromBody] TenantPlanAssignmentRequest request)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        try
        {
            await _entitlementService.AssignTenantPlanAsync(tenantId, request, GetActorId());
            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Subscription updated." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    [HttpGet("audit")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<EntitlementAuditEntryDto>>>> GetAuditLogs(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action)
    {
        if (!IsPlatformAdmin())
        {
            return Forbid();
        }

        var logs = await _entitlementService.GetAuditLogsAsync(tenantId, from, to, action);
        return Ok(new ServiceResponse<IReadOnlyList<EntitlementAuditEntryDto>>
        {
            Success = true,
            Data = logs
        });
    }

    private bool IsPlatformAdmin() => User.HasClaim("isPlatformAdmin", "true");

    private bool TryGetTenantId(out Guid tenantId) =>
        Guid.TryParse(User.FindFirstValue(CustomClaimTypes.OrganizationId), out tenantId) && tenantId != Guid.Empty;

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;
}

public sealed class DuplicateEntitlementPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
