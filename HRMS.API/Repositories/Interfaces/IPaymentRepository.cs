using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<SubscriptionPlanDto?> GetSubscriptionPlanByIdAsync(Guid planId);
    Task<IEnumerable<SubscriptionPlanDto>> GetAllSubscriptionPlansAsync();
    Task<IEnumerable<SubscriptionPlanDto>> GetActiveSubscriptionPlansAsync();
    Task<Guid> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request);
    Task<bool> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request);
    Task<bool> InactivateSubscriptionPlanAsync(Guid planId);
    Task<bool> DeleteSubscriptionPlanAsync(Guid planId);
}

