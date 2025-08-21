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

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        return Guid.Parse(orgIdClaim!);
    }
}
