using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public OrganizationService(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<OrganizationDTO>> CreateOrganizationAsync(CreateOrganizationDTO createOrganizationDTO)
        {
            var organization = _mapper.Map<Organization>(createOrganizationDTO);
            var createdOrganization = await _organizationRepository.CreateOrganizationAsync(organization);
            var organizationDTO = _mapper.Map<OrganizationDTO>(createdOrganization);
            return new ServiceResponse<OrganizationDTO> { Data = organizationDTO, Success = true, Message = "Organization created successfully." };
        }

        public async Task<ServiceResponse<OrganizationDTO>> GetOrganizationByIdAsync(Guid organizationId)
        {
            var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
            {
                return new ServiceResponse<OrganizationDTO> { Success = false, Message = "Organization not found." };
            }
            var organizationDTO = _mapper.Map<OrganizationDTO>(organization);
            return new ServiceResponse<OrganizationDTO> { Data = organizationDTO, Success = true };
        }

        public async Task<ServiceResponse<OrganizationDTO>> GetOrganizationByNameAsync(string name)
        {
            var organization = await _organizationRepository.GetOrganizationByNameAsync(name);
            if (organization == null)
            {
                return new ServiceResponse<OrganizationDTO> { Success = false, Message = "Organization not found." };
            }
            var organizationDTO = _mapper.Map<OrganizationDTO>(organization);
            return new ServiceResponse<OrganizationDTO> { Data = organizationDTO, Success = true };
        }

        public async Task<ServiceResponse<OrganizationDTO>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationDTO updateOrganizationDTO)
        {
            var existingOrganization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
            if (existingOrganization == null)
            {
                return new ServiceResponse<OrganizationDTO> { Success = false, Message = "Organization not found." };
            }

            _mapper.Map(updateOrganizationDTO, existingOrganization);
            existingOrganization.OrganizationId = organizationId; // Ensure ID is not changed by mapping

            var success = await _organizationRepository.UpdateOrganizationAsync(existingOrganization);
            if (!success)
            {
                return new ServiceResponse<OrganizationDTO> { Success = false, Message = "Failed to update organization." };
            }

            var updatedOrganizationDTO = _mapper.Map<OrganizationDTO>(existingOrganization);
            return new ServiceResponse<OrganizationDTO> { Data = updatedOrganizationDTO, Success = true, Message = "Organization updated successfully." };
        }

        public async Task<ServiceResponse<bool>> DeleteOrganizationAsync(Guid organizationId)
        {
            var success = await _organizationRepository.DeleteOrganizationAsync(organizationId);
            if (!success)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Failed to delete organization." };
            }
            return new ServiceResponse<bool> { Data = true, Success = true, Message = "Organization deleted successfully." };
        }

        public async Task<ServiceResponse<OrganizationDTO>> GetOrganizationByDomainAsync(string domain)
        {
            var organization = await _organizationRepository.GetOrganizationByDomainAsync(domain);
            if (organization == null)
            {
                return new ServiceResponse<OrganizationDTO> { Success = false, Message = "Organization not found." };
            }
            var organizationDTO = _mapper.Map<OrganizationDTO>(organization);
            return new ServiceResponse<OrganizationDTO> { Data = organizationDTO, Success = true };
        }

        public async Task<ServiceResponse<IEnumerable<OrganizationDTO>>> GetAllOrganizationsAsync()
        {
            var organizations = await _organizationRepository.GetAllOrganizationsAsync();
            var organizationDTOs = _mapper.Map<IEnumerable<OrganizationDTO>>(organizations);
            return new ServiceResponse<IEnumerable<OrganizationDTO>> { Data = organizationDTOs, Success = true };
        }
    }


}