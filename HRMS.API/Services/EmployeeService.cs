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
        var existingByCode = await _employeeRepository.GetEmployeeByCodeAsync(organizationId, request.EmployeeCode);
        if (existingByCode != null)
            throw new InvalidOperationException($"Employee with code '{request.EmployeeCode}' already exists");

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

    public async Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, Guid organizationId)
    {
        var position = _mapper.Map<Position>(request);
        position.OrganizationId = organizationId;

        var createdPosition = await _employeeRepository.CreatePositionAsync(position);
        return _mapper.Map<PositionDto>(createdPosition);
    }
}
