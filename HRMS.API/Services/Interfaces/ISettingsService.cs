using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ISettingsService
{
    Task<ServiceResponse<OrganizationSettingsDto>> GetOrganizationSettingsAsync(Guid organizationId);
    Task<ServiceResponse<bool>> UpdateCurrencyAsync(Guid organizationId, string currency);
}






