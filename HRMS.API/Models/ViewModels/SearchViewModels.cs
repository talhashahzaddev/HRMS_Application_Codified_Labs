using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Models.ViewModels;

public class EmployeeSearchViewModel
{
    public string? SearchTerm { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }      // Add this
    public string? EmploymentType { get; set; } // Add this
    [FromQuery(Name = "employmentStatus")]
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}

public class AttendanceSearchViewModel
{
    public string? SearchTerm { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    [FromQuery(Name="departmentId")]
    public Guid? departmentid { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class LeaveRequestSearchViewModel
{
    public Guid? EmployeeId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class TeamRemainingLeavesSearchViewModel
{
    public string? EmployeeName { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}

public class PayrollSearchViewModel
{
    public Guid? PeriodId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AssetSearchViewModel
{
    public Guid? AssetTypeId { get; set; }
    public string? Status { get; set; }
}
