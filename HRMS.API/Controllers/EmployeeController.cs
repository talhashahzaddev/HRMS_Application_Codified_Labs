using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;

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
    public async Task<ActionResult<ServiceResponse<PagedResult<EmployeeListItemDto>>>> GetEmployees([FromQuery] EmployeeSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employees = await _employeeService.GetEmployeesAsync(searchModel, organizationId);
            
            return Ok(new ServiceResponse<PagedResult<EmployeeListItemDto>>
            {
                Success = true,
                Data = employees
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees");
            return StatusCode(500, new ServiceResponse<PagedResult<EmployeeListItemDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employees"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<EmployeeDto>>> GetEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound(new ServiceResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            return Ok(new ServiceResponse<EmployeeDto>
            {
                Success = true,
                Data = employee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
            return StatusCode(500, new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<EmployeeDto>>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var employee = await _employeeService.CreateEmployeeAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, new ServiceResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee created successfully",
                Data = employee
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating employee");
            return BadRequest(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating employee");
            return BadRequest(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while creating employee"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var result = await _employeeService.UpdateEmployeeAsync(id, request);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Employee updated successfully",
                Data = result
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating employee: {EmployeeId}", id);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating employee: {EmployeeId}", id);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error updating employee: {EmployeeId}", id);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating employee"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteEmployee(Guid id, [FromQuery] string status)
    {
        try
        {
            var result = await _employeeService.DeleteEmployeeAsync(id, status);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Employee deleted successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deleting employee: {EmployeeId}", id);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting employee"
            });
        }
    }

    [HttpGet("departments")]
    public async Task<ActionResult<ServiceResponse<List<DepartmentDto>>>> GetDepartments([FromQuery] string? searchQuery,
    [FromQuery] bool? status)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var departments = await _employeeService.GetDepartmentsAsync(organizationId, searchQuery, status);
            
            return Ok(new ServiceResponse<List<DepartmentDto>>
            {
                Success = true,
                Data = departments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, new ServiceResponse<List<DepartmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving departments"
            });
        }
    }

    [HttpPost("departments")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<DepartmentDto>>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var department = await _employeeService.CreateDepartmentAsync(request, organizationId);
            
            return Ok(new ServiceResponse<DepartmentDto>
            {
                Success = true,
                Message = "Department created successfully",
                Data = department
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate department name error");
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating department");
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating department");
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return StatusCode(500, new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while creating department"
            });
        }
    }

    [HttpPut("departments/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<DepartmentDto>>> UpdateDepartment(Guid id, [FromBody] CreateDepartmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var department = await _employeeService.UpdateDepartmentAsync(id, request);
            
            return Ok(new ServiceResponse<DepartmentDto>
            {
                Success = true,
                Message = "Department updated successfully",
                Data = department
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when updating department: {DepartmentId}", id);
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating department: {DepartmentId}", id);
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating department: {DepartmentId}", id);
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department {DepartmentId}", id);
            return StatusCode(500, new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while updating department"
            });
        }
    }

    [HttpDelete("departments/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteDepartment(Guid id)
    {
        try
        {
            var result = await _employeeService.DeleteDepartmentAsync(id);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Department deleted successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Department not found for deletion: {DepartmentId}", id);
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error deleting department: {DepartmentId}", id);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department {DepartmentId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting department"
            });
        }
    }

    [HttpGet("positions")]
    public async Task<ActionResult<ServiceResponse<List<PositionDto>>>> GetPositions([FromQuery] Guid? departmentId = null, [FromQuery] string? search = null,
    [FromQuery] string? status = null)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var positions = await _employeeService.GetPositionsAsync(organizationId, departmentId, search, status);

            return Ok(new ServiceResponse<List<PositionDto>>
            {
                Success = true,
                Data = positions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving positions");
            return StatusCode(500, new ServiceResponse<List<PositionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving positions"
            });
        }
    }

  
    // GET: api/employees/positions/{positionId}/employees
    [HttpGet("positions/{positionId}/employees")]
    public async Task<ActionResult<ServiceResponse<PositionEmployeesMainDto>>> GetEmployeesByPosition(Guid positionId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId(); // Your method to get current org

            var result = await _employeeService.GetEmployeesByPositionAsync(organizationId, positionId);

            return Ok(new ServiceResponse<PositionEmployeesMainDto>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees for position {PositionId}", positionId);

            return StatusCode(500, new ServiceResponse<PositionEmployeesMainDto>
            {
                Success = false,
                Message = "Failed to load employees for this position"
            });
        }
    }



    [HttpPut("positions/{positionId}/status")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdatePositionStatus(
    Guid positionId,
    [FromQuery] bool isActive)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            var result = await _employeeService.UpdatePositionStatusAsync(
                organizationId,
                positionId,
                isActive
            );

            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Position not found"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = isActive ? "Position activated successfully" : "Position deactivated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position status");

            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating position status"
            });
        }
    }

    [HttpPut("departments/{departmentId}/status")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateDepartmentStatus(
    Guid departmentId,
    [FromQuery] bool isActive)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            var result = await _employeeService.UpdateDepartmentStatusAsync(
                organizationId,
                departmentId,
                isActive
            );

            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = isActive ? "Department activated successfully" : "Department deactivated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department status");

            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating department status"
            });
        }
    }




    [HttpPost("positions")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PositionDto>>> CreatePosition([FromBody] CreatePositionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<PositionDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var position = await _employeeService.CreatePositionAsync(request, organizationId);
            
            return Ok(new ServiceResponse<PositionDto>
            {
                Success = true,
                Message = "Position created successfully",
                Data = position
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating position");
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when creating position");
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Related entity not found when creating position");
            return NotFound(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating position");
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating position");
            return StatusCode(500, new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while creating position"
            });
        }
    }

    [HttpPut("positions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PositionDto>>> UpdatePosition(Guid id, [FromBody] CreatePositionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<PositionDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var position = await _employeeService.UpdatePositionAsync(id, request);

            return Ok(new ServiceResponse<PositionDto>
            {
                Success = true,
                Message = "Position updated successfully",
                Data = position
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when updating position: {PositionId}", id);
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating position: {PositionId}", id);
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating position: {PositionId}", id);
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position {PositionId}", id);
            return StatusCode(500, new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while updating position"
            });
        }
    }




    [HttpDelete("positions/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeletePosition(Guid id)
    {
        try
        {
            var result = await _employeeService.DeletePositionAsync(id);

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Position deleted successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Position not found for deletion: {PositionId}", id);
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error deleting position: {PositionId}", id);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting position {PositionId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting position"
            });
        }
    }


    //Employee Update
    [HttpPut("edit")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateEmployee(
     [FromForm] UpdateEmployeeRequestDTo request,
     IFormFile? profileurl)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            // Manual deserialization of JSON strings into Dictionary
            if (Request.Form.ContainsKey("Address"))
                request.Address = JsonSerializer.Deserialize<Dictionary<string, object>>(Request.Form["Address"]);

            if (Request.Form.ContainsKey("EmergencyContact"))
                request.EmergencyContact = JsonSerializer.Deserialize<Dictionary<string, object>>(Request.Form["EmergencyContact"]);

            // Call service to update employee
            await _employeeService.UpdateEmployeeAsync(request.EmployeeId, request, profileurl);

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Employee updated successfully",
                Data = true
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating employee: {EmployeeId}", request.EmployeeId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating employee: {EmployeeId}", request.EmployeeId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee update failed for EmployeeId: {EmployeeId}", request.EmployeeId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating employee"
            });
        }
    }



    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<EmployeeDto>>> ActivateEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.ActivateEmployeeAsync(id);
            return Ok(new ServiceResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee activated successfully",
                Data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for activating employee: {EmployeeId}", id);
            return BadRequest(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employee {EmployeeId}", id);
            return StatusCode(500, new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while activating employee"
            });
        }
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<EmployeeDto>>> DeactivateEmployee(Guid id)
    {
        try
        {
            var employee = await _employeeService.DeactivateEmployeeAsync(id);
            return Ok(new ServiceResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee deactivated successfully",
                Data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deactivating employee: {EmployeeId}", id);
            return BadRequest(new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee {EmployeeId}", id);
            return StatusCode(500, new ServiceResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while deactivating employee"
            });
        }
    }

    [HttpGet("departments/{id}")]
    public async Task<ActionResult<ServiceResponse<DepartmentDto>>> GetDepartment(Guid id)
    {
        try
        {
            var department = await _employeeService.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound(new ServiceResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            return Ok(new ServiceResponse<DepartmentDto>
            {
                Success = true,
                Data = department
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving department: {DepartmentId}", id);
            return BadRequest(new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {DepartmentId}", id);
            return StatusCode(500, new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving department"
            });
        }
    }

    [HttpGet("positions/{id}")]
    public async Task<ActionResult<ServiceResponse<PositionDto>>> GetPosition(Guid id)
    {
        try
        {
            var position = await _employeeService.GetPositionByIdAsync(id);
            if (position == null)
            {
                return NotFound(new ServiceResponse<PositionDto>
                {
                    Success = false,
                    Message = "Position not found"
                });
            }

            return Ok(new ServiceResponse<PositionDto>
            {
                Success = true,
                Data = position
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving position: {PositionId}", id);
            return BadRequest(new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving position {PositionId}", id);
            return StatusCode(500, new ServiceResponse<PositionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving position"
            });
        }
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<EmployeeStatisticsDto>>> GetEmployeeStatistics()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var statistics = await _employeeService.GetEmployeeStatisticsAsync(organizationId);
            
            return Ok(new ServiceResponse<EmployeeStatisticsDto>
            {
                Success = true,
                Data = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee statistics");
            return StatusCode(500, new ServiceResponse<EmployeeStatisticsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee statistics"
            });
        }
    }

    [HttpGet("managers")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeDto>>>> GetManagers()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var managers = await _employeeService.GetManagersAsync(organizationId);
            
            return Ok(new ServiceResponse<List<EmployeeDto>>
            {
                Success = true,
                Data = managers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving managers");
            return StatusCode(500, new ServiceResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving managers"
            });
        }
    }

    [HttpPatch("bulk/activate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> BulkActivateEmployees([FromBody] BulkEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.BulkActivateEmployeesAsync(request.EmployeeIds);
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully activated {request.EmployeeIds.Count} employees",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk activating employees");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while activating employees"
            });
        }
    }

    [HttpPatch("bulk/deactivate")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> BulkDeactivateEmployees([FromBody] BulkEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.BulkDeactivateEmployeesAsync(request.EmployeeIds);
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully deactivated {request.EmployeeIds.Count} employees",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deactivating employees");
            return StatusCode(500, new ServiceResponse<bool>
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid export format requested: {Format}", format);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting employees");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while exporting employees"
            });
        }
    }

    [HttpGet("managers/{managerEmployeeId}/employees")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeListItemDto>>>> GetEmployeesByManager(Guid managerEmployeeId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employees = await _employeeService.GetEmployeesByManagerAsync(managerEmployeeId, organizationId);
            
            return Ok(new ServiceResponse<List<EmployeeListItemDto>>
            {
                Success = true,
                Data = employees
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for manager {ManagerEmployeeId}", managerEmployeeId);
            return StatusCode(500, new ServiceResponse<List<EmployeeListItemDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving manager's employees"
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
