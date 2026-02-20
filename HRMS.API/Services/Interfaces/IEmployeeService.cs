using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Services.Interfaces;

public interface IEmployeeService
{
    Task<PositionEmployeesMainDto> GetEmployeesByPositionAsync(Guid organizationId, Guid positionId);
    Task<bool> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequestDTo request, IFormFile? profileFile);
    Task<bool> UpdateDepartmentStatusAsync(Guid organizationId, Guid departmentId, bool isActive);
    Task<bool> UpdatePositionStatusAsync(Guid organizationId,Guid positionId,bool isActive);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, Guid organizationId);
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid userId);
    /// <summary>Gets employee by employee ID (no userId lookup). Use when you already have the employee ID.</summary>
    Task<EmployeeDto?> GetEmployeeByEmployeeIdAsync(Guid employeeId);
    Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(EmployeeSearchViewModel searchModel, Guid organizationId);
    Task<bool> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request);
    Task<bool> DeleteEmployeeAsync(Guid employeeId,string status);
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid organizationId, string? searchQuery = null, bool? status = null);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, Guid organizationId);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid departmentId, CreateDepartmentRequest request);
    Task<bool> DeleteDepartmentAsync(Guid departmentId);
    Task<List<PositionDto>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null, string? search = null,string? status = null);
    Task<List<PositionDto>> GetPositionsAsync(Guid organizationId);
    Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, Guid organizationId);
    Task<PositionDto> UpdatePositionAsync(Guid positionId, CreatePositionRequest request);
    Task<bool> DeletePositionAsync(Guid positionId);
    
    // New missing methods
    Task<EmployeeDto> ActivateEmployeeAsync(Guid employeeId);
    Task<EmployeeDto> DeactivateEmployeeAsync(Guid employeeId);
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid departmentId);
    Task<PositionDto?> GetPositionByIdAsync(Guid positionId);
    Task<EmployeeStatisticsDto> GetEmployeeStatisticsAsync(Guid organizationId);
    Task<List<EmployeeDto>> GetManagersAsync(Guid organizationId);
    Task<bool> BulkActivateEmployeesAsync(List<Guid> employeeIds);
    Task<bool> BulkDeactivateEmployeesAsync(List<Guid> employeeIds);
    Task<byte[]> ExportEmployeesAsync(Guid organizationId, string format);
    Task<List<EmployeeListItemDto>> GetEmployeesByManagerAsync(Guid managerEmployeeId, Guid organizationId);
}
