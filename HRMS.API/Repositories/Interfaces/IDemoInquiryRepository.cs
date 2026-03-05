using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IDemoInquiryRepository
{
    Task<Guid> CreateAsync(DemoInquiry inquiry);
    Task<DemoInquiry?> GetByIdAsync(Guid inquiryId);
    Task<PagedResult<DemoInquiryDto>> GetAllPagedAsync(DemoInquiryFilterRequest filter);
    Task<bool> UpdateStatusAsync(Guid inquiryId, string status, string? notes);
    Task<int> GetCountByStatusAsync(string? status = null);
}
