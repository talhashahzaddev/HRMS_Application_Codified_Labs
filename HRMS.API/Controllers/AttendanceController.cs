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
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    [HttpPost("clock-in")]
    public async Task<ActionResult<ApiResponse<bool>>> ClockIn([FromBody] ClockInOutRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId(); // This would need to be implemented
            var result = await _attendanceService.ClockInAsync(employeeId, request);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Clocked in successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock in");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while clocking in"
            });
        }
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<ApiResponse<bool>>> ClockOut([FromBody] ClockInOutRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var result = await _attendanceService.ClockOutAsync(employeeId, request);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Clocked out successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock out");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while clocking out"
            });
        }
    }

    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> GetTodayAttendance([FromQuery] Guid? employeeId = null)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var attendance = await _attendanceService.GetTodayAttendanceAsync(empId);
            
            return Ok(new ApiResponse<AttendanceDto>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's attendance");
            return StatusCode(500, new ApiResponse<AttendanceDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance"
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AttendanceDto>>>> GetAttendance([FromQuery] AttendanceSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var attendance = await _attendanceService.GetAttendanceAsync(searchModel, organizationId);
            
            return Ok(new ApiResponse<PagedResult<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance records");
            return StatusCode(500, new ApiResponse<PagedResult<AttendanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance records"
            });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<AttendanceSummaryDto>>> GetAttendanceSummary([FromQuery] Guid? employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var summary = await _attendanceService.GetAttendanceSummaryAsync(empId, startDate, endDate);
            
            return Ok(new ApiResponse<AttendanceSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance summary");
            return StatusCode(500, new ApiResponse<AttendanceSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance summary"
            });
        }
    }

    [HttpGet("department/{departmentId}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> GetDepartmentAttendance(Guid departmentId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var attendanceDate = date ?? DateTime.Today;
            var attendance = await _attendanceService.GetDepartmentAttendanceAsync(departmentId, attendanceDate);
            
            return Ok(new ApiResponse<List<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department attendance");
            return StatusCode(500, new ApiResponse<List<AttendanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving department attendance"
            });
        }
    }

    private Guid GetCurrentEmployeeId()
    {
        // This would typically query the database to get employee ID from user ID
        // For now, using user ID as placeholder
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        return Guid.Parse(orgIdClaim!);
    }
}
