using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.DTOs;


public class EmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public Dictionary<string, object> emergencycontact { get; set; } = new();
    public string gender { get; set; } = string.Empty;
    public string nationality { get; set; } = string.Empty;
    public string maritalstatus { get; set; } = string.Empty;
    public string WorkLocation { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PositionTitle { get; set; }
    public decimal? BasicSalary { get; set; }
    public Guid ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? DepartmentId { get; set; }       // ? Added
    public Guid? PositionId { get; set; }
}

public class EmployeeSimpleDto
{
    public Guid EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; } 
    public string EmployeeCode { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty; 
    public string ReportingManagerName { get; set; } = string.Empty;


}



public class EmployeeListItemDto
{
    public Guid? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
    public string? FullName { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Status { get; set; } = string.Empty;

    // Existing fields
    public bool? IsActive { get; set; }
    public string? employmentType { get; set; }

    // ? New ID fields
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? PositionId { get; set; }
    public string? PositionTitle { get; set; }

    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string? PayType { get; set; }
    public decimal? BasicSalary { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; } = string.Empty;

    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? MaritalStatus { get; set; }
    public DateTime? DateOfBirth { get; set; }
    // ? NEW FIELDS
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }

    // JSON fields
    public Dictionary<string, string>? Address { get; set; }
    public Dictionary<string, string>? EmergencyContact { get; set; }
}





public class CreateEmployeeRequest
{
    public string? EmployeeNumber { get; set; }
    public string? FirstName { get; set; } 
    public string? LastName { get; set; } 
    public string? Email { get; set; } 
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public decimal? BasicSalary { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string? WorkLocation { get; set; }
    public string employmentType { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
    public Dictionary<string, object> EmergencyContact { get; set; } = new();
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

public class UpdateEmployeeRequestDTo
{
    public Guid EmployeeId { get; set; }   // ? REQUIRED

    public string? EmployeeNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public Dictionary<string, object> Address { get; set; } = new();
    public Dictionary<string, object> EmergencyContact { get; set; } = new();

    public DateTime? DateOfBirth { get; set; }
    public DateTime? HireDate { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public decimal? BasicSalary { get; set; }
    public Guid? ReportingManagerId { get; set; }

    public string? WorkLocation { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Nationality { get; set; }

    public Guid? roleId { get; set; }
    public IFormFile? ProfilePicture { get; set; }
    public string? EmploymentType { get; set; }   // Full-Time, Part-Time, Contract, Intern
    public string? PayType { get; set; }

}


public class DepartmentDto
{
    public Guid DepartmentId { get; set; }
    public Guid OrganizationId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime? CreatedAt { get; set; }
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
    public Guid OrganizationId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Description { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}




public class PositionEmployeeDto
{
    public Guid PositionId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; } // Optional if you want hire date
    public string PositionTitle { get; set; } = string.Empty;
}

public class PositionEmployeesMainDto
{
    public Guid PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public List<PositionEmployeeDto> Employees { get; set; } = new();
}






public class CreatePositionRequest
{
    public string PositionTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? RoleId { get; set; }
}

public class EmployeeStatisticsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int NewEmployeesThisMonth { get; set; }
    public int EmployeesTurnedoverThisMonth { get; set; }
    public decimal AverageTenure { get; set; }
    public List<DepartmentStatsDto> DepartmentBreakdown { get; set; } = new();
    public List<EmploymentTypeStatsDto> EmploymentTypeBreakdown { get; set; } = new();
}

public class EmploymentTypeStatsDto
{
    public string EmploymentType { get; set; } = string.Empty;
    public int Count { get; set; }
    public int EmployeeCount { get; set; }
    public decimal Percentage { get; set; }
}

public class BulkEmployeeRequest
{
    public List<Guid> EmployeeIds { get; set; } = new();
}