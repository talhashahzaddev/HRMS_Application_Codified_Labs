using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces
{
    public interface IOnboardingConfigService
    {
        Task<OnboardingFieldConfigResponse> GetConfigAsync(Guid organizationId);
        Task<bool> SaveConfigAsync(Guid organizationId, OnboardingFieldConfigRequest request);
        Task<OnboardingFieldConfigResponse> GetDefaultConfig();
    }
}
