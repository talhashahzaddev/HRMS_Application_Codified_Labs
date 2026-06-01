using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces
{
    public interface IOnboardingConfigRepository
    {
        Task<List<FieldConfigRecord>> GetOnBoadingConfigurations(Guid organizationId);
        Task<bool> UpsertConfigAsync(Guid organizationId, string section, string fieldKey, bool enabled, bool required);
        Task<bool> BulkUpsertAsync(Guid organizationId, List<FieldConfigRecord> records);
    }
    public interface IPolicyRepository
    {
        // Super Admin gets all (published + drafts)
        Task<List<PolicyDto>> GetAllAsync(Guid organizationId);

        // Employees get only published
        Task<List<PolicyDto>> GetPublishedAsync(Guid organizationId);

        Task<PolicyDto?> GetByIdAsync(Guid policyId, Guid organizationId);
        Task<PolicyDto> CreateAsync(PolicyDto policy);
        Task<bool> UpdateAsync(PolicyDto policy);
        Task<bool> DeleteAsync(Guid policyId, Guid organizationId);
    }
}
