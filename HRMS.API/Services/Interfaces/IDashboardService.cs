using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(Guid organizationId);
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid organizationId, int count = 10);
    
    // New dashboard methods
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid organizationId);
    Task<AttendanceStatsDto> GetAttendanceStatsAsync(Guid organizationId, string period = "month");
    Task<LeaveStatsDto> GetLeaveStatsAsync(Guid organizationId, string period = "month");
    Task<PayrollStatsDto> GetPayrollStatsAsync(Guid organizationId, string period = "month");
    Task<PerformanceStatsDto> GetPerformanceStatsAsync(Guid organizationId);
    Task<EmployeeGrowthDto> GetEmployeeGrowthAsync(Guid organizationId, string period = "12m");
    Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync(Guid organizationId);
    Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(Guid organizationId, int count = 5);
    Task<object> GetAttendanceTrendsAsync(Guid organizationId, string period = "30d");
    Task<object> GetLeaveTrendsAsync(Guid organizationId, string period = "12m");
    Task<object> GetPayrollTrendsAsync(Guid organizationId, string period = "12m");
    Task<List<object>> GetTopPerformersAsync(Guid organizationId, int limit = 5);
    Task<object> GetRemindersAsync(Guid organizationId);
    Task<object> GetQuickStatsAsync(Guid organizationId);
}
