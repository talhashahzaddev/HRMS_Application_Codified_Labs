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

    [HttpGet("{id}")]
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
                Message = "An error occurred while retrieving the department"
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
