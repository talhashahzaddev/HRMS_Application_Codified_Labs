using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IOnboardingConfigService
{
    /// <summary>
    /// Returns the onboarding field configuration for the given organisation.
    /// Returns null (never throws) when no configuration record exists — the
    /// controller treats a null config as "no config-driven required fields".
    /// </summary>
    //Task<OnboardingConfiguration?> GetConfigurationAsync(string orgId);
    //Task SaveConfigAsync(Guid orgId, OnboardingFieldConfigRequest request);
    //Task<OnboardingConfiguration> SaveConfigurationAsync(string orgId, OnboardingConfiguration config);
    Task<IReadOnlyList<FieldConfigRecord>> GetConfigAsync(Guid orgId);
    Task<bool> UpdateConfigAsync(Guid organizationId, UpdateOnBoardingConfigurationRequest request);
}