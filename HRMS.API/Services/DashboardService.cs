using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        ILogger<DashboardService> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid organizationId)
    {
        return await _dashboardRepository.GetDashboardStatsAsync(organizationId);
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid organizationId, int count = 10)
    {
        return await _dashboardRepository.GetRecentActivitiesAsync(organizationId, count);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid organizationId)
    {
        return await _dashboardRepository.GetDashboardSummaryAsync(organizationId);
    }

    public async Task<AttendanceStatsDto> GetAttendanceStatsAsync(Guid organizationId, string period = "month")
    {
        return await _dashboardRepository.GetAttendanceStatsAsync(organizationId, period);
    }

    public async Task<LeaveStatsDto> GetLeaveStatsAsync(Guid organizationId, string period = "month")
    {
        return await _dashboardRepository.GetLeaveStatsAsync(organizationId, period);
    }

    public async Task<PayrollStatsDto> GetPayrollStatsAsync(Guid organizationId, string period = "month")
    {
        return await _dashboardRepository.GetPayrollStatsAsync(organizationId, period);
    }

    public async Task<PerformanceStatsDto> GetPerformanceStatsAsync(Guid organizationId)
    {
        return await _dashboardRepository.GetPerformanceStatsAsync(organizationId);
    }

    public async Task<EmployeeGrowthDto> GetEmployeeGrowthAsync(Guid organizationId, string period = "12m")
    {
        return await _dashboardRepository.GetEmployeeGrowthAsync(organizationId, period);
    }

    public async Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync(Guid organizationId)
    {
        return await _dashboardRepository.GetDepartmentStatsAsync(organizationId);
    }

    public async Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(Guid organizationId, int count = 5)
    {
        return await _dashboardRepository.GetUpcomingEventsAsync(organizationId, count);
    }

    public async Task<object> GetAttendanceTrendsAsync(Guid organizationId, string period = "30d")
    {
        return await _dashboardRepository.GetAttendanceTrendsAsync(organizationId, period);
    }

    public async Task<object> GetLeaveTrendsAsync(Guid organizationId, string period = "12m")
    {
        return await _dashboardRepository.GetLeaveTrendsAsync(organizationId, period);
    }

    public async Task<object> GetPayrollTrendsAsync(Guid organizationId, string period = "12m")
    {
        return await _dashboardRepository.GetPayrollTrendsAsync(organizationId, period);
    }

    public async Task<List<object>> GetTopPerformersAsync(Guid organizationId, int limit = 5)
    {
        return await _dashboardRepository.GetTopPerformersAsync(organizationId, limit);
    }

    public async Task<object> GetRemindersAsync(Guid organizationId)
    {
        return await _dashboardRepository.GetRemindersAsync(organizationId);
    }

    public async Task<object> GetQuickStatsAsync(Guid organizationId)
    {
        return await _dashboardRepository.GetQuickStatsAsync(organizationId);
    }
}
