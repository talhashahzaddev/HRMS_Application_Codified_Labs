using System.Text.Json.Serialization;

namespace HRMS.API.Models.DTOs;

public class PagedResult<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;
}

public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public string? Errors { get; set; }
}

public class DashboardStatsDto
{
    public long TotalEmployees { get; set; }
    public long ActiveEmployees { get; set; }
    public long PendingLeaveRequests { get; set; }
    public long PresentToday { get; set; }
    public long TotalDepartments { get; set; }
    public List<DepartmentStatsDto> DepartmentStats { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class DepartmentStatsDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal Percentage { get; set; }
    public int ActiveEmployees { get; set; }
    public decimal? AverageSalary { get; set; }
}

public class RecentActivityDto
{
    //public string Id { get; set; } = string.Empty;

    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = string.Empty;
}

// Dashboard-specific DTOs
public class DashboardSummaryDto
{
    public long? TotalEmployees { get; set; }
    public long? PresentToday { get; set; }
    public long? OnLeaveToday { get; set; }
    public long? PendingApprovals { get; set; }
    public decimal? EmployeeGrowth { get; set; }
}

public class AttendanceStatsDto
{
    public List<string> Dates { get; set; } = new();
    public List<int> PresentCounts { get; set; } = new();
    public List<int> AbsentCounts { get; set; } = new();
    public decimal AveragePresence { get; set; }
}

public class LeaveStatsDto
{
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int PendingRequests { get; set; }
    public int RejectedRequests { get; set; }
    public Dictionary<string, int> LeaveTypes { get; set; } = new();
}

public class PayrollStatsDto
{
    public decimal TotalPayroll { get; set; }
    public decimal AverageSalary { get; set; }
    public List<PayrollCostDto> PayrollCosts { get; set; } = new();
}

public class PayrollCostDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PerformanceStatsDto
{
    public decimal AverageRating { get; set; }
    public int CompletedReviews { get; set; }
    public int PendingReviews { get; set; }
    public List<TopPerformerDto> TopPerformers { get; set; } = new();
}



public class EmployeeGrowthDto
{
    public List<string> Months { get; set; } = new();
    public List<int> Counts { get; set; } = new();
    public decimal GrowthRate { get; set; }
    public List<int> CumulativeEmployees { get; set; } = new();
    public List<int> NewEmployees { get; set; } = new();
}

public class UpcomingEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
