using AutoMapper;
using ClosedXML.Excel;
using CloudinaryDotNet.Core;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using OfficeOpenXml;
using System.Linq;

namespace HRMS.API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuthService _authService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;
    private readonly ICloudinaryService _cloudinaryService;
    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IAuthService authService,
        ISettingsRepository settingsRepository,
        IMapper mapper,
         ICloudinaryService cloudinaryService,
        ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _authService = authService;
        _settingsRepository = settingsRepository;
        _cloudinaryService = cloudinaryService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, Guid organizationId)
    {
        _logger.LogInformation("Creating employee for organization: {OrganizationId}", organizationId);
        
        // Validate required fields
        if (string.IsNullOrEmpty(request.Email))
            throw new InvalidOperationException("Employee email is required");
        
        if (string.IsNullOrEmpty(request.FirstName))
            throw new InvalidOperationException("Employee first name is required");
            
        if (string.IsNullOrEmpty(request.LastName))
            throw new InvalidOperationException("Employee last name is required");

        // Validate unique employee code and email
        var existingByCode = await _employeeRepository.GetEmployeeByCodeAsync(organizationId, request.EmployeeNumber);
        if (existingByCode != null)
            throw new InvalidOperationException($"Employee with code '{request.EmployeeNumber}' already exists");

        var existingByEmail = await _employeeRepository.GetEmployeeByEmailAsync(organizationId, request.Email);
        if (existingByEmail != null)
            throw new InvalidOperationException($"Employee with email '{request.Email}' already exists");

        // Step 1: Create user account first
        Guid? userId = null;
        string? roleName = "Employee"; // Default role
        
        // If position is assigned, get the role from that position
        if (request.PositionId.HasValue)
        {
            var position = await _employeeRepository.GetPositionByIdAsync(request.PositionId.Value);
            if (position?.RoleId.HasValue == true)
            {
                // Get role name by role ID
                var roles = await _authService.GetOrganizationRolesAsync(organizationId);
                var assignedRole = roles.FirstOrDefault(r => r.RoleId == position.RoleId.Value);
                if (assignedRole != null)
                {
                    roleName = assignedRole.RoleName;
                }
            }
        }

        // Create user account with default password "Password123"
        var createUserRequest = new CreateUserRequest
        {
            Email = request.Email,
            Password = "Password123", // Default password
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleName = roleName
        };

        try
        {
            var userDto = await _authService.CreateUserAsync(createUserRequest, organizationId);
            userId = userDto.UserId;
            _logger.LogInformation("Created user account {UserId} for employee {Email} with role {RoleName}", 
                userId, request.Email, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user account for employee {Email}", request.Email);
            throw new InvalidOperationException($"Failed to create user account for employee: {ex.Message}");
        }

        // Step 2: Get organization currency
        var organizationCurrency = await _settingsRepository.GetCurrencyAsync(organizationId);
        if (string.IsNullOrEmpty(organizationCurrency))
        {
            organizationCurrency = "USD"; // Default to USD if no currency is set
            _logger.LogWarning("No currency set for organization {OrganizationId}, defaulting to USD", organizationId);
        }

        // Step 3: Create employee record with the created user ID
        var employee = _mapper.Map<Employee>(request);
        employee.OrganizationId = organizationId;
        employee.UserId = userId;

        var createdEmployee = await _employeeRepository.CreateEmployeeAsync(employee);

        _logger.LogInformation("Created employee {EmployeeId} with user account {UserId}",
            createdEmployee.EmployeeId, userId);

        // Step 4: Update EmploymentDetail currency to match organization currency
        await _employeeRepository.UpdateEmploymentDetailCurrencyAsync(createdEmployee.EmployeeId, organizationCurrency);
        _logger.LogInformation("Updated EmploymentDetail currency to {Currency} for employee {EmployeeId}",
            organizationCurrency, createdEmployee.EmployeeId);

        var result = await _employeeRepository.GetEmployeedetailsAsync(createdEmployee.EmployeeId);

       
        return _mapper.Map<EmployeeDto>(result);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(Guid userId)
    {
        var empid = await _employeeRepository.getemployeeid(userId);

        var employee = await _employeeRepository.GetEmployeeByIdAsync(empid);

        return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(EmployeeSearchViewModel searchModel, Guid organizationId)
    {
        return await _employeeRepository.GetEmployeesAsync(
            organizationId,
            searchModel.SearchTerm,
            searchModel.DepartmentId,
            searchModel.PositionId,       // pass it
            searchModel.EmploymentType,
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


    public async Task<bool> DeleteEmployeeAsync(Guid employeeId, string? status)
    {
        if (status.ToLower() == "delete")
        {
            status = "delete";
        }
        else
        {
            status = "active";
        }

        return await _employeeRepository.DeleteEmployeeAsync(employeeId, status);
    }


    public async Task<List<DepartmentDto>> GetDepartmentsAsync(Guid organizationId, string? searchQuery = null, bool? status = null)
    {
        var departments = await _employeeRepository.GetDepartmentsAsync(organizationId, searchQuery, status);

        return departments.Select(d => new DepartmentDto
        {
            DepartmentId = d.DepartmentId,
            OrganizationId = d.OrganizationId,
            DepartmentName = d.DepartmentName,
            Description = d.Description,
            ManagerId = d.ManagerId,
            ManagerName = d.Manager?.FullName,
            IsActive = d.IsActive,
            EmployeeCount = d.EmployeeCount,
            CreatedAt = d.CreatedAt
        }).ToList();
    }

    public async Task<bool> UpdatePositionStatusAsync(
    Guid organizationId,
    Guid positionId,
    bool isActive)
    {
        // business decision directly
        bool status = isActive;

        var result = await _employeeRepository.UpdatePositionStatusAsync(
            organizationId,
            positionId,
            status
        );

        return result;
    }


    public async Task<bool> UpdateDepartmentStatusAsync(
    Guid organizationId,
    Guid departmentId,
    bool isActive)
    {
        // business decision directly
        bool status = isActive;

        var result = await _employeeRepository.UpdateDepartmentStatusAsync(
            organizationId,
            departmentId,
            status
        );

        return result;
    }

    //
    public async Task<bool> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequestDTo request, IFormFile? profileFile)
    {
        //Update user table first
        // get employee userId to update employe in user 
        var userId = await _employeeRepository.GetEmpUserIdAsync(employeeId);

        var UpdateUserTable = await _employeeRepository.editEmployeeUserAsync(userId, request);

        // 1️⃣ Get employee from database
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

        // 2️⃣ Upload profile picture if provided
        string? profileUrl = employee.ProfilePictureUrl;
        if (profileFile != null && profileFile.Length > 0)
        {
            var uploadedUrl = await _cloudinaryService.UploadProfilePictureAsync(profileFile);
            if (!string.IsNullOrEmpty(uploadedUrl))
                profileUrl = uploadedUrl;
        }

        // 3️⃣ Update Employee table
        var updateEmployeeTable = await _employeeRepository.EditEmployeeTableAsync(employee.EmployeeId, request, profileUrl);

        // 4️⃣ Update Employee Details table
        var updateEmployeeDetails = await _employeeRepository.EditEmployeeDetailsAsync(employee.EmployeeId, request);

        //return updateEmployeeTable && updateEmployeeDetails;

        return true;
    }



    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, Guid organizationId)
    {
        var department = _mapper.Map<Models.Entities.Department>(request);
        department.OrganizationId = organizationId;

        var createdDepartment = await _employeeRepository.CreateDepartmentAsync(department);
        return _mapper.Map<DepartmentDto>(createdDepartment);
    }

    public async Task<List<PositionDto>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null, string? search = null,
    string? status = null)
    {
        var positions = await _employeeRepository.GetPositionsAsync(organizationId, departmentId,search,status);
        var positionDtos = _mapper.Map<List<PositionDto>>(positions);

      
        foreach (var positionDto in positionDtos)
        {
            var position = positions.FirstOrDefault(p => p.PositionId == positionDto.PositionId);
            positionDto.EmployeeCount = position?.EmploymentDetails?.Count ?? 0;
        }

        return positionDtos;
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

        var oldRoleId = position.RoleId;
        _mapper.Map(request, position);
        var updatedPosition = await _employeeRepository.UpdatePositionAsync(position);
        
        // If the role of the position changed, update all employees with this position
        if (oldRoleId != updatedPosition.RoleId && updatedPosition.RoleId.HasValue)
        {
            var employeesWithPosition = await _employeeRepository.GetEmployeesByPositionAsync(positionId);
            foreach (var employee in employeesWithPosition)
            {
                if (employee.UserId.HasValue)
                {
                    await _authService.UpdateUserRoleAsync(employee.UserId.Value, updatedPosition.RoleId.Value);
                    _logger.LogInformation("Updated user {UserId} role to {RoleId} due to position {PositionId} role change", 
                        employee.UserId, updatedPosition.RoleId, positionId);
                }
            }
        }
        
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
        _logger.LogInformation(
            "Exporting employees for organization: {OrganizationId}", organizationId);

        var employees = await _employeeRepository.GetEmployeesAsync(
            organizationId, null, null, null, null, null, 1, 1000);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");

        // ===== HEADERS =====
        sheet.Cell(1, 1).Value = "Employee Code";
        sheet.Cell(1, 2).Value = "First Name";
        sheet.Cell(1, 3).Value = "Last Name";
        sheet.Cell(1, 4).Value = "Full Name";
        sheet.Cell(1, 5).Value = "Email";
        sheet.Cell(1, 6).Value = "Phone";
        sheet.Cell(1, 7).Value = "Department";
        sheet.Cell(1, 8).Value = "Position";
        sheet.Cell(1, 9).Value = "Employment Type";
        sheet.Cell(1, 10).Value = "Pay Type";
        sheet.Cell(1, 11).Value = "Basic Salary";
        sheet.Cell(1, 12).Value = "Hire Date";
        sheet.Cell(1, 13).Value = "Status";
        sheet.Cell(1, 14).Value = "Reporting Manager";
        sheet.Cell(1, 15).Value = "Gender";
        sheet.Cell(1, 16).Value = "Nationality";
        sheet.Cell(1, 17).Value = "Marital Status";
        sheet.Cell(1, 18).Value = "Date of Birth";
        sheet.Cell(1, 19).Value = "Street";
        sheet.Cell(1, 20).Value = "City";
        sheet.Cell(1, 21).Value = "State";
        sheet.Cell(1, 22).Value = "Zip";
        sheet.Cell(1, 23).Value = "Emergency Name";
        sheet.Cell(1, 24).Value = "Emergency Email";
        sheet.Cell(1, 25).Value = "Emergency Phone";
        sheet.Cell(1, 26).Value = "Emergency Relation";

        sheet.Row(1).Style.Font.Bold = true;


        // ===== DATA =====

        int row = 2;
        foreach (var emp in employees.Data)
        {
            sheet.Cell(row, 1).Value = emp.EmployeeCode;
            sheet.Cell(row, 2).Value = emp.FirstName;
            sheet.Cell(row, 3).Value = emp.LastName;
            sheet.Cell(row, 4).Value = emp.FullName;
            sheet.Cell(row, 5).Value = emp.Email;
            sheet.Cell(row, 6).Value = emp.Phone;

            sheet.Cell(row, 7).Value = emp.DepartmentName;
            sheet.Cell(row, 8).Value = emp.PositionTitle;

            sheet.Cell(row, 9).Value = emp.employmentType;   // (column skip fix)
            sheet.Cell(row, 10).Value = emp.PayType;
            sheet.Cell(row, 11).Value = emp.BasicSalary;

            sheet.Cell(row, 12).Value = emp.HireDate?.ToString("yyyy-MM-dd");
            sheet.Cell(row, 13).Value = emp.Status;

            sheet.Cell(row, 14).Value = emp.ReportingManagerName;
            sheet.Cell(row, 15).Value = emp.Gender;
            sheet.Cell(row, 16).Value = emp.Nationality;
            sheet.Cell(row, 17).Value = emp.MaritalStatus;
            sheet.Cell(row, 18).Value = emp.DateOfBirth?.ToString("yyyy-MM-dd");

            // ===== Address (Dictionary) =====
            sheet.Cell(row, 19).Value = GetDictValue(emp.Address, "street");
            sheet.Cell(row, 20).Value = GetDictValue(emp.Address, "city");
            sheet.Cell(row, 21).Value = GetDictValue(emp.Address, "state");
            sheet.Cell(row, 22).Value = GetDictValue(emp.Address, "zip");

            // ===== Emergency Contact (Dictionary) =====
            sheet.Cell(row, 23).Value = GetDictValue(emp.EmergencyContact, "name");
            sheet.Cell(row, 24).Value = GetDictValue(emp.EmergencyContact, "email");
            sheet.Cell(row, 25).Value = GetDictValue(emp.EmergencyContact, "phone");
            sheet.Cell(row, 26).Value = GetDictValue(emp.EmergencyContact, "relationship");

            row++;
        }



        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    //Helper function for converting the Dictionary elemtens
    string GetDictValue(Dictionary<string, string>? dict, string key)
    {
        return dict != null && dict.TryGetValue(key, out var value)
            ? value
            : string.Empty;
    }


    public async Task<List<EmployeeListItemDto>> GetEmployeesByManagerAsync(Guid managerEmployeeId, Guid organizationId)
    {
        return await _employeeRepository.GetEmployeesByManagerAsync(managerEmployeeId, organizationId);
    }
}
