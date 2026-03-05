using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IDemoInquiryService
{
    Task<ServiceResponse<DemoInquiryDto>> CreateAsync(CreateDemoInquiryRequest request);
    Task<ServiceResponse<DemoInquiryDto>> GetByIdAsync(Guid inquiryId);
    Task<ServiceResponse<PagedResult<DemoInquiryDto>>> GetAllPagedAsync(DemoInquiryFilterRequest filter);
    Task<ServiceResponse<bool>> UpdateStatusAsync(Guid inquiryId, UpdateDemoInquiryStatusRequest request);
}
