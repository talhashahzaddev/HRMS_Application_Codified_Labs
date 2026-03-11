using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ISettingsService
{
    Task<ServiceResponse<OrganizationSettingsDto>> GetOrganizationSettingsAsync(Guid organizationId);
    Task<ServiceResponse<bool>> UpdateCurrencyAsync(Guid organizationId, string currency);
    Task<ServiceResponse<bool>> UpdateCareerPageAsync(Guid organizationId, UpdateCareerPageRequest request);
    Task<ServiceResponse<CareerPageDto>> GetCareerPageAsync(Guid organizationId);
    Task<ServiceResponse<bool>> UpdateLocalizationAsync(Guid organizationId,string currency,string timeZone,string culture);
}










