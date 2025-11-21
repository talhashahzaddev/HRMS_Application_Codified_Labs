using HRMS.API.Models.Entities;
using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IEmployeeRepository
{

    //adding this now
    Task<EmployeeDto> GetEmployeedetailsAsync(Guid employeeId);

    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task<Employee?> GetEmployeeByIdAsync(Guid employeeId);
    Task<Employee?> GetEmployeeByCodeAsync(Guid organizationId, string employeeCode);
    Task<Employee?> GetEmployeeByEmailAsync(Guid organizationId, string email);
    Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(
    Guid? organizationId = null, string? searchTerm = null, Guid? departmentId = null,
    string? status = null, int page = 1, int pageSize = 50);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(Guid employeeId);
    Task<List<Department>> GetDepartmentsAsync(Guid organizationId);
    Task<Department> CreateDepartmentAsync(Department department);
    Task<Department?> GetDepartmentByIdAsync(Guid departmentId);
    Task<Department> UpdateDepartmentAsync(Department department);
    Task<bool> DeleteDepartmentAsync(Guid departmentId);
    Task<int> GetEmployeeCountByDepartmentAsync(Guid departmentId);
    Task<List<Position>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null);
    Task<Position> CreatePositionAsync(Position position);
    Task<Position?> GetPositionByIdAsync(Guid positionId);
    Task<Position> UpdatePositionAsync(Position position);
    Task<bool> DeletePositionAsync(Guid positionId);
    Task<int> GetEmployeeCountByPositionAsync(Guid positionId);
    Task<List<Employee>> GetEmployeesByPositionAsync(Guid positionId);
    Task<List<Employee>> GetManagersAsync(Guid organizationId);
    Task<bool> UpdateEmploymentDetailCurrencyAsync(Guid employeeId, string currency);
    Task<List<EmployeeListItemDto>> GetEmployeesByManagerAsync(Guid managerEmployeeId, Guid organizationId);
}
