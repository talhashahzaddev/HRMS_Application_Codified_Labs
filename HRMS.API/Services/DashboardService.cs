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
                Id = Guid.NewGuid().ToString(),
                Type = "Employee",
                Title = "New Employee Added",
                Description = "John Doe was added to the system",
                Icon = "person_add",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                UserName = "HR Manager"
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                Type = "Leave",
                Title = "Leave Request Submitted",
                Description = "Jane Smith submitted a leave request",
                Icon = "event_available",
                Timestamp = DateTime.UtcNow.AddHours(-5),
                UserName = "Jane Smith"
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                Type = "Payroll",
                Title = "Payroll Processed",
                Description = "Payroll for March 2024 has been processed",
                Icon = "account_balance_wallet",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                UserName = "System"
            }
        };
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid organizationId)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Use existing function for now, can be optimized later
        var result = await connection.QuerySingleAsync<dynamic>(
            "SELECT * FROM admin.fn_get_dashboard_stats(@organizationId)",
            new { organizationId });

        return new DashboardSummaryDto
        {
            TotalEmployees = result.total_employees,
            PresentToday = result.present_today,
            OnLeaveToday = result.total_employees - result.present_today, // Mock calculation
            PendingApprovals = result.pending_leave_requests,
            EmployeeGrowth = 5.2m // Mock growth percentage
        };
    }

    public async Task<AttendanceStatsDto> GetAttendanceStatsAsync(Guid organizationId, string period = "month")
    {
        // Mock data for attendance statistics
        var daysInPeriod = period switch
        {
            "week" => 7,
            "month" => 30,
            "quarter" => 90,
            "year" => 365,
            _ => 30
        };

        var dates = new List<string>();
        var presentCounts = new List<int>();
        var absentCounts = new List<int>();
        var random = new Random();

        for (int i = daysInPeriod - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            dates.Add(date.ToString("yyyy-MM-dd"));
            
            var present = random.Next(80, 100);
            var absent = random.Next(5, 20);
            
            presentCounts.Add(present);
            absentCounts.Add(absent);
        }

        return new AttendanceStatsDto
        {
            Dates = dates,
            PresentCounts = presentCounts,
            AbsentCounts = absentCounts,
            AveragePresence = (decimal)presentCounts.Average()
        };
    }

    public async Task<LeaveStatsDto> GetLeaveStatsAsync(Guid organizationId, string period = "month")
    {
        // Mock data for leave statistics
        return new LeaveStatsDto
        {
            TotalRequests = 45,
            ApprovedRequests = 38,
            PendingRequests = 5,
            RejectedRequests = 2,
            LeaveTypes = new Dictionary<string, int>
            {
                { "Annual Leave", 20 },
                { "Sick Leave", 12 },
                { "Personal Leave", 8 },
                { "Maternity Leave", 3 },
                { "Emergency Leave", 2 }
            }
        };
    }

    public async Task<PayrollStatsDto> GetPayrollStatsAsync(Guid organizationId, string period = "month")
    {
        // Mock data for payroll statistics
        var payrollCosts = new List<PayrollCostDto>();
        
        for (int i = 11; i >= 0; i--)
        {
            var month = DateTime.UtcNow.AddMonths(-i);
            payrollCosts.Add(new PayrollCostDto
            {
                Month = month.ToString("yyyy-MM"),
                Amount = 450000 + (new Random().Next(-50000, 50000))
            });
        }

        return new PayrollStatsDto
        {
            TotalPayroll = 4500000,
            AverageSalary = 75000,
            PayrollCosts = payrollCosts
        };
    }

    public async Task<PerformanceStatsDto> GetPerformanceStatsAsync(Guid organizationId)
    {
        // Mock data for performance statistics
        return new PerformanceStatsDto
        {
            AverageRating = 4.2m,
            CompletedReviews = 85,
            PendingReviews = 15,
            TopPerformers = new List<TopPerformerDto>
            {
                new() { EmployeeName = "Alice Johnson", Rating = 4.9m, Department = "Engineering" },
                new() { EmployeeName = "Bob Smith", Rating = 4.8m, Department = "Sales" },
                new() { EmployeeName = "Carol Brown", Rating = 4.7m, Department = "Marketing" },
                new() { EmployeeName = "David Wilson", Rating = 4.6m, Department = "Finance" },
                new() { EmployeeName = "Eva Davis", Rating = 4.5m, Department = "HR" }
            }
        };
    }

    public async Task<EmployeeGrowthDto> GetEmployeeGrowthAsync(Guid organizationId, string period = "12m")
    {
        // Mock data for employee growth
        var months = new List<string>();
        var counts = new List<int>();
        var random = new Random();
        
        var monthsCount = period switch
        {
            "6m" => 6,
            "12m" => 12,
            "24m" => 24,
            _ => 12
        };

        var baseCount = 80;
        for (int i = monthsCount - 1; i >= 0; i--)
        {
            var month = DateTime.UtcNow.AddMonths(-i);
            months.Add(month.ToString("MMM yyyy"));
            
            baseCount += random.Next(-2, 8);
            counts.Add(baseCount);
        }

        var growthRate = counts.Count > 1 ? 
            ((decimal)(counts.Last() - counts.First()) / counts.First()) * 100 : 0;

        return new EmployeeGrowthDto
        {
            Months = months,
            Counts = counts,
            GrowthRate = Math.Round(growthRate, 1)
        };
    }

    public async Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync(Guid organizationId)
    {
        // Mock data for department statistics
        return new List<DepartmentStatsDto>
        {
            new() { DepartmentName = "Engineering", EmployeeCount = 35, PresentCount = 32, AbsentCount = 3 },
            new() { DepartmentName = "Sales", EmployeeCount = 25, PresentCount = 23, AbsentCount = 2 },
            new() { DepartmentName = "Marketing", EmployeeCount = 15, PresentCount = 14, AbsentCount = 1 },
            new() { DepartmentName = "HR", EmployeeCount = 8, PresentCount = 8, AbsentCount = 0 },
            new() { DepartmentName = "Finance", EmployeeCount = 12, PresentCount = 11, AbsentCount = 1 },
            new() { DepartmentName = "Operations", EmployeeCount = 10, PresentCount = 9, AbsentCount = 1 }
        };
    }

    public async Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(Guid organizationId, int count = 5)
    {
        // Mock data for upcoming events
        var events = new List<UpcomingEventDto>
        {
            new() {
                Id = Guid.NewGuid().ToString(),
                Title = "Team Building Event",
                Description = "Annual team building activities",
                Date = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd"),
                Time = "09:00",
                Type = "meeting"
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                Title = "John Doe's Birthday",
                Description = "Birthday celebration for John Doe",
                Date = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd"),
                Time = "15:00",
                Type = "birthday"
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                Title = "Quarterly Review Deadline",
                Description = "Submit quarterly performance reviews",
                Date = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                Time = "17:00",
                Type = "deadline"
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                Title = "Holiday - Labor Day",
                Description = "Office closed for Labor Day",
                Date = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd"),
                Time = "00:00",
                Type = "holiday"
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                Title = "Alice Johnson - Work Anniversary",
                Description = "5 years with the company",
                Date = DateTime.UtcNow.AddDays(12).ToString("yyyy-MM-dd"),
                Time = "10:00",
                Type = "work_anniversary"
            }
        };

        return events.Take(count).ToList();
    }
}
