using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPayslipTemplateService
{
    Task<ServiceResponse<PayslipTemplateDto>> GetActiveTemplateAsync(Guid organizationId);
    Task<ServiceResponse<PayslipTemplateDto>> UpsertActiveTemplateAsync(Guid organizationId, UpsertPayslipTemplateRequest request, Guid? updatedBy);
    Task<byte[]> BuildPreviewPdfAsync(Guid organizationId, UpsertPayslipTemplateRequest? draftTemplate, string currencyLabel);
}
