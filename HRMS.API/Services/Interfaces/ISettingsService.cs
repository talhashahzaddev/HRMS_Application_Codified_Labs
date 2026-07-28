using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ISettingsService
{
    Task<ServiceResponse<OrganizationSettingsDto>> GetOrganizationSettingsAsync(Guid organizationId);
    Task<ServiceResponse<bool>> UpdateCurrencyAsync(Guid organizationId, string currency);
    Task<ServiceResponse<bool>> UpdateCareerPageAsync(Guid organizationId, UpdateCareerPageRequest request);
    Task<ServiceResponse<CareerPageDto>> GetCareerPageAsync(Guid organizationId);
    Task<ServiceResponse<bool>> UpdateLocalizationAsync(Guid organizationId,string currency,string timeZone,string culture);
    Task<ServiceResponse<List<CompanyPolicyDto>>> GetPoliciesAsync(Guid organizationId);
    Task<ServiceResponse<CompanyPolicyDto>> GetPolicyByIdAsync(Guid organizationId, Guid policyId);
    Task<ServiceResponse<CompanyPolicyDto>> CreatePolicyAsync(Guid organizationId, CreateCompanyPolicyRequest request, string createdBy);
    Task<ServiceResponse<CompanyPolicyDto>> UpdatePolicyAsync(Guid organizationId, Guid policyId, UpdateCompanyPolicyRequest request, string updatedBy);
    Task<ServiceResponse<bool>> DeletePolicyAsync(Guid organizationId, Guid policyId);
}










