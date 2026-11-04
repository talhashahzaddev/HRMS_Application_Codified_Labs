using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayController : ControllerBase
{
    private readonly IHolidayService _holidayService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<HolidayController> _logger;

    [ActivatorUtilitiesConstructor]
    public HolidayController(IHolidayService holidayService, IEmployeeRepository employeeRepository, ILogger<HolidayController> logger)
    {
        _holidayService = holidayService;
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    // ========================
    // Catalog Endpoints
    // ========================

    [HttpGet("catalog/countries")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<CatalogCountryDto>>>> GetAvailableCountries()
    {
        try
        {
            var result = await _holidayService.GetAvailableCountriesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available countries");
            return StatusCode(500, new ServiceResponse<IEnumerable<CatalogCountryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving countries"
            });
        }
    }

    [HttpGet("catalog/{countryCode}")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<HolidayCatalogDto>>>> GetCatalogByCountry(string countryCode)
    {
        try
        {
            var result = await _holidayService.GetCatalogByCountryAsync(countryCode);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting catalog for country: {CountryCode}", countryCode);
            return StatusCode(500, new ServiceResponse<IEnumerable<HolidayCatalogDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving holiday catalog"
            });
        }
    }

    // ========================
    // Company Holiday Endpoints
    // ========================

    [HttpGet]
    [PermissionAuthorize("Holiday_Table")]

    public async Task<ActionResult<ServiceResponse<IEnumerable<CompanyHolidayDto>>>> GetCompanyHolidays([FromQuery] int? year)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var result = await _holidayService.GetCompanyHolidaysAsync(organizationId, targetYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company holidays");
            return StatusCode(500, new ServiceResponse<IEnumerable<CompanyHolidayDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving holidays"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<CompanyHolidayDto>>> GetCompanyHoliday(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.GetCompanyHolidayByIdAsync(id, organizationId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holiday: {HolidayId}", id);
            return StatusCode(500, new ServiceResponse<CompanyHolidayDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the holiday"
            });
        }
    }

    [HttpPost]
    [PermissionAuthorize("add_holidays")]

    public async Task<ActionResult<ServiceResponse<CompanyHolidayDto>>> CreateCompanyHoliday([FromBody] CreateCompanyHolidayDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeIdAsync();
            var result = await _holidayService.CreateCompanyHolidayAsync(dto, organizationId, employeeId);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetCompanyHoliday), new { id = result.Data?.HolidayId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating holiday");
            return StatusCode(500, new ServiceResponse<CompanyHolidayDto>
            {
                Success = false,
                Message = "An error occurred while creating the holiday"
            });
        }
    }

    [HttpPut("{id}")]
    [PermissionAuthorize(" edit_holidays_details")]
    public async Task<ActionResult<ServiceResponse<CompanyHolidayDto>>> UpdateCompanyHoliday(Guid id, [FromBody] UpdateCompanyHolidayDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.UpdateCompanyHolidayAsync(id, dto, organizationId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating holiday: {HolidayId}", id);
            return StatusCode(500, new ServiceResponse<CompanyHolidayDto>
            {
                Success = false,
                Message = "An error occurred while updating the holiday"
            });
        }
    }

    [HttpDelete("{id}")]
    [PermissionAuthorize("delete_holidays_details")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteCompanyHoliday(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.DeleteCompanyHolidayAsync(id, organizationId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday: {HolidayId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the holiday"
            });
        }
    }

    [HttpPost("import-from-catalog")]
    [PermissionAuthorize("import_holidays_from_catalog")]
    public async Task<ActionResult<ServiceResponse<List<CompanyHolidayDto>>>> ImportFromCatalog([FromBody] ImportFromCatalogDto dto)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeIdAsync();
            var result = await _holidayService.ImportFromCatalogAsync(dto, organizationId, employeeId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing holidays from catalog");
            return StatusCode(500, new ServiceResponse<List<CompanyHolidayDto>>
            {
                Success = false,
                Message = "An error occurred while importing holidays"
            });
        }
    }

    // ========================
    // Employee Assignment Endpoints
    // ========================

    [HttpGet("{holidayId}/assignments")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<Guid>>>> GetEmployeeAssignments(Guid holidayId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.GetEmployeeAssignmentsAsync(holidayId, organizationId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee assignments for holiday: {HolidayId}", holidayId);
            return StatusCode(500, new ServiceResponse<IEnumerable<Guid>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee assignments"
            });
        }
    }

    // ========================
    // Holiday Range Queries (for calendar/attendance)
    // ========================

    [HttpGet("range")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<HolidayRangeDto>>>> GetHolidaysInRange(
        [FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.GetHolidaysInRangeAsync(organizationId, start, end);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays in range");
            return StatusCode(500, new ServiceResponse<IEnumerable<HolidayRangeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving holidays"
            });
        }
    }

    /// <summary>
    /// Get holidays in a date range filtered for the current employee's department, location, and assignments.
    /// </summary>
    [HttpGet("range/my")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<HolidayRangeDto>>>> GetMyHolidaysInRange(
        [FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeIdAsync();
            var result = await _holidayService.GetHolidaysInRangeForEmployeeAsync(
                organizationId, employeeId, null, null, start, end);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee-specific holidays in range");
            return StatusCode(500, new ServiceResponse<IEnumerable<HolidayRangeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving holidays"
            });
        }
    }

    [HttpGet("check")]
    public async Task<ActionResult<ServiceResponse<bool>>> IsHoliday([FromQuery] DateTime date)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.IsHolidayAsync(organizationId, date);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking holiday");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpGet("summary")]
    [PermissionAuthorize("Holiday_Summary")]
    public async Task<ActionResult<ServiceResponse<HolidaySummaryDto>>> GetHolidaySummary([FromQuery] int? year)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var result = await _holidayService.GetHolidaySummaryAsync(organizationId, targetYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holiday summary");
            return StatusCode(500, new ServiceResponse<HolidaySummaryDto>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    // ========================
    // Employee Restricted Holidays
    // ========================

    [HttpGet("restricted")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<CompanyHolidayDto>>>> GetAvailableRestrictedHolidays([FromQuery] int? year)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var result = await _holidayService.GetAvailableRestrictedHolidaysAsync(organizationId, targetYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting restricted holidays");
            return StatusCode(500, new ServiceResponse<IEnumerable<CompanyHolidayDto>>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpGet("restricted/available/my")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<CompanyHolidayDto>>>> GetMyAvailableRestrictedHolidays([FromQuery] int? year)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            var organizationId = GetCurrentOrganizationId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var result = await _holidayService.GetAvailableRestrictedHolidaysForEmployeeAsync(
                organizationId, employeeId, null, null, targetYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee available restricted holidays");
            return StatusCode(500, new ServiceResponse<IEnumerable<CompanyHolidayDto>>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpGet("my-holidays")]
    public async Task<ActionResult<ServiceResponse<IEnumerable<EmployeeRestrictedHolidayDto>>>> GetMyRestrictedHolidays([FromQuery] int? year)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            var organizationId = GetCurrentOrganizationId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var result = await _holidayService.GetEmployeeRestrictedHolidaysAsync(employeeId, organizationId, targetYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my restricted holidays");
            return StatusCode(500, new ServiceResponse<IEnumerable<EmployeeRestrictedHolidayDto>>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpPost("restricted/select")]
    public async Task<ActionResult<ServiceResponse<EmployeeRestrictedHolidayDto>>> SelectRestrictedHoliday([FromBody] SelectRestrictedHolidayDto dto)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            var organizationId = GetCurrentOrganizationId();
            var result = await _holidayService.SelectRestrictedHolidayAsync(employeeId, organizationId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting restricted holiday");
            return StatusCode(500, new ServiceResponse<EmployeeRestrictedHolidayDto>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpDelete("restricted/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> CancelRestrictedHoliday(Guid id)
    {
        try
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            var result = await _holidayService.CancelRestrictedHolidayAsync(id, employeeId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling restricted holiday");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    // ========================
    // Helper Methods
    // ========================

    private async Task<Guid> GetCurrentEmployeeIdAsync()
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

        var employeeId = await _employeeRepository.GetEmployeeIdAsync(userId);
        if (employeeId == null)
        {
            _logger.LogError("Employee not found for User ID: {UserId}", userId);
            throw new UnauthorizedAccessException("Employee profile not found");
        }

        return employeeId.Value;
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
            _logger.LogError("Invalid organization ID format in JWT token: {OrgId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }

        return organizationId;
    }
}
