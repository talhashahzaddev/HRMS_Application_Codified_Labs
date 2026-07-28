using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using System.Threading.Tasks;

namespace HRMS.API.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<IEnumerable<EmployeeDto>> GetEmployeesReportingToManagerAsync(Guid managerId);
    Task<List<PositionEmployeeDto>> GetEmployeesForPositionAsync(Guid organizationId, Guid positionId);
    Task<Guid?> GetEmployeeIdAsync(Guid userId);
    Task<bool> EditEmployeeDetailsAsync(Guid employeeId, UpdateEmployeeRequestDTo request);
    Task<bool> EditEmployeeTableAsync(Guid employeeId, UpdateEmployeeRequestDTo request, string? profileUrl);
    Task<bool> editEmployeeUserAsync(Guid? userId, UpdateEmployeeRequestDTo request);
    Task<Guid?> GetEmpUserIdAsync(Guid employeeId);
    Task<bool> UpdateDepartmentStatusAsync(Guid organizationId,Guid departmentId,bool isActive);
    Task<bool> UpdatePositionStatusAsync(Guid organizationId,Guid positionId,bool isActive);
    Task<EmployeeDto> GetEmployeedetailsAsync(Guid employeeId);

    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task<Employee?> GetEmployeeByIdAsync(Guid? employeeId);
    Task<Employee?> GetEmployeeByEmployeeIdAsync(Guid? employeeId);
    Task<string> GetUserRoleAsync(Guid userid);
    Task<List<EmployeeNameList>> GetEmployeeByOrganizationAsync(Guid? organizationid, Guid? reportingmanagerid);
    Task<Employee?> GetEmployeeByCodeAsync(Guid organizationId, string employeeCode);
    Task<List<Employee>> GeSelectedtEmployeesByIdsAsync(List<Guid> employeeIds);
    Task<Employee?> GetEmployeeByEmailAsync(Guid organizationId, string email);
    Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(
    Guid? organizationId = null, string? searchTerm = null, Guid? departmentId = null,Guid? positionId=null,string? employmentType=null,
    string? status = null, int page = 1, int pageSize = 50);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(Guid employeeId,string status);
    Task<List<Department>> GetDepartmentsAsync(Guid organizationId, string? searchQuery = null,
    bool? status = null);
    Task<Department> CreateDepartmentAsync(Department department);
    Task<Department?> GetDepartmentByIdAsync(Guid departmentId);
    Task<Department> UpdateDepartmentAsync(Department department);
    Task<bool> DeleteDepartmentAsync(Guid departmentId);
    Task<int> GetEmployeeCountByDepartmentAsync(Guid departmentId);
    Task<List<Position>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null, string? search = null,string? status = null);
    Task<Position> CreatePositionAsync(Position position);
    Task<Position?> GetPositionByIdAsync(Guid positionId);
    Task<Position> UpdatePositionAsync(Position position);
    Task<bool> DeletePositionAsync(Guid positionId);
    Task<int> GetEmployeeCountByPositionAsync(Guid positionId);
    Task<List<Employee>> GetEmployeesByPositionAsync(Guid positionId);
    Task<List<Employee>> GetManagersAsync(Guid organizationId, Guid? departmentId = null);
    Task<bool> UpdateEmploymentDetailCurrencyAsync(Guid employeeId, string currency);
    Task<List<EmployeeListItemDto>> GetEmployeesByManagerAsync(Guid managerEmployeeId, Guid organizationId);
    Task<bool> CheckEmployeeLimitAsync(Guid organizationId);
    Task<Guid?> GetRoleIdByPositionIdAsync(Guid positionId);
    Task<List<Employee>> GetDepartmentEmployeesAndAdminsAsync(Guid organizationId, Guid departmentId);
}
