using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces
{
    public interface IOrganizationRepository
    {
        Task<Organization> CreateOrganizationAsync(Organization organization);
        Task<Organization?> GetOrganizationByIdAsync(Guid organizationId);
        Task<Organization?> GetOrganizationByNameAsync(string name);
        Task<bool> UpdateOrganizationAsync(Organization organization);
        Task<bool> DeleteOrganizationAsync(Guid organizationId);
        Task<Organization?> GetOrganizationByDomainAsync(string domain);
        Task<IReadOnlyList<Organization>> GetAllOrganizationsAsync();
    }
}