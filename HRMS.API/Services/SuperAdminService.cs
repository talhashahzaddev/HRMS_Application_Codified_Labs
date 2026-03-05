using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly ISuperAdminRepository _superAdminRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(
        ISuperAdminRepository superAdminRepository,
        IPaymentRepository paymentRepository,
        ILogger<SuperAdminService> logger)
    {
        _superAdminRepository = superAdminRepository;
        _paymentRepository = paymentRepository;
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
            var validActions = new[] { "upgrade", "downgrade", "extend", "cancel" };
            if (!validActions.Contains(request.Action.ToLower()))
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = $"Invalid action. Allowed values: {string.Join(", ", validActions)}"
                };
            }

            // Get current subscription
            var currentSubs = await _paymentRepository.GetCompanySubscriptionsByCompanyIdAsync(companyId);
            var currentSub = currentSubs.OrderByDescending(s => s.EndDate).FirstOrDefault();

            if (currentSub == null && request.Action.ToLower() != "upgrade")
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "No active subscription found for this organization"
                };
            }

            Guid? oldPlanId = currentSub?.PlanId;
            DateTime? oldEndDate = currentSub?.EndDate;

            switch (request.Action.ToLower())
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

                    var plan = await _paymentRepository.GetSubscriptionPlanByIdAsync(request.NewPlanId.Value);
                    if (plan == null)
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "Selected plan not found"
                        };
                    }

                    var updateRequest = new UpdateCompanySubscriptionRequest
                    {
                        PlanId = request.NewPlanId.Value,
                        Status = "active",
                        BillingCycle = request.BillingCycle ?? currentSub?.BillingCycle ?? "monthly",
                        StartDate = DateTime.UtcNow,
                        EndDate = request.NewEndDate ?? DateTime.UtcNow.AddMonths(
                            (request.BillingCycle ?? currentSub?.BillingCycle) == "annual" ? 12 : 1)
                    };

                    if (currentSub != null)
                    {
                        await _paymentRepository.UpdateCompanySubscriptionAsync(currentSub.SubscriptionId, updateRequest);
                    }
                    else
                    {
                        // Create new subscription
                        await _paymentRepository.CreateCompanySubscriptionAsync(new CreateCompanySubscriptionRequest
                        {
                            CompanyId = companyId,
                            PlanId = request.NewPlanId.Value,
                            Status = "active",
                            BillingCycle = request.BillingCycle ?? "monthly",
                            StartDate = DateTime.UtcNow,
                            EndDate = updateRequest.EndDate
                        });
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
                action: request.Action.ToLower(),
                oldPlanId: oldPlanId,
                newPlanId: request.NewPlanId,
                oldEndDate: oldEndDate,
                newEndDate: request.NewEndDate ?? (request.Action == "cancel" ? DateTime.UtcNow : null),
                performedBy: performedBy,
                notes: request.Notes
            );

            _logger.LogInformation("Subscription {Action} for company {CompanyId} by admin {AdminId}",
                request.Action, companyId, performedBy);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Subscription {request.Action} completed successfully"
            };
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
