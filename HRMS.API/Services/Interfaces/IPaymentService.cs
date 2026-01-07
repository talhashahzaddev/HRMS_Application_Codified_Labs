using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPaymentService
{
    Task<ServiceResponse<SubscriptionPlanDto>> GetSubscriptionPlanByIdAsync(Guid planId);
    Task<ServiceResponse<IEnumerable<SubscriptionPlanDto>>> GetAllSubscriptionPlansAsync();
    Task<ServiceResponse<IEnumerable<SubscriptionPlanDto>>> GetActiveSubscriptionPlansAsync();
    Task<ServiceResponse<SubscriptionPlanDto>> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request);
    Task<ServiceResponse<SubscriptionPlanDto>> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request);
    Task<ServiceResponse<bool>> InactivateSubscriptionPlanAsync(Guid planId);
    Task<ServiceResponse<bool>> DeleteSubscriptionPlanAsync(Guid planId);

    // Company Subscriptions
    Task<ServiceResponse<CompanySubscriptionDto>> GetCompanySubscriptionByIdAsync(Guid subscriptionId);
    Task<ServiceResponse<IEnumerable<CompanySubscriptionDto>>> GetCompanySubscriptionsByCompanyIdAsync(Guid companyId);
    Task<ServiceResponse<IEnumerable<CompanySubscriptionDto>>> GetAllCompanySubscriptionsAsync();
    Task<ServiceResponse<CompanySubscriptionDto>> CreateCompanySubscriptionAsync(CreateCompanySubscriptionRequest request);
    Task<ServiceResponse<CompanySubscriptionDto>> UpdateCompanySubscriptionAsync(Guid subscriptionId, UpdateCompanySubscriptionRequest request);
    Task<ServiceResponse<bool>> DeleteCompanySubscriptionAsync(Guid subscriptionId);
}

