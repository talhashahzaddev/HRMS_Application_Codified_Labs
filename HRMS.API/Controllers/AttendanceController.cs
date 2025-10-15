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
    public async Task<ActionResult<ServiceResponse<bool>>> ClockIn([FromBody] ClockInOutRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId(); // This would need to be implemented
            var result = await _attendanceService.ClockInAsync(employeeId, request);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Clocked in successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock in");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while clocking in"
            });
        }
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<ServiceResponse<bool>>> ClockOut([FromBody] ClockInOutRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var result = await _attendanceService.ClockOutAsync(employeeId, request);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Clocked out successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock out");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while clocking out"
            });
        }
    }

    [HttpGet("today")]
    public async Task<ActionResult<ServiceResponse<AttendanceDto>>> GetTodayAttendance([FromQuery] Guid? employeeId = null)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var attendance = await _attendanceService.GetTodayAttendanceAsync(empId);
            
            return Ok(new ServiceResponse<AttendanceDto>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's attendance");
            return StatusCode(500, new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance"
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<PagedResult<AttendanceDto>>>> GetAttendance([FromQuery] AttendanceSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var attendance = await _attendanceService.GetAttendanceAsync(searchModel, organizationId);
            
            return Ok(new ServiceResponse<PagedResult<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance records");
            return StatusCode(500, new ServiceResponse<PagedResult<AttendanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance records"
            });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ServiceResponse<AttendanceSummaryDto>>> GetAttendanceSummary([FromQuery] Guid? employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var summary = await _attendanceService.GetAttendanceSummaryAsync(empId, startDate, endDate);
            
            return Ok(new ServiceResponse<AttendanceSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance summary");
            return StatusCode(500, new ServiceResponse<AttendanceSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance summary"
            });
        }
    }

    [HttpGet("department/{departmentId}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceDto>>>> GetDepartmentAttendance(Guid departmentId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var attendanceDate = date ?? DateTime.Today;
            var attendance = await _attendanceService.GetDepartmentAttendanceAsync(departmentId, attendanceDate);
            
            return Ok(new ServiceResponse<List<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department attendance");
            return StatusCode(500, new ServiceResponse<List<AttendanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving department attendance"
            });
        }
    }

    [HttpGet("current-session")]
    public async Task<ActionResult<ServiceResponse<TimeTrackingSessionDto>>> GetCurrentSession()
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var session = await _attendanceService.GetCurrentSessionAsync(employeeId);
            
            return Ok(new ServiceResponse<TimeTrackingSessionDto>
            {
                Success = true,
                Data = session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current session");
            return StatusCode(500, new ServiceResponse<TimeTrackingSessionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving current session"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<AttendanceDto>>> GetAttendance(Guid id)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByIdAsync(id);
            if (attendance == null)
            {
                return NotFound(new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Attendance record not found"
                });
            }

            return Ok(new ServiceResponse<AttendanceDto>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance {AttendanceId}", id);
            return StatusCode(500, new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance record"
            });
        }
    }

    [HttpPost("manual")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<AttendanceDto>>> CreateManualAttendance([FromBody] ManualAttendanceRequest request)
    {
        try
        {
            var attendance = await _attendanceService.CreateManualAttendanceAsync(request);
            
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.AttendanceId }, new ServiceResponse<AttendanceDto>
            {
                Success = true,
                Message = "Manual attendance record created successfully",
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual attendance");
            return StatusCode(500, new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = "An error occurred while creating manual attendance record"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<AttendanceDto>>> UpdateAttendance(Guid id, [FromBody] ManualAttendanceRequest request)
    {
        try
        {
            var attendance = await _attendanceService.UpdateAttendanceAsync(id, request);
            
            return Ok(new ServiceResponse<AttendanceDto>
            {
                Success = true,
                Message = "Attendance record updated successfully",
                Data = attendance
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance {AttendanceId}", id);
            return StatusCode(500, new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = "An error occurred while updating attendance record"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAttendance(Guid id)
    {
        try
        {
            var result = await _attendanceService.DeleteAttendanceAsync(id);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Attendance record deleted successfully",
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance {AttendanceId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting attendance record"
            });
        }
    }

    [HttpGet("my-attendance")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceDto>>>> GetMyAttendance([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var attendance = await _attendanceService.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);
            
            return Ok(new ServiceResponse<List<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my attendance");
            return StatusCode(500, new ServiceResponse<List<AttendanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance records"
            });
        }
    }

    [HttpGet("my-summary")]
    public async Task<ActionResult<ServiceResponse<AttendanceSummaryDto>>> GetMyAttendanceSummary([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var summary = await _attendanceService.GetAttendanceSummaryAsync(employeeId, startDate, endDate);
            
            return Ok(new ServiceResponse<AttendanceSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my attendance summary");
            return StatusCode(500, new ServiceResponse<AttendanceSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance summary"
            });
        }
    }

    [HttpGet("employee/{employeeId}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceDto>>>> GetEmployeeAttendance(Guid employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var attendance = await _attendanceService.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);
            
            return Ok(new ServiceResponse<List<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee attendance");
            return StatusCode(500, new ServiceResponse<List<AttendanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee attendance"
            });
        }
    }

    [HttpGet("calendar/employee/{employeeId}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceCalendarDto>>>> GetEmployeeAttendanceCalendar(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var calendar = await _attendanceService.GetAttendanceCalendarAsync(employeeId, year, month);
            
            return Ok(new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = true,
                Data = calendar
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee attendance calendar");
            return StatusCode(500, new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance calendar"
            });
        }
    }

    [HttpGet("my-calendar")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceCalendarDto>>>> GetMyAttendanceCalendar([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var calendar = await _attendanceService.GetAttendanceCalendarAsync(employeeId, year, month);
            
            return Ok(new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = true,
                Data = calendar
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my attendance calendar");
            return StatusCode(500, new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance calendar"
            });
        }
    }

    [HttpGet("daily-stats")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<DailyAttendanceStatsDto>>> GetDailyAttendanceStats([FromQuery] DateTime? date)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var stats = await _attendanceService.GetDailyAttendanceStatsAsync(organizationId, date ?? DateTime.Today);
            
            return Ok(new ServiceResponse<DailyAttendanceStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily attendance stats");
            return StatusCode(500, new ServiceResponse<DailyAttendanceStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving daily attendance statistics"
            });
        }
    }

    [HttpGet("reports")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<AttendanceReportDto>>> GetAttendanceReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? employeeId, [FromQuery] Guid? departmentId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var report = await _attendanceService.GetAttendanceReportAsync(organizationId, startDate, endDate);
            
            return Ok(new ServiceResponse<AttendanceReportDto>
            {
                Success = true,
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report");
            return StatusCode(500, new ServiceResponse<AttendanceReportDto>
            {
                Success = false,
                Message = "An error occurred while generating attendance report"
            });
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<IActionResult> ExportAttendanceReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? employeeId, [FromQuery] string format = "xlsx")
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var fileBytes = await _attendanceService.ExportAttendanceReportAsync(organizationId, startDate, endDate, format, employeeId);
            
            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = $"attendance-report-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.{format}";
            
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting attendance report");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while exporting attendance report"
            });
        }
    }

    [HttpPatch("bulk/approve")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> BulkApproveAttendance([FromBody] BulkAttendanceRequest request)
    {
        try
        {
            var result = await _attendanceService.BulkApproveAttendanceAsync(request.AttendanceIds);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully approved {request.AttendanceIds.Count} attendance records",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving attendance");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while approving attendance records"
            });
        }
    }

    [HttpPatch("bulk/reject")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> BulkRejectAttendance([FromBody] BulkAttendanceRejectRequest request)
    {
        try
        {
            var result = await _attendanceService.BulkRejectAttendanceAsync(request.AttendanceIds, request.Reason);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully rejected {request.AttendanceIds.Count} attendance records",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk rejecting attendance");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while rejecting attendance records"
            });
        }
    }

    private Guid GetCurrentEmployeeId()
    {
        // This would typically query the database to get employee ID from user ID
        // For now, using user ID as placeholder
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        
        return userId;
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
