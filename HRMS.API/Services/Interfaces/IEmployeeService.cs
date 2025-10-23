using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;

namespace HRMS.API.Services.Interfaces;

public interface IEmployeeService
{

    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, Guid organizationId);
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid employeeId);
    Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(EmployeeSearchViewModel searchModel, Guid organizationId);
    Task<bool> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request);
    Task<bool> DeleteEmployeeAsync(Guid employeeId);
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid organizationId);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, Guid organizationId);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid departmentId, CreateDepartmentRequest request);
    Task<bool> DeleteDepartmentAsync(Guid departmentId);
    Task<List<PositionDto>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null);
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
}
