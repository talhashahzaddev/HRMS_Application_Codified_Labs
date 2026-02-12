using HRMS.API.Models.DTOs;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Repositories.Interfaces
{
    public interface INewsRepository
    {
        Task<NewsDto> CreateNewsAsync(Guid createdBy, Guid organizationId, NewsRequest news);
        Task<PagedResult<NewsDto>> GetAllNewsAdminAsync(Guid employeeId, Guid OrganizationId, NewsSearchViewModel searchModel);
        Task<PagedResult<NewsDto>> GetAllEmployeeAsync(Guid employeeId, Guid OrganizationId, NewsSearchViewModel searchModel);
        Task<bool> UpdateNewsAsync(Guid newsId, Guid employeeId, Guid organizationId, NewsRequest request);
        Task<NewsDto> GetNewsByNewsIdAsync(Guid newsId);
        Task<bool> DeleteNewsAsync(Guid newsId);
        Task<bool> IncrementNewsViewAsync(Guid newsId);
        Task<bool> MarkNewsAsReadAsync(Guid newsId, Guid employeeId);
        Task<bool> UpdateNewsStatusAsync(Guid newsId, string status);
    }
}
