using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPaymentService
{
    Task<ServiceResponse<SubscriptionPlanDto>> GetSubscriptionPlanByIdAsync(Guid planId);
    Task<ServiceResponse<IReadOnlyList<SubscriptionPlanDto>>> GetAllSubscriptionPlansAsync();
    Task<ServiceResponse<IReadOnlyList<SubscriptionPlanDto>>> GetActiveSubscriptionPlansAsync();
    Task<ServiceResponse<SubscriptionPlanDto>> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request);
    Task<ServiceResponse<SubscriptionPlanDto>> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request);
    Task<ServiceResponse<bool>> InactivateSubscriptionPlanAsync(Guid planId);
    Task<ServiceResponse<bool>> DeleteSubscriptionPlanAsync(Guid planId);

    // Company Subscriptions
    Task<ServiceResponse<CompanySubscriptionDto>> GetCompanySubscriptionByIdAsync(Guid subscriptionId);
    Task<ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>> GetCompanySubscriptionsByCompanyIdAsync(Guid companyId);
    Task<ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>> GetAllCompanySubscriptionsAsync();
    Task<ServiceResponse<CompanySubscriptionDetailsDto>> GetCompanySubscriptionDetailsByCompanyIdAsync(Guid companyId);
    Task<ServiceResponse<CompanySubscriptionDto>> CreateCompanySubscriptionAsync(CreateCompanySubscriptionRequest request);
    Task<ServiceResponse<CompanySubscriptionDto>> UpdateCompanySubscriptionAsync(Guid subscriptionId, UpdateCompanySubscriptionRequest request);
    Task<ServiceResponse<bool>> DeleteCompanySubscriptionAsync(Guid subscriptionId);
}

