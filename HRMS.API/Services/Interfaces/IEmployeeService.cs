using HRMS.API.Models.DTOs;
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
    Task<List<PositionDto>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null);
    Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, Guid organizationId);
}
