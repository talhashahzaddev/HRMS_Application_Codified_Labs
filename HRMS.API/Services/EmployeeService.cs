using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, Guid organizationId)
    {
        // Validate unique employee code and email
        var existingByCode = await _employeeRepository.GetEmployeeByCodeAsync(organizationId, request.EmployeeNumber);
        if (existingByCode != null)
            throw new InvalidOperationException($"Employee with code '{request.EmployeeNumber}' already exists");

        var existingByEmail = await _employeeRepository.GetEmployeeByEmailAsync(organizationId, request.Email);
        if (existingByEmail != null)
            throw new InvalidOperationException($"Employee with email '{request.Email}' already exists");

        var employee = _mapper.Map<Employee>(request);
        employee.OrganizationId = organizationId;

        var createdEmployee = await _employeeRepository.CreateEmployeeAsync(employee);
        var result = await _employeeRepository.GetEmployeeByIdAsync(createdEmployee.EmployeeId);

        return _mapper.Map<EmployeeDto>(result);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(Guid employeeId)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(EmployeeSearchViewModel searchModel, Guid organizationId)
    {
        return await _employeeRepository.GetEmployeesAsync(
            organizationId,
            searchModel.SearchTerm,
            searchModel.DepartmentId,
            searchModel.Status,
            searchModel.Page,
            searchModel.PageSize);
    }

    public async Task<bool> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

        _mapper.Map(request, employee);
        return await _employeeRepository.UpdateEmployeeAsync(employee);
    }

    public async Task<bool> DeleteEmployeeAsync(Guid employeeId)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

        return await _employeeRepository.DeleteEmployeeAsync(employeeId);
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(Guid organizationId)
    {
        var departments = await _employeeRepository.GetDepartmentsAsync(organizationId);
        return _mapper.Map<List<DepartmentDto>>(departments);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, Guid organizationId)
    {
        var department = _mapper.Map<Department>(request);
        department.OrganizationId = organizationId;

        var createdDepartment = await _employeeRepository.CreateDepartmentAsync(department);
        return _mapper.Map<DepartmentDto>(createdDepartment);
    }

    public async Task<List<PositionDto>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null)
    {
        var positions = await _employeeRepository.GetPositionsAsync(organizationId, departmentId);
        return _mapper.Map<List<PositionDto>>(positions);
    }

    public async Task<List<PositionDto>> GetPositionsAsync(Guid organizationId)
    {
        var positions = await _employeeRepository.GetPositionsAsync(organizationId, null);
        return _mapper.Map<List<PositionDto>>(positions);
    }

    public async Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, Guid organizationId)
    {
        var position = _mapper.Map<Position>(request);
        position.OrganizationId = organizationId;

        var createdPosition = await _employeeRepository.CreatePositionAsync(position);
        return _mapper.Map<PositionDto>(createdPosition);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid departmentId, CreateDepartmentRequest request)
    {
        _logger.LogInformation("Updating department: {DepartmentId}", departmentId);
        
        var department = await _employeeRepository.GetDepartmentByIdAsync(departmentId);
        if (department == null)
            throw new KeyNotFoundException($"Department with ID {departmentId} not found");

        _mapper.Map(request, department);
        var updatedDepartment = await _employeeRepository.UpdateDepartmentAsync(department);
        
        _logger.LogInformation("Department {DepartmentId} updated successfully", departmentId);
        return _mapper.Map<DepartmentDto>(updatedDepartment);
    }

    public async Task<bool> DeleteDepartmentAsync(Guid departmentId)
    {
        _logger.LogInformation("Deleting department: {DepartmentId}", departmentId);
        
        var department = await _employeeRepository.GetDepartmentByIdAsync(departmentId);
        if (department == null)
            throw new KeyNotFoundException($"Department with ID {departmentId} not found");

        // Check if department has employees
        var employeeCount = await _employeeRepository.GetEmployeeCountByDepartmentAsync(departmentId);
        if (employeeCount > 0)
            throw new InvalidOperationException($"Cannot delete department with {employeeCount} employees. Please reassign employees first.");

        var result = await _employeeRepository.DeleteDepartmentAsync(departmentId);
        
        _logger.LogInformation("Department {DepartmentId} deleted successfully", departmentId);
        return result;
    }

    public async Task<PositionDto> UpdatePositionAsync(Guid positionId, CreatePositionRequest request)
    {
        _logger.LogInformation("Updating position: {PositionId}", positionId);
        
        var position = await _employeeRepository.GetPositionByIdAsync(positionId);
        if (position == null)
            throw new KeyNotFoundException($"Position with ID {positionId} not found");

        _mapper.Map(request, position);
        var updatedPosition = await _employeeRepository.UpdatePositionAsync(position);
        
        _logger.LogInformation("Position {PositionId} updated successfully", positionId);
        return _mapper.Map<PositionDto>(updatedPosition);
    }

    public async Task<bool> DeletePositionAsync(Guid positionId)
    {
        _logger.LogInformation("Deleting position: {PositionId}", positionId);
        
        var position = await _employeeRepository.GetPositionByIdAsync(positionId);
        if (position == null)
            throw new KeyNotFoundException($"Position with ID {positionId} not found");

        // Check if position has employees
        var employeeCount = await _employeeRepository.GetEmployeeCountByPositionAsync(positionId);
        if (employeeCount > 0)
            throw new InvalidOperationException($"Cannot delete position with {employeeCount} employees. Please reassign employees first.");

        var result = await _employeeRepository.DeletePositionAsync(positionId);
        
        _logger.LogInformation("Position {PositionId} deleted successfully", positionId);
        return result;
    }

    // New missing methods implementation
    public async Task<EmployeeDto> ActivateEmployeeAsync(Guid employeeId)
    {
        _logger.LogInformation("Activating employee: {EmployeeId}", employeeId);
        
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

        employee.Status = "active";
        await _employeeRepository.UpdateEmployeeAsync(employee);
        
        _logger.LogInformation("Employee {EmployeeId} activated successfully", employeeId);
        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto> DeactivateEmployeeAsync(Guid employeeId)
    {
        _logger.LogInformation("Deactivating employee: {EmployeeId}", employeeId);
        
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

        employee.Status = "inactive";
        await _employeeRepository.UpdateEmployeeAsync(employee);
        
        _logger.LogInformation("Employee {EmployeeId} deactivated successfully", employeeId);
        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid departmentId)
    {
        _logger.LogInformation("Retrieving department: {DepartmentId}", departmentId);
        
        var department = await _employeeRepository.GetDepartmentByIdAsync(departmentId);
        return department != null ? _mapper.Map<DepartmentDto>(department) : null;
    }

    public async Task<PositionDto?> GetPositionByIdAsync(Guid positionId)
    {
        _logger.LogInformation("Retrieving position: {PositionId}", positionId);
        
        var position = await _employeeRepository.GetPositionByIdAsync(positionId);
        return position != null ? _mapper.Map<PositionDto>(position) : null;
    }

    public async Task<EmployeeStatisticsDto> GetEmployeeStatisticsAsync(Guid organizationId)
    {
        _logger.LogInformation("Retrieving employee statistics for organization: {OrganizationId}", organizationId);
        
        // Mock implementation - in a real app, this would calculate from database
        var statistics = new EmployeeStatisticsDto
        {
            TotalEmployees = 150,
            ActiveEmployees = 142,
            InactiveEmployees = 8,
            NewEmployeesThisMonth = 5,
            EmployeesTurnedoverThisMonth = 2,
            AverageTenure = 2.5m,
            DepartmentBreakdown = new List<DepartmentStatsDto>
            {
                new() { DepartmentName = "Engineering", EmployeeCount = 60, Percentage = 40.0m },
                new() { DepartmentName = "Sales", EmployeeCount = 30, Percentage = 20.0m },
                new() { DepartmentName = "Marketing", EmployeeCount = 25, Percentage = 16.7m },
                new() { DepartmentName = "HR", EmployeeCount = 15, Percentage = 10.0m },
                new() { DepartmentName = "Finance", EmployeeCount = 20, Percentage = 13.3m }
            },
            EmploymentTypeBreakdown = new List<EmploymentTypeStatsDto>
            {
                new() { EmploymentType = "Full-time", EmployeeCount = 120, Percentage = 80.0m },
                new() { EmploymentType = "Part-time", EmployeeCount = 20, Percentage = 13.3m },
                new() { EmploymentType = "Contract", EmployeeCount = 10, Percentage = 6.7m }
            }
        };
        
        return statistics;
    }

    public async Task<List<EmployeeDto>> GetManagersAsync(Guid organizationId)
    {
        _logger.LogInformation("Retrieving managers for organization: {OrganizationId}", organizationId);
        
        var managers = await _employeeRepository.GetManagersAsync(organizationId);
        return _mapper.Map<List<EmployeeDto>>(managers);
    }

    public async Task<bool> BulkActivateEmployeesAsync(List<Guid> employeeIds)
    {
        _logger.LogInformation("Bulk activating {Count} employees", employeeIds.Count);
        
        var successCount = 0;
        foreach (var employeeId in employeeIds)
        {
            try
            {
                await ActivateEmployeeAsync(employeeId);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to activate employee {EmployeeId}", employeeId);
            }
        }
        
        _logger.LogInformation("Successfully activated {SuccessCount} out of {TotalCount} employees", successCount, employeeIds.Count);
        return successCount == employeeIds.Count;
    }

    public async Task<bool> BulkDeactivateEmployeesAsync(List<Guid> employeeIds)
    {
        _logger.LogInformation("Bulk deactivating {Count} employees", employeeIds.Count);
        
        var successCount = 0;
        foreach (var employeeId in employeeIds)
        {
            try
            {
                await DeactivateEmployeeAsync(employeeId);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deactivate employee {EmployeeId}", employeeId);
            }
        }
        
        _logger.LogInformation("Successfully deactivated {SuccessCount} out of {TotalCount} employees", successCount, employeeIds.Count);
        return successCount == employeeIds.Count;
    }

    public async Task<byte[]> ExportEmployeesAsync(Guid organizationId, string format)
    {
        _logger.LogInformation("Exporting employees for organization: {OrganizationId} in format: {Format}", organizationId, format);
        
        var employees = await _employeeRepository.GetEmployeesAsync(organizationId, null, null, null, 1, 1000);
        
        // Mock implementation - generate simple CSV or Excel data
        var csvData = "EmployeeCode,FirstName,LastName,Email,Department,Position,HireDate,Status\n";
        foreach (var employee in employees.Data)
        {
            csvData += $"{employee.EmployeeCode},{employee.FirstName},{employee.LastName},{employee.Email},{employee.Department},{employee.Position},{employee.HireDate:yyyy-MM-dd},{((bool)employee.IsActive ? "Active" : "Inactive")}\n";
        }
        
        return System.Text.Encoding.UTF8.GetBytes(csvData);
    }
}
