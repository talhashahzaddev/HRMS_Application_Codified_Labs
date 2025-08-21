using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(Guid organizationId);
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid organizationId, int count = 10);
}
