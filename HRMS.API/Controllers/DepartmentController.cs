using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IEmployeeService employeeService, ILogger<DepartmentController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<DepartmentDto>>>> GetDepartments()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var departments = await _employeeService.GetDepartmentsAsync(organizationId);
            
            return Ok(new ServiceResponse<IReadOnlyList<DepartmentDto>>
            {
                Success = true,
                Data = departments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<DepartmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving departments"
            });
        }
    }

    [HttpGet("{id}")]
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {DepartmentId}", id);
            return StatusCode(500, new ServiceResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the department"
            });
        }
    }

    [HttpPost]
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when creating department");
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
