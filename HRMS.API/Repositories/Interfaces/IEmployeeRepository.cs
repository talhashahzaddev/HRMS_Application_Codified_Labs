using HRMS.API.Models.Entities;
using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task<Employee?> GetEmployeeByIdAsync(Guid employeeId);
    Task<Employee?> GetEmployeeByCodeAsync(Guid organizationId, string employeeCode);
    Task<Employee?> GetEmployeeByEmailAsync(Guid organizationId, string email);
    Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(Guid organizationId, string? searchTerm = null, Guid? departmentId = null, string? status = null, int page = 1, int pageSize = 50);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(Guid employeeId);
    Task<List<Department>> GetDepartmentsAsync(Guid organizationId);
    Task<Department> CreateDepartmentAsync(Department department);
    Task<Department?> GetDepartmentByIdAsync(Guid departmentId);
    Task<List<Position>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null);
    Task<Position> CreatePositionAsync(Position position);
    Task<Position?> GetPositionByIdAsync(Guid positionId);
}
