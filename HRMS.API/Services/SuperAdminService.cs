using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly ISuperAdminRepository _superAdminRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(
        ISuperAdminRepository superAdminRepository,
        IPaymentRepository paymentRepository,
        IEntitlementService entitlementService,
        ILogger<SuperAdminService> logger)
    {
        _superAdminRepository = superAdminRepository;
        _paymentRepository = paymentRepository;
        _entitlementService = entitlementService;
        _logger = logger;
    }

    public async Task<ServiceResponse<PlatformDashboardDto>> GetPlatformDashboardAsync()
    {
        try
        {
            var dashboard = await _superAdminRepository.GetPlatformDashboardAsync();
            return new ServiceResponse<PlatformDashboardDto> { Success = true, Data = dashboard };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform dashboard");
            return new ServiceResponse<PlatformDashboardDto>
            {
                Success = false,
                Message = "Failed to load dashboard data"
            };
        }
    }

    public async Task<ServiceResponse<PagedResult<OrganizationListItemDto>>> GetOrganizationsAsync(OrganizationFilterRequest filter)
    {
        try
        {
            var result = await _superAdminRepository.GetOrganizationsPagedAsync(filter);
            return new ServiceResponse<PagedResult<OrganizationListItemDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations list");
            return new ServiceResponse<PagedResult<OrganizationListItemDto>>
            {
                Success = false,
                Message = "Failed to load organizations"
            };
        }
    }

    public async Task<ServiceResponse<OrganizationDetailDto>> GetOrganizationDetailAsync(Guid organizationId)
    {
        try
        {
            var detail = await _superAdminRepository.GetOrganizationDetailAsync(organizationId);
            if (detail == null)
            {
                return new ServiceResponse<OrganizationDetailDto>
                {
                    Success = false,
                    Message = "Organization not found"
                };
            }
            return new ServiceResponse<OrganizationDetailDto> { Success = true, Data = detail };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization detail for {OrganizationId}", organizationId);
            return new ServiceResponse<OrganizationDetailDto>
            {
                Success = false,
                Message = "Failed to load organization details"
            };
        }
    }

    public async Task<ServiceResponse<List<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(Guid companyId)
    {
        try
        {
            var history = await _superAdminRepository.GetSubscriptionHistoryAsync(companyId);
            return new ServiceResponse<List<SubscriptionHistoryDto>> { Success = true, Data = history };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history for {CompanyId}", companyId);
            return new ServiceResponse<List<SubscriptionHistoryDto>>
            {
                Success = false,
                Message = "Failed to load subscription history"
            };
        }
    }

    public async Task<ServiceResponse<bool>> ManageSubscriptionAsync(Guid companyId, ManageSubscriptionRequest request, Guid performedBy)
    {
        try
        {
            var action = request.Action.Trim().ToLowerInvariant();
            var validActions = new[] { "upgrade", "downgrade", "extend", "cancel" };
            if (!validActions.Contains(action))
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = $"Invalid action. Allowed values: {string.Join(", ", validActions)}"
                };
            }

            // Get current subscription
            var currentSubs = await _paymentRepository.GetCompanySubscriptionsByCompanyIdAsync(companyId);
            var currentSub = currentSubs
                .OrderBy(subscription => string.Equals(subscription.Status, "active", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : string.Equals(subscription.Status, "trial", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                .ThenBy(subscription => subscription.EndDate.HasValue ? 0 : 1)
                .ThenByDescending(subscription => subscription.EndDate)
                .FirstOrDefault();

            if (currentSub == null && action != "upgrade")
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "No active subscription found for this organization"
                };
            }

            Guid? oldPlanId = currentSub?.PlanId;
            DateTime? oldEndDate = currentSub?.EndDate;

            switch (action)
            {
                case "upgrade":
                case "downgrade":
                    if (!request.NewPlanId.HasValue)
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "New plan ID is required for upgrade/downgrade"
                        };
                    }

                    var plan = await _entitlementService.GetPlanAsync(request.NewPlanId.Value);
                    if (plan == null || !string.Equals(
                            plan.Status?.Trim(),
                            "active",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "The selected plan is unavailable or archived."
                        };
                    }

                    var billingCycle = request.BillingCycle ?? currentSub?.BillingCycle ?? "monthly";
                    var startDate = DateTime.UtcNow;
                    var endDate = request.NewEndDate ?? (string.Equals(
                        billingCycle,
                        "annual",
                        StringComparison.OrdinalIgnoreCase)
                        ? startDate.AddYears(1)
                        : startDate.AddMonths(1));
                    var assigned = await _entitlementService.AssignTenantPlanAsync(companyId, new TenantPlanAssignmentRequest
                    {
                        PlanId = request.NewPlanId.Value,
                        Status = "active",
                        BillingCycle = billingCycle,
                        StartDate = startDate,
                        EndDate = endDate,
                        CustomCurrency = plan.Currency
                    }, performedBy);
                    if (!assigned)
                    {
                        throw new InvalidOperationException("The subscription could not be updated.");
                    }
                    break;

                case "extend":
                    if (!request.NewEndDate.HasValue)
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "New end date is required for extension"
                        };
                    }

                    await _paymentRepository.UpdateCompanySubscriptionAsync(currentSub!.SubscriptionId,
                        new UpdateCompanySubscriptionRequest
                        {
                            PlanId = currentSub.PlanId,
                            Status = "active",
                            BillingCycle = currentSub.BillingCycle,
                            StartDate = currentSub.StartDate,
                            EndDate = request.NewEndDate.Value
                        });
                    break;

                case "cancel":
                    await _paymentRepository.UpdateCompanySubscriptionAsync(currentSub!.SubscriptionId,
                        new UpdateCompanySubscriptionRequest
                        {
                            PlanId = currentSub.PlanId,
                            Status = "cancelled",
                            BillingCycle = currentSub.BillingCycle,
                            StartDate = currentSub.StartDate,
                            EndDate = DateTime.UtcNow
                        });
                    break;
            }

            // Log history
            await _superAdminRepository.LogSubscriptionHistoryAsync(
                subscriptionId: currentSub?.SubscriptionId ?? Guid.Empty,
                companyId: companyId,
                action,
                oldPlanId: oldPlanId,
                newPlanId: request.NewPlanId,
                oldEndDate: oldEndDate,
                newEndDate: request.NewEndDate ?? (action == "cancel" ? DateTime.UtcNow : null),
                performedBy: performedBy,
                notes: request.Notes
            );

            _entitlementService.InvalidateTenantCache(companyId);
            _logger.LogInformation("Subscription {Action} for company {CompanyId} by admin {AdminId}",
                action, companyId, performedBy);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Subscription {action} completed successfully"
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription change for {CompanyId}", companyId);
            return new ServiceResponse<bool> { Success = false, Message = ex.Message };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription change rejected for {CompanyId}", companyId);
            return new ServiceResponse<bool> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing subscription for {CompanyId}", companyId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to manage subscription"
            };
        }
    }
}
