using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using System.Data;
using Dapper;
using HRMS.API.Configuration;

namespace HRMS.API.Services;

public class DashboardService : IDashboardService
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDatabaseConnectionFactory connectionFactory,
        ILogger<DashboardService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid organizationId)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var result = await connection.QuerySingleAsync<dynamic>(
            "SELECT * FROM admin.fn_get_dashboard_stats(@organizationId)",
            new { organizationId });

        return new DashboardStatsDto
        {
            TotalEmployees = result.total_employees,
            ActiveEmployees = result.active_employees,
            PendingLeaveRequests = result.pending_leave_requests,
            PresentToday = result.present_today,
            TotalDepartments = result.total_departments
        };
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid organizationId, int count = 10)
    {
        // For now, return mock data - in a real implementation, this would query audit logs
        return new List<RecentActivityDto>
        {
            new() {
                Type = "Employee",
                Description = "New employee John Doe was added to the system",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                UserName = "HR Manager"
            },
            new() {
                Type = "Leave",
                Description = "Leave request submitted by Jane Smith",
                Timestamp = DateTime.UtcNow.AddHours(-5),
                UserName = "Jane Smith"
            },
            new() {
                Type = "Payroll",
                Description = "Payroll for March 2024 has been processed",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                UserName = "System"
            }
        };
    }
}
