using HRMS.API.Models.DTOs;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Services.Interfaces
{
    public interface INewsService
    {
        Task<NewsDto> CreateNewsAsync(Guid employeeId, Guid organizationId, NewsRequest request);
        Task<PagedResult<NewsDto>> GetAllNewsAsync(Guid employeeId, Guid organizationId, NewsSearchViewModel searchModel);
        Task<Guid> GetEmployeeFromTable(Guid Userid);
        Task<bool> UpdateNewsAsync(Guid newsId, Guid employeeId, Guid organizationId, NewsRequest request);
        Task<NewsDto> GetNewsByNewsIdAsync(Guid newsId);
        Task<bool> DeleteNewsAsync(Guid newsId);
        Task<bool> AddNewsViewAsync(Guid newsId);
        Task<bool> MarkNewsAsReadAsync(Guid newsId, Guid employeeId);
        Task<bool> UpdateNewsStatusAsync(Guid newsId, string status);
    }
}
