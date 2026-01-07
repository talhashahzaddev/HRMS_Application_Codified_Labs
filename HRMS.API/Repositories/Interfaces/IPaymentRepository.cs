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

    // Company Subscriptions
    Task<CompanySubscriptionDto?> GetCompanySubscriptionByIdAsync(Guid subscriptionId);
    Task<IEnumerable<CompanySubscriptionDto>> GetCompanySubscriptionsByCompanyIdAsync(Guid companyId);
    Task<IEnumerable<CompanySubscriptionDto>> GetAllCompanySubscriptionsAsync();
    Task<Guid> CreateCompanySubscriptionAsync(CreateCompanySubscriptionRequest request);
    Task<bool> UpdateCompanySubscriptionAsync(Guid subscriptionId, UpdateCompanySubscriptionRequest request);
    Task<bool> DeleteCompanySubscriptionAsync(Guid subscriptionId);
}

