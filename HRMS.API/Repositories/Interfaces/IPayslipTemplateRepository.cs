using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayslipTemplateRepository
{
    Task<PayslipTemplateDto?> GetActiveTemplateAsync(Guid organizationId);
    Task<string?> GetOrganizationLogoUrlAsync(Guid organizationId);
    Task<Guid?> UpsertActiveTemplateAsync(Guid organizationId, UpsertPayslipTemplateRequest request, Guid? updatedBy);
}
