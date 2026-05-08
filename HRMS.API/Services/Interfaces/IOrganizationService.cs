using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Services.Interfaces
{
    public interface IOrganizationService
    {
        Task<ServiceResponse<OrganizationDTO>> CreateOrganizationAsync(CreateOrganizationDTO createOrganizationDTO);
        Task<ServiceResponse<OrganizationDTO>> GetOrganizationByIdAsync(Guid organizationId);
        Task<ServiceResponse<OrganizationDTO>> GetOrganizationByNameAsync(string name);
        Task<ServiceResponse<OrganizationDTO>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationDTO updateOrganizationDTO);
        Task<ServiceResponse<bool>> DeleteOrganizationAsync(Guid organizationId);
        Task<ServiceResponse<OrganizationDTO>> GetOrganizationByDomainAsync(string domain);
        Task<ServiceResponse<IEnumerable<OrganizationDTO>>> GetAllOrganizationsAsync();
    }
}