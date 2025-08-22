using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeListItemDto>>>> GetEmployees([FromQuery] EmployeeSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employees = await _employeeService.GetEmployeesAsync(searchModel, organizationId);
            
            return Ok(new ApiResponse<PagedResult<EmployeeListItemDto>>
            {
                Success = true,
                Data = employees
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees");
            return StatusCode(500, new ApiResponse<PagedResult<EmployeeListItemDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employees"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            return Ok(new ApiResponse<EmployeeDto>
            {
                Success = true,
                Data = employee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employee = await _employeeService.CreateEmployeeAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee created successfully",
                Data = employee
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while creating employee"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.UpdateEmployeeAsync(id, request);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Employee updated successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating employee"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEmployee(Guid id)
    {
        try
        {
            var result = await _employeeService.DeleteEmployeeAsync(id);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Employee deleted successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting employee"
            });
        }
    }

    [HttpGet("departments")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var departments = await _employeeService.GetDepartmentsAsync(organizationId);
            
            return Ok(new ApiResponse<List<DepartmentDto>>
            {
                Success = true,
                Data = departments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, new ApiResponse<List<DepartmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving departments"
            });
        }
    }

    [HttpPost("departments")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var department = await _employeeService.CreateDepartmentAsync(request, organizationId);
            
            return Ok(new ApiResponse<DepartmentDto>
            {
                Success = true,
                Message = "Department created successfully",
                Data = department
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return StatusCode(500, new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while creating department"
            });
        }
    }

    [HttpGet("positions")]
    public async Task<ActionResult<ApiResponse<List<PositionDto>>>> GetPositions([FromQuery] Guid? departmentId = null)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var positions = await _employeeService.GetPositionsAsync(organizationId, departmentId);
            
            return Ok(new ApiResponse<List<PositionDto>>
            {
                Success = true,
                Data = positions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving positions");
            return StatusCode(500, new ApiResponse<List<PositionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving positions"
            });
        }
    }

    [HttpPost("positions")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition([FromBody] CreatePositionRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var position = await _employeeService.CreatePositionAsync(request, organizationId);
            
            return Ok(new ApiResponse<PositionDto>
            {
                Success = true,
                Message = "Position created successfully",
                Data = position
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating position");
            return StatusCode(500, new ApiResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while creating position"
            });
        }
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> ActivateEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.ActivateEmployeeAsync(id);
            return Ok(new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee activated successfully",
                Data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while activating employee"
            });
        }
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> DeactivateEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.DeactivateEmployeeAsync(id);
            return Ok(new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee deactivated successfully",
                Data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while deactivating employee"
            });
        }
    }

    [HttpGet("departments/{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(Guid id)
    {
        try
        {
            var department = await _employeeService.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound(new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            return Ok(new ApiResponse<DepartmentDto>
            {
                Success = true,
                Data = department
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {DepartmentId}", id);
            return StatusCode(500, new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving department"
            });
        }
    }

    [HttpGet("positions/{id}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetPosition(Guid id)
    {
        try
        {
            var position = await _employeeService.GetPositionByIdAsync(id);
            if (position == null)
            {
                return NotFound(new ApiResponse<PositionDto>
                {
                    Success = false,
                    Message = "Position not found"
                });
            }

            return Ok(new ApiResponse<PositionDto>
            {
                Success = true,
                Data = position
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving position {PositionId}", id);
            return StatusCode(500, new ApiResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving position"
            });
        }
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<EmployeeStatisticsDto>>> GetEmployeeStatistics()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var statistics = await _employeeService.GetEmployeeStatisticsAsync(organizationId);
            
            return Ok(new ApiResponse<EmployeeStatisticsDto>
            {
                Success = true,
                Data = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee statistics");
            return StatusCode(500, new ApiResponse<EmployeeStatisticsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee statistics"
            });
        }
    }

    [HttpGet("managers")]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetManagers()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var managers = await _employeeService.GetManagersAsync(organizationId);
            
            return Ok(new ApiResponse<List<EmployeeDto>>
            {
                Success = true,
                Data = managers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving managers");
            return StatusCode(500, new ApiResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving managers"
            });
        }
    }

    [HttpPatch("bulk/activate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkActivateEmployees([FromBody] BulkEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.BulkActivateEmployeesAsync(request.EmployeeIds);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = $"Successfully activated {request.EmployeeIds.Count} employees",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk activating employees");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while activating employees"
            });
        }
    }

    [HttpPatch("bulk/deactivate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkDeactivateEmployees([FromBody] BulkEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.BulkDeactivateEmployeesAsync(request.EmployeeIds);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = $"Successfully deactivated {request.EmployeeIds.Count} employees",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deactivating employees");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deactivating employees"
            });
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<IActionResult> ExportEmployees([FromQuery] string format = "xlsx")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var fileBytes = await _employeeService.ExportEmployeesAsync(organizationId, format);
            
            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = $"employees-{DateTime.Now:yyyy-MM-dd}.{format}";
            
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting employees");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while exporting employees"
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        
        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }
        
        return organizationId;
    }
}
