namespace HRMS.API.Models.DTOs;

public class EmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PositionTitle { get; set; }
    public decimal? BasicSalary { get; set; }
    public string? ManagerName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmployeeListItemDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime HireDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PositionTitle { get; set; }
}

public class CreateEmployeeRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public decimal? BasicSalary { get; set; }
    public Guid? ReportingManagerId { get; set; }
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DepartmentDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
}

public class CreateDepartmentRequest
{
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
}

public class PositionDto
{
    public Guid PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
}

public class CreatePositionRequest
{
    public string PositionTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
}
