using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<ServiceResponse<SubscriptionPlanDto>> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        _logger.LogInformation("Getting subscription plan by ID: {PlanId}", planId);

        if (planId == Guid.Empty)
        {
            throw new ArgumentException("Plan ID cannot be empty", nameof(planId));
        }

        var plan = await _paymentRepository.GetSubscriptionPlanByIdAsync(planId);
        
        if (plan == null)
        {
            _logger.LogWarning("Subscription plan not found: {PlanId}", planId);
            return new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = "Subscription plan not found"
            };
        }

        return new ServiceResponse<SubscriptionPlanDto>
        {
            Success = true,
            Data = plan
        };
    }

    public async Task<ServiceResponse<IEnumerable<SubscriptionPlanDto>>> GetAllSubscriptionPlansAsync()
    {
        _logger.LogInformation("Getting all subscription plans");

        var plans = await _paymentRepository.GetAllSubscriptionPlansAsync();

        return new ServiceResponse<IEnumerable<SubscriptionPlanDto>>
        {
            Success = true,
            Data = plans
        };
    }

    public async Task<ServiceResponse<IEnumerable<SubscriptionPlanDto>>> GetActiveSubscriptionPlansAsync()
    {
        _logger.LogInformation("Getting active subscription plans");

        var plans = await _paymentRepository.GetActiveSubscriptionPlansAsync();

        return new ServiceResponse<IEnumerable<SubscriptionPlanDto>>
        {
            Success = true,
            Data = plans
        };
    }

    public async Task<ServiceResponse<SubscriptionPlanDto>> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request)
    {
        _logger.LogInformation("Creating subscription plan: {Name}", request?.Name);

        // Validate request
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Plan name is required", nameof(request));
        }

        if (request.Name.Length > 200)
        {
            throw new ArgumentException("Plan name cannot exceed 200 characters", nameof(request));
        }

        // Validate price values
        if (request.MonthlyPrice.HasValue && request.MonthlyPrice.Value < 0)
        {
            throw new ArgumentException("Monthly price cannot be negative", nameof(request));
        }

        if (request.AnnualPrice.HasValue && request.AnnualPrice.Value < 0)
        {
            throw new ArgumentException("Annual price cannot be negative", nameof(request));
        }

        if (request.MaxUsers.HasValue && request.MaxUsers.Value < 0)
        {
            throw new ArgumentException("Max users cannot be negative", nameof(request));
        }

        try
        {
            var planId = await _paymentRepository.CreateSubscriptionPlanAsync(request);
            
            if (planId == Guid.Empty)
            {
                _logger.LogError("Failed to create subscription plan: {Name}", request.Name);
                throw new InvalidOperationException("Failed to create subscription plan");
            }

            var createdPlan = await _paymentRepository.GetSubscriptionPlanByIdAsync(planId);

            if (createdPlan == null)
            {
                _logger.LogError("Created subscription plan but failed to retrieve it: {PlanId}", planId);
                throw new InvalidOperationException("Failed to retrieve created subscription plan");
            }

            _logger.LogInformation("Successfully created subscription plan: {PlanId}", planId);

            return new ServiceResponse<SubscriptionPlanDto>
            {
                Success = true,
                Message = "Subscription plan created successfully",
                Data = createdPlan
            };
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating subscription plan: {Name}", request.Name);
            throw new InvalidOperationException("An error occurred while creating the subscription plan", ex);
        }
    }

    public async Task<ServiceResponse<SubscriptionPlanDto>> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request)
    {
        _logger.LogInformation("Updating subscription plan: {PlanId}", planId);

        // Validate planId
        if (planId == Guid.Empty)
        {
            throw new ArgumentException("Plan ID cannot be empty", nameof(planId));
        }

        // Validate request
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Plan name is required", nameof(request));
        }

        if (request.Name.Length > 200)
        {
            throw new ArgumentException("Plan name cannot exceed 200 characters", nameof(request));
        }

        // Validate price values
        if (request.MonthlyPrice.HasValue && request.MonthlyPrice.Value < 0)
        {
            throw new ArgumentException("Monthly price cannot be negative", nameof(request));
        }

        if (request.AnnualPrice.HasValue && request.AnnualPrice.Value < 0)
        {
            throw new ArgumentException("Annual price cannot be negative", nameof(request));
        }

        if (request.MaxUsers.HasValue && request.MaxUsers.Value < 0)
        {
            throw new ArgumentException("Max users cannot be negative", nameof(request));
        }

        // Check if plan exists
        var existingPlan = await _paymentRepository.GetSubscriptionPlanByIdAsync(planId);
        
        if (existingPlan == null)
        {
            _logger.LogWarning("Subscription plan not found for update: {PlanId}", planId);
            return new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = "Subscription plan not found"
            };
        }

        try
        {
            var success = await _paymentRepository.UpdateSubscriptionPlanAsync(planId, request);
            
            if (!success)
            {
                _logger.LogError("Failed to update subscription plan: {PlanId}", planId);
                throw new InvalidOperationException("Failed to update subscription plan");
            }

            var updatedPlan = await _paymentRepository.GetSubscriptionPlanByIdAsync(planId);

            if (updatedPlan == null)
            {
                _logger.LogError("Updated subscription plan but failed to retrieve it: {PlanId}", planId);
                throw new InvalidOperationException("Failed to retrieve updated subscription plan");
            }

            _logger.LogInformation("Successfully updated subscription plan: {PlanId}", planId);

            return new ServiceResponse<SubscriptionPlanDto>
            {
                Success = true,
                Message = "Subscription plan updated successfully",
                Data = updatedPlan
            };
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating subscription plan: {PlanId}", planId);
            throw new InvalidOperationException("An error occurred while updating the subscription plan", ex);
        }
    }

    public async Task<ServiceResponse<bool>> InactivateSubscriptionPlanAsync(Guid planId)
    {
        _logger.LogInformation("Inactivating subscription plan: {PlanId}", planId);

        if (planId == Guid.Empty)
        {
            throw new ArgumentException("Plan ID cannot be empty", nameof(planId));
        }

        var existingPlan = await _paymentRepository.GetSubscriptionPlanByIdAsync(planId);
        
        if (existingPlan == null)
        {
            _logger.LogWarning("Subscription plan not found for inactivation: {PlanId}", planId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Subscription plan not found"
            };
        }

        if (!existingPlan.IsActive)
        {
            _logger.LogInformation("Subscription plan is already inactive: {PlanId}", planId);
            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Subscription plan is already inactive",
                Data = true
            };
        }

        try
        {
            var success = await _paymentRepository.InactivateSubscriptionPlanAsync(planId);
            
            if (!success)
            {
                _logger.LogError("Failed to inactivate subscription plan: {PlanId}", planId);
                throw new InvalidOperationException("Failed to inactivate subscription plan");
            }

            _logger.LogInformation("Successfully inactivated subscription plan: {PlanId}", planId);

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Subscription plan inactivated successfully",
                Data = true
            };
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error inactivating subscription plan: {PlanId}", planId);
            throw new InvalidOperationException("An error occurred while inactivating the subscription plan", ex);
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSubscriptionPlanAsync(Guid planId)
    {
        _logger.LogInformation("Deleting subscription plan: {PlanId}", planId);

        if (planId == Guid.Empty)
        {
            throw new ArgumentException("Plan ID cannot be empty", nameof(planId));
        }

        var existingPlan = await _paymentRepository.GetSubscriptionPlanByIdAsync(planId);
        
        if (existingPlan == null)
        {
            _logger.LogWarning("Subscription plan not found for deletion: {PlanId}", planId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Subscription plan not found"
            };
        }

        try
        {
            var success = await _paymentRepository.DeleteSubscriptionPlanAsync(planId);
            
            if (!success)
            {
                _logger.LogError("Failed to delete subscription plan: {PlanId}", planId);
                throw new InvalidOperationException("Failed to delete subscription plan");
            }

            _logger.LogInformation("Successfully deleted subscription plan: {PlanId}", planId);

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Subscription plan deleted successfully",
                Data = true
            };
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting subscription plan: {PlanId}", planId);
            throw new InvalidOperationException("An error occurred while deleting the subscription plan", ex);
        }
    }
}

