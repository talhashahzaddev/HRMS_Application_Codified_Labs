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
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetDashboardStatsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetDashboardStatsAsync(organizationId);
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid organizationId, int count = 10)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetRecentActivitiesAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (count <= 0 || count > 100)
        {
            _logger.LogWarning("Invalid count {Count} for GetRecentActivitiesAsync", count);
            throw new ArgumentException("Count must be between 1 and 100", nameof(count));
        }
        return await _dashboardRepository.GetRecentActivitiesAsync(organizationId, count);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetDashboardSummaryAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetDashboardSummaryAsync(organizationId);
    }

    public async Task<AttendanceStatsDto> GetAttendanceStatsAsync(Guid organizationId, string period = "month")
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetAttendanceStatsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (!IsValidStatsPeriod(period))
        {
            _logger.LogWarning("Invalid period {Period} for GetAttendanceStatsAsync", period);
            throw new ArgumentException("Invalid period. Allowed values: week, month, quarter, year", nameof(period));
        }
        return await _dashboardRepository.GetAttendanceStatsAsync(organizationId, period);
    }

    public async Task<LeaveStatsDto> GetLeaveStatsAsync(Guid organizationId, string period = "month")
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetLeaveStatsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (!IsValidStatsPeriod(period))
        {
            _logger.LogWarning("Invalid period {Period} for GetLeaveStatsAsync", period);
            throw new ArgumentException("Invalid period. Allowed values: week, month, quarter, year", nameof(period));
        }
        return await _dashboardRepository.GetLeaveStatsAsync(organizationId, period);
    }

    public async Task<PerformanceStatsDto> GetPerformanceStatsAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetPerformanceStatsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetPerformanceStatsAsync(organizationId);
    }

    public async Task<EmployeeGrowthDto> GetEmployeeGrowthAsync(Guid organizationId, string period = "12m")
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetEmployeeGrowthAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (!IsValidGrowthPeriod(period))
        {
            _logger.LogWarning("Invalid period {Period} for GetEmployeeGrowthAsync", period);
            throw new ArgumentException("Invalid period. Allowed values: 3m, 6m, 12m, 24m", nameof(period));
        }
        return await _dashboardRepository.GetEmployeeGrowthAsync(organizationId, period);
    }

    public async Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetDepartmentStatsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetDepartmentStatsAsync(organizationId);
    }

    public async Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(Guid organizationId, int count = 5)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetUpcomingEventsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (count <= 0 || count > 100)
        {
            _logger.LogWarning("Invalid count {Count} for GetUpcomingEventsAsync", count);
            throw new ArgumentException("Count must be between 1 and 100", nameof(count));
        }
        return await _dashboardRepository.GetUpcomingEventsAsync(organizationId, count);
    }

    public async Task<object> GetAttendanceTrendsAsync(Guid organizationId, string period = "30d")
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetAttendanceTrendsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (!IsValidTrendPeriod(period))
        {
            _logger.LogWarning("Invalid period {Period} for GetAttendanceTrendsAsync", period);
            throw new ArgumentException("Invalid period. Allowed values: 7d, 30d, 90d", nameof(period));
        }
        return await _dashboardRepository.GetAttendanceTrendsAsync(organizationId, period);
    }

    public async Task<object> GetLeaveTrendsAsync(Guid organizationId, string period = "12m")
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetLeaveTrendsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (!IsValidGrowthPeriod(period))
        {
            _logger.LogWarning("Invalid period {Period} for GetLeaveTrendsAsync", period);
            throw new ArgumentException("Invalid period. Allowed values: 3m, 6m, 12m, 24m", nameof(period));
        }
        return await _dashboardRepository.GetLeaveTrendsAsync(organizationId, period);
    }

    public async Task<List<object>> GetTopPerformersAsync(Guid organizationId, int limit = 5)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetTopPerformersAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        if (limit <= 0 || limit > 100)
        {
            _logger.LogWarning("Invalid limit {Limit} for GetTopPerformersAsync", limit);
            throw new ArgumentException("Limit must be between 1 and 100", nameof(limit));
        }
        return await _dashboardRepository.GetTopPerformersAsync(organizationId, limit);
    }

    public async Task<object> GetRemindersAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetRemindersAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetRemindersAsync(organizationId);
    }

    public async Task<object> GetQuickStatsAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetQuickStatsAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetQuickStatsAsync(organizationId);
    }

    public async Task<OnboardingStatusDto> GetOnboardingStatusAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Invalid organizationId for GetOnboardingStatusAsync");
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }
        return await _dashboardRepository.GetOnboardingStatusAsync(organizationId);
    }

    private static bool IsValidStatsPeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return false;
        var p = period.Trim().ToLowerInvariant();
        return p is "week" or "month" or "quarter" or "year";
    }

    private static bool IsValidGrowthPeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return false;
        var p = period.Trim().ToLowerInvariant();
        return p is "3m" or "6m" or "12m" or "24m";
    }

    private static bool IsValidTrendPeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return false;
        var p = period.Trim().ToLowerInvariant();
        return p is "7d" or "30d" or "90d";
    }
}
