using DocumentFormat.OpenXml.Bibliography;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    public async Task<ActionResult<ServiceResponse<ClockInOutResponse>>> ClockIn([FromBody] ClockInOutRequest request)
    {
        try
        {
            _logger.LogInformation("ClockIn request received: ShiftId={ShiftId}, Action={Action}", request.ShiftId, request.Action);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var employeeId = await GetCurrentEmployeeId(); // This would need to be implemented
            var ipAddress = GetClientIp();

            var result = await _attendanceService.ClockInAsync(employeeId, request,ipAddress);

            return Ok(new ServiceResponse<ClockInOutResponse>
            {
                Success = result.Success,
                Message = result.Message,
                Data = result
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for clock in");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for clock in");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error during clock in");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock in");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<ServiceResponse<ClockInOutResponse>>> ClockOut([FromBody] ClockInOutRequest request)
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
            var employeeId = await GetCurrentEmployeeId();
            var ipAddress = GetClientIp();

            var result = await _attendanceService.ClockOutAsync(employeeId, request, ipAddress);

            return Ok(new ServiceResponse<ClockInOutResponse>
            {
                Success = result.Success,
                Message = result.Message,
                Data = result
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for clock out");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for clock out");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error during clock out");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock out");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("today")]
    public async Task<ActionResult<ServiceResponse<AttendanceDto>>> GetTodayAttendance([FromQuery] Guid? employeeId = null)
    {
        try
        {
            var empId = employeeId ?? await GetCurrentEmployeeId();
            var attendance = await _attendanceService.GetTodayAttendanceAsync(empId);
            
            return Ok(new ServiceResponse<AttendanceDto>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving today's attendance");
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
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
            var currentEmployeeId = await GetCurrentEmployeeId();
            var attendance = await _attendanceService.GetAttendanceAsync(searchModel, organizationId, currentEmployeeId);
            
            return Ok(new ServiceResponse<PagedResult<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving attendance records");
            return BadRequest(new ServiceResponse<PagedResult<AttendanceDto>>
            {
                Success = false,
                Message = ex.Message
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
            var empId = employeeId ?? await GetCurrentEmployeeId();
            var summary = await _attendanceService.GetAttendanceSummaryAsync(empId, startDate, endDate);
            
            return Ok(new ServiceResponse<AttendanceSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving attendance summary");
            return BadRequest(new ServiceResponse<AttendanceSummaryDto>
            {
                Success = false,
                Message = ex.Message
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving department attendance");
            return BadRequest(new ServiceResponse<List<AttendanceDto>>
            {
                Success = false,
                Message = ex.Message
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
            var employeeId = await GetCurrentEmployeeId();
            var session = await _attendanceService.GetCurrentSessionAsync(employeeId);

            return Ok(new ServiceResponse<TimeTrackingSessionDto>
            {
                Success = true,
                Data = session
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving current session");
            return BadRequest(new ServiceResponse<TimeTrackingSessionDto>
            {
                Success = false,
                Message = ex.Message
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


    [HttpGet("employeeSession")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceSessionDto>>>> EmployeeTodaySessions([FromQuery] DateTime? date = null)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeId();
            var sessions = await _attendanceService.EmployeeTodaySessions(employeeId, date);

            return Ok(new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = true,
                Data = sessions
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving current sessions");
            return BadRequest(new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current sessions");
            return StatusCode(500, new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving current sessions"
            });
        }
    }



    [HttpGet("employeeSession/{id}")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceSessionDto>>>> EmployeeSessionsByid(Guid id, [FromQuery] DateTime? date = null)
     {
        try
        {
            //var employeeId = await GetCurrentEmployeeId();
            var sessions = await _attendanceService.EmployeeTodaySessions(id,date);

            return Ok(new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = true,
                Data = sessions
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving sessions by id");
            return BadRequest(new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current sessions");
            return StatusCode(500, new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving current sessions"
            });
        }
    }


    [HttpGet("employeeSessionforcalendar/{id}")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceSessionDto>>>> EmployeeSessionsByidforcalendar(Guid id, [FromQuery] DateTime? date = null)
    {
        try
        {

            var employeeId = await GetCurrentEmployeeId();
            var sessions = await _attendanceService.EmployeeTodaySessions(employeeId, date);

            return Ok(new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = true,
                Data = sessions
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving sessions by id");
            return BadRequest(new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current sessions");
            return StatusCode(500, new ServiceResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving current sessions"
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving attendance by id");
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var attendance = await _attendanceService.CreateManualAttendanceAsync(request);
            
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.AttendanceId }, new ServiceResponse<AttendanceDto>
            {
                Success = true,
                Message = "Manual attendance record created successfully",
                Data = attendance
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating manual attendance");
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating manual attendance");
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating manual attendance");
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
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
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating attendance {AttendanceId}", id);
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating attendance {AttendanceId}", id);
            return BadRequest(new ServiceResponse<AttendanceDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error updating attendance {AttendanceId}", id);
            return BadRequest(new ServiceResponse<AttendanceDto>
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deleting attendance {AttendanceId}", id);
            return BadRequest(new ServiceResponse<bool>
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
            var employeeId = await GetCurrentEmployeeId();
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
            var employeeId = await GetCurrentEmployeeId();
            var summary = await _attendanceService.GetAttendanceSummaryAsync(employeeId, startDate, endDate);

            return Ok(new ServiceResponse<AttendanceSummaryDto>
            {
                Success = true,
                Data = summary
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving my attendance summary");
            return BadRequest(new ServiceResponse<AttendanceSummaryDto>
            {
                Success = false,
                Message = ex.Message
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





    [HttpGet("EmployeeAllAttendance")]
    public async Task<ActionResult<ServiceResponse<PagedResult<AttendanceSessionWithInfoDto>>>> EmployeeAllAttendance(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeId();

            var result = await _attendanceService.EmployeeAllAttendanceAsync(
                employeeId,
                startDate,
                endDate,
                pageNumber,
                pageSize
            );

            return Ok(new ServiceResponse<PagedResult<AttendanceSessionWithInfoDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving employee attendance");
            return BadRequest(new ServiceResponse<PagedResult<AttendanceSessionWithInfoDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee attendance");
            return StatusCode(500, new ServiceResponse<PagedResult<AttendanceSessionWithInfoDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee attendance"
            });
        }
    }



    [HttpGet("employee/{employeeId}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceDto>>>> GetEmployeeAttendance(Guid employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var empid = await GetCurrentEmployeeId();
            var attendance = await _attendanceService.GetEmployeeAttendanceAsync(empid, startDate, endDate);
            
            return Ok(new ServiceResponse<List<AttendanceDto>>
            {
                Success = true,
                Data = attendance
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving employee attendance");
            return BadRequest(new ServiceResponse<List<AttendanceDto>>
            {
                Success = false,
                Message = ex.Message
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving employee attendance calendar");
            return BadRequest(new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = false,
                Message = ex.Message
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



    [HttpPost("shift")]
    public async Task<ActionResult<ServiceResponse<bool>>> CreateShift([FromBody] CreateShiftDto request)
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
            var organizationId = GetCurrentOrganizationId();

            var createdShift = await _attendanceService.CreateShiftAsync(organizationId, request);

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Shift Created Sucessfully"
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating shift");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating shift");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating shift");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while creating the shift"
            });
        }
    }


    [HttpPost("assign-shift")]
    public async Task<ActionResult<ServiceResponse<bool>>> AssignShift([FromBody] AssignShiftDto request)
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
            var organizationId = GetCurrentOrganizationId();
            var currentEmp = await GetCurrentEmployeeId();

            var result = await _attendanceService.AssignShiftToEmployeeAsync(organizationId, request,currentEmp);

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Shift assigned successfully.",
                Data = true
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for assigning shift");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for assigning shift");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error assigning shift");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning shift to employee");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while assigning the shift."
            });
        }
    }


    [HttpGet("shift/{shiftId}")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeShiftDto>>>> GetEmployeesByShift(Guid shiftId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var CurrentEmployee = await GetCurrentEmployeeId();
           
            var employees = await _attendanceService.GetEmployeesByShiftAsync(organizationId, shiftId,CurrentEmployee);

            return Ok(new ServiceResponse<List<EmployeeShiftDto>>
            {
                Success = true,
                Message = "Employees retrieved successfully.",
                Data = employees
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving employees by shift");
            return BadRequest(new ServiceResponse<List<EmployeeShiftDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees by shift");
            return StatusCode(500, new ServiceResponse<List<EmployeeShiftDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employees by shift"
            });
        }
    }

    //Shift swap Create

    [HttpPost("shiftswap")]
    public async Task<ActionResult<ServiceResponse<bool>>> CreateShiftSwapRequest([FromBody] CreateShiftSwapRequestDto requestDto)
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
            var organizationId = GetCurrentOrganizationId();

            // Call service to create the shift swap request
            var success = await _attendanceService.CreateShiftSwapRequestAsync(organizationId, requestDto);

            return Ok(new ServiceResponse<bool>
            {
                Success = success,
                Message = success ? "Shift swap request created successfully." : "Failed to create shift swap request.",
                Data = success
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating shift swap");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating shift swap");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating shift swap request");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift swap request");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while creating the shift swap request",
                Data = false
            });
        }
    }

    [HttpPost("shiftswap/approve")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveShiftSwapRequest([FromBody] ApproveShiftSwapRequestDto dto)
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
            var status = dto.IsApproved ? "approved" : " rejected";
            var organizationId= GetCurrentOrganizationId();
            var success = await _attendanceService.ApproveShiftSwapRequestAsync(dto,organizationId);

            return Ok(new ServiceResponse<bool>
            {
                Success = success,
                Message = success ? $"Shift swap request {status} successfully." : "Failed to process request.",
                Data = success
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for approving shift swap");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for approving shift swap");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error processing shift swap request {RequestId}", dto.RequestId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing shift swap request {RequestId}", dto.RequestId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while processing the shift swap request.",
                Data = false
            });
        }
    }

    [HttpGet("shiftswap/pending")]
    public async Task<ActionResult<ServiceResponse<List<PendingShiftSwapDto>>>> GetPendingShiftSwapRequests()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId(); // method to get org id from context
            var currentEmployee = await GetCurrentEmployeeId();
            var requests = await _attendanceService.GetPendingShiftSwapRequestsAsync(organizationId,currentEmployee);

            return Ok(new ServiceResponse<List<PendingShiftSwapDto>>
            {
                Success = true,
                Message = "Pending shift swap requests retrieved successfully.",
                Data = requests
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for fetching pending shift swap requests");
            return BadRequest(new ServiceResponse<List<PendingShiftSwapDto>>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending shift swap requests");
            return StatusCode(500, new ServiceResponse<List<PendingShiftSwapDto>>
            {
                Success = false,
                Message = "An error occurred while fetching pending shift swap requests.",
                Data = null
            });
        }
    }

    //Getting employee All shifts

    [HttpGet("shiftswap/eemployeeShifts/{employeeId}")]
    public async Task<ActionResult<ServiceResponse<List<PendingShiftSwapDto>>>> GetShiftSwapRequestsByEmployee(Guid employeeId)
    {
        try
        {
            var requests = await _attendanceService.GetShiftSwapRequestsByEmployeeAsync(employeeId);

            return Ok(new ServiceResponse<List<PendingShiftSwapDto>>
            {
                Success = true,
                Message = requests.Count > 0 ? "Shift swap requests retrieved successfully." : "No shift swap requests found.",
                Data = requests
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for fetching shift swap requests for employee {EmployeeId}", employeeId);
            return BadRequest(new ServiceResponse<List<PendingShiftSwapDto>>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift swap requests for employee {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<List<PendingShiftSwapDto>>
            {
                Success = false,
                Message = "An error occurred while fetching the employee's shift swap requests.",
                Data = null
            });
        }
    }





    //Getting Employee Current Shift of Employee


    [HttpGet("CurrentShift/{employeeId}")]
    public async Task<ActionResult<ServiceResponse<ShiftDto>>> GetShiftByEmployeeId(Guid employeeId)
    {
        try
        {
            var shift = await _attendanceService.GetShiftByEmployeeIdAsync(employeeId);

            //if (shift == null)
            //{
            //    return NotFound(new ServiceResponse<ShiftDto>
            //    {
            //        Success = true,
            //        Message = "Shift not found for the given employee.",
            //        Data = null
            //    });
            //}

            return Ok(new ServiceResponse<ShiftDto>
            {
                Success = true,
                Message = "Employee shift retrieved successfully.",
                Data = shift
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for fetching shift for employee {EmployeeId}", employeeId);
            return BadRequest(new ServiceResponse<ShiftDto>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift for employee {EmployeeId}", employeeId);
            return StatusCode(500, new ServiceResponse<ShiftDto>
            {
                Success = false,
                Message = "An error occurred while fetching the employee shift.",
                Data = null
            });
        }
    }


    [HttpGet("shifts")]
    public async Task<ActionResult<ServiceResponse<List<ShiftDto>>>> GetAllShifts()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            var shifts = await _attendanceService.GetAllShiftsAsync(organizationId);

            return Ok(new ServiceResponse<List<ShiftDto>>
            {
                Success = true,
                Message = "Shifts retrieved successfully.",
                Data = shifts
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving all shifts");
            return BadRequest(new ServiceResponse<List<ShiftDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all shifts");
            return StatusCode(500, new ServiceResponse<List<ShiftDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving shifts."
            });
        }
    }

    [HttpPut("shift/{shiftId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateShift(Guid shiftId, [FromBody] UpdateShiftDto request)
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
            var organizationId = GetCurrentOrganizationId();

            var success = await _attendanceService.UpdateShiftAsync(organizationId, shiftId, request);

            if (!success)
                return NotFound(new ServiceResponse<bool> { Success = false, Message = "Shift not found" });

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Shift updated successfully"
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating shift {ShiftId}", shiftId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating shift {ShiftId}", shiftId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error updating shift {ShiftId}", shiftId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift {ShiftId}", shiftId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating the shift"
            });
        }
    }

    [HttpDelete("shift/{shiftId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteShift(Guid shiftId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            var success = await _attendanceService.DeleteShiftAsync(organizationId, shiftId);

            if (!success)
                return NotFound(new ServiceResponse<bool> { Success = false, Message = "Shift not found" });

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Shift deleted successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deleting shift {ShiftId}", shiftId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift {ShiftId}", shiftId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the shift"
            });
        }
    }



    [HttpGet("my-calendar")]
    public async Task<ActionResult<ServiceResponse<List<AttendanceCalendarDto>>>> GetMyAttendanceCalendar([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeId();
            var calendar = await _attendanceService.GetAttendanceCalendarAsync(employeeId, year, month);
            
            return Ok(new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = true,
                Data = calendar
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving my attendance calendar");
            return BadRequest(new ServiceResponse<List<AttendanceCalendarDto>>
            {
                Success = false,
                Message = ex.Message
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
            var employeeId = await GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();
            var stats = await _attendanceService.GetDailyAttendanceStatsAsync(employeeId,organizationId, date ?? DateTime.Today);
            
            return Ok(new ServiceResponse<DailyAttendanceStatsDto>
            {
                Success = true,
                Data = stats
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving daily attendance stats");
            return BadRequest(new ServiceResponse<DailyAttendanceStatsDto>
            {
                Success = false,
                Message = ex.Message
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
    public async Task<ActionResult<ServiceResponse<AttendanceReportDto>>> GetAttendanceReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? employeeId, [FromQuery] Guid? departmentId, [FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();
            var report = await _attendanceService.GetAttendanceReportAsync(organizationId, startDate, endDate, currentEmployeeId, departmentId, status, employeeId, pageNumber, pageSize);

            return Ok(new ServiceResponse<AttendanceReportDto>
            {
                Success = true,
                Data = report
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for generating attendance report");
            return BadRequest(new ServiceResponse<AttendanceReportDto>
            {
                Success = false,
                Message = ex.Message
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



    [HttpGet("departmentEmployees")]
    public async Task<ActionResult<ServiceResponse<List<EmployeeSimpleDto>>>> GetEmployeesByDepartment([FromQuery] Guid? departmentId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();

            var employees = await _attendanceService.GetdepartmentEmployeesAsync(currentEmployeeId, departmentId, organizationId);

            return Ok(new ServiceResponse<List<EmployeeSimpleDto>>
            {
                Success = true,
                Data = employees
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employees by department");
            return StatusCode(500, new ServiceResponse<List<EmployeeSimpleDto>>
            {
                Success = false,
                Message = "An error occurred while fetching employees"
            });
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<IActionResult> ExportAttendanceReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? employeeId, [FromQuery] Guid? departmentId, [FromQuery] string? status, [FromQuery] string format = "xlsx")
    {
        try
        {
            if (!new[] { "csv", "xlsx" }.Contains(format.ToLower()))
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid export format. Allowed formats are 'csv' and 'xlsx'"
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var currentEmployeeId = await GetCurrentEmployeeId();
            var fileBytes = await _attendanceService.ExportAttendanceReportAsync(organizationId, startDate, endDate, format, employeeId, currentEmployeeId,departmentId, status);
            
            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = $"attendance-report-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.{format}";
            
            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for exporting attendance report");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var result = await _attendanceService.BulkApproveAttendanceAsync(request.AttendanceIds);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully approved {request.AttendanceIds.Count} attendance records",
                Data = result
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for bulk approve attendance");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for bulk approve attendance");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var result = await _attendanceService.BulkRejectAttendanceAsync(request.AttendanceIds, request.Reason);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = $"Successfully rejected {request.AttendanceIds.Count} attendance records",
                Data = result
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for bulk reject attendance");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for bulk reject attendance");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
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

    private async Task<Guid> GetCurrentEmployeeId()
    {
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
        var employeeId = await _attendanceService.GetEmployeeFromTable(userId);
        return employeeId;
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

    private string GetClientIp()
    {
        var deviceIp = HttpContext.Request.Headers["X-Device-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceIp))
            return deviceIp;
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        string ip = !string.IsNullOrEmpty(forwardedFor)
            ? forwardedFor.Split(',')[0].Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";
        if (ip == "::1")
            ip = "127.0.0.1";
        if (ip.StartsWith("::ffff:"))
            ip = ip.Substring("::ffff:".Length);

        return ip;
    }



}
