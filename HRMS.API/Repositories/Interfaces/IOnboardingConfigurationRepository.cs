using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces
{
    public interface IOnboardingConfigRepository
    {
        Task<IReadOnlyList<FieldConfigRecord>> GetByOrganizationAsync(Guid organizationId);
        Task<bool> UpsertConfigAsync(Guid organizationId, string section, string fieldKey, bool enabled, bool required);
        Task<bool> BulkUpsertAsync(Guid organizationId, IReadOnlyList<FieldConfigRecord> records);
        Task<bool> UpdateConfigAsync(Guid organizationId, UpdateOnBoardingConfigurationRequest request);
    }
    public interface IPolicyRepository
    {
        // Super Admin gets all (published + drafts)
        Task<IReadOnlyList<PolicyDto>> GetAllAsync(Guid organizationId);

        // Employees get only published
        Task<IReadOnlyList<PolicyDto>> GetPublishedAsync(Guid organizationId);

        Task<PolicyDto?> GetByIdAsync(Guid policyId, Guid organizationId);
        Task<PolicyDto> CreateAsync(PolicyDto policy);
        Task<bool> UpdateAsync(PolicyDto policy);
        Task<bool> DeleteAsync(Guid policyId, Guid organizationId);
    }
}
