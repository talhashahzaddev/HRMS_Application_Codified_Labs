using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeoFenceController : ControllerBase
{
    private readonly IGeoFenceService _service;
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<GeoFenceController> _logger;

    public GeoFenceController(
        IGeoFenceService service,
        IAttendanceService attendanceService,
        ILogger<GeoFenceController> logger)
    {
        _service = service;
        _attendanceService = attendanceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<IEnumerable<GeoFenceDto>>>> GetAll()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var fences = await _service.GetAllGeoFencesAsync(orgId);
            return Ok(new ServiceResponse<IEnumerable<GeoFenceDto>> { Success = true, Data = fences });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting geo-fences");
            return StatusCode(500, new ServiceResponse<IEnumerable<GeoFenceDto>>
            { Success = false, Message = "An error occurred." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<GeoFenceDto>>> GetById(Guid id)
    {
        try
        {
            var fence = await _service.GetGeoFenceByIdAsync(id);
            if (fence == null)
                return NotFound(new ServiceResponse<GeoFenceDto> { Success = false, Message = "Not found." });

            return Ok(new ServiceResponse<GeoFenceDto> { Success = true, Data = fence });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting geo-fence {Id}", id);
            return StatusCode(500, new ServiceResponse<GeoFenceDto> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse<Guid>>> Create([FromBody] CreateGeoFenceDto dto)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var empId = await GetCurrentEmployeeId();
            var result = await _service.CreateGeoFenceAsync(orgId, empId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating geo-fence");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> Update(Guid id, [FromBody] UpdateGeoFenceDto dto)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.UpdateGeoFenceAsync(id, orgId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating geo-fence {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> Delete(Guid id)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.DeleteGeoFenceAsync(id, orgId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting geo-fence {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<ServiceResponse<bool>>> Toggle(Guid id, [FromBody] bool isActive)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var result = await _service.ToggleActiveAsync(id, orgId, isActive);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling geo-fence {Id}", id);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred." });
        }
    }

    // ── Shift ↔ Geo-Fence ─────────────────────────────────────────────────

    [HttpGet("shift/{shiftId}")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<ShiftGeoFenceDto>>>> GetByShift(Guid shiftId)
    {
        var fences = await _service.GetGeoFencesByShiftAsync(shiftId);
        return Ok(new ServiceResponse<IEnumerable<ShiftGeoFenceDto>> { Success = true, Data = fences });
    }

    [HttpPost("shift/{shiftId}/link/{geoFenceId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> LinkToShift(Guid shiftId, Guid geoFenceId)
    {
        try
        {
            var result = await _service.LinkGeoFenceToShiftAsync(shiftId, geoFenceId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking geo-fence {GeoFenceId} to shift {ShiftId}", geoFenceId, shiftId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpDelete("shift/{shiftId}/unlink/{geoFenceId}")]
    public async Task<ActionResult<ServiceResponse<bool>>> UnlinkFromShift(Guid shiftId, Guid geoFenceId)
    {
        try
        {
            var result = await _service.UnlinkGeoFenceFromShiftAsync(shiftId, geoFenceId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking geo-fence {GeoFenceId} from shift {ShiftId}", geoFenceId, shiftId);
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = "An error occurred." });
        }
    }

    // ── Face Recognition ──────────────────────────────────────────────────

    [HttpPost("face-descriptor")]
    public async Task<ActionResult<ServiceResponse<Guid>>> SaveFaceDescriptor([FromBody] SaveFaceDescriptorRequest dto)
    {
        try
        {
            var empId = await GetCurrentEmployeeId();
            var result = await _service.SaveFaceDescriptorAsync(empId, empId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving face descriptor");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred." });
        }
    }

    [HttpPost("face-log")]
    public async Task<ActionResult<ServiceResponse<Guid>>> SubmitFaceLog([FromBody] SubmitFaceLogRequest dto)
    {
        try
        {
            var empId = await GetCurrentEmployeeId();
            var result = await _service.SubmitFaceLogAsync(empId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting face log");
            return StatusCode(500, new ServiceResponse<Guid> { Success = false, Message = "An error occurred." });
        }
    }

    // ── Geo-Aware Clock In/Out ─────────────────────────────────────────────

    [HttpPost("clock")]
    public async Task<ActionResult<ServiceResponse<GeoClockInResponse>>> GeoClock([FromBody] GeoClockInRequest request)
    {
        try
        {
            var empId = await GetCurrentEmployeeId();
            var result = await _service.GeoClockInOutAsync(empId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in geo clock");
            return StatusCode(500, new ServiceResponse<GeoClockInResponse>
            { Success = false, Message = "Clock failed." });
        }
    }

    // ── Dashboard & Reports ────────────────────────────────────────────────

    [HttpGet("team-live-stream")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<TeamLiveStreamDto>>>> GetTeamLiveStream()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var data = await _service.GetTeamLiveStreamAsync(orgId);
            return Ok(new ServiceResponse<IEnumerable<TeamLiveStreamDto>> { Success = true, Data = data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team live stream");
            return StatusCode(500, new ServiceResponse<IEnumerable<TeamLiveStreamDto>>
            { Success = false, Message = "An error occurred." });
        }
    }

    [HttpGet("violations")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<GeoFenceViolationReportDto>>>> GetViolations(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] Guid? shiftId = null)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();

            // Parse as local date to avoid UTC midnight shifting to previous day
            if (!DateTime.TryParse(startDate, out var parsedStart))
                return BadRequest(new ServiceResponse<IEnumerable<GeoFenceViolationReportDto>>
                { Success = false, Message = "Invalid startDate format. Use yyyy-MM-dd." });

            if (!DateTime.TryParse(endDate, out var parsedEnd))
                return BadRequest(new ServiceResponse<IEnumerable<GeoFenceViolationReportDto>>
                { Success = false, Message = "Invalid endDate format. Use yyyy-MM-dd." });

            var data = await _service.GetViolationsReportAsync(orgId, parsedStart, parsedEnd, shiftId);
            return Ok(new ServiceResponse<IEnumerable<GeoFenceViolationReportDto>> { Success = true, Data = data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting violations report");
            return StatusCode(500, new ServiceResponse<IEnumerable<GeoFenceViolationReportDto>>
            { Success = false, Message = "An error occurred." });
        }
    }

    [HttpGet("system-health")]
    public async Task<ActionResult<ServiceResponse<SystemHealthDto>>> GetSystemHealth()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var data = await _service.GetSystemHealthAsync(orgId);
            return Ok(new ServiceResponse<SystemHealthDto> { Success = true, Data = data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(500, new ServiceResponse<SystemHealthDto> { Success = false, Message = "An error occurred." });
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var orgId))
            throw new UnauthorizedAccessException("Organization ID not found in token");
        return orgId;
    }

    private async Task<Guid> GetCurrentEmployeeId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token");
        return await _attendanceService.GetEmployeeFromTable(userId);
    }
}


