using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<HolidayService> _logger;

    public HolidayService(IHolidayRepository holidayRepository, IEmployeeRepository employeeRepository, ILogger<HolidayService> logger)
    {
        _holidayRepository = holidayRepository;
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    // ========================
    // Catalog
    // ========================

    public async Task<ServiceResponse<IEnumerable<HolidayCatalogDto>>> GetCatalogByCountryAsync(string countryCode)
    {
        try
        {
            var holidays = await _holidayRepository.GetCatalogByCountryAsync(countryCode);
            return new ServiceResponse<IEnumerable<HolidayCatalogDto>> { Success = true, Data = holidays };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holiday catalog for country: {CountryCode}", countryCode);
            return new ServiceResponse<IEnumerable<HolidayCatalogDto>> { Success = false, Message = "Failed to retrieve holiday catalog" };
        }
    }

    public async Task<ServiceResponse<IEnumerable<CatalogCountryDto>>> GetAvailableCountriesAsync()
    {
        try
        {
            var countries = await _holidayRepository.GetAvailableCountriesAsync();
            return new ServiceResponse<IEnumerable<CatalogCountryDto>> { Success = true, Data = countries };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available countries");
            return new ServiceResponse<IEnumerable<CatalogCountryDto>> { Success = false, Message = "Failed to retrieve countries" };
        }
    }

    // ========================
    // Company Holidays
    // ========================

    public async Task<ServiceResponse<IEnumerable<CompanyHolidayDto>>> GetCompanyHolidaysAsync(Guid organizationId, int year)
    {
        try
        {
            var holidays = await _holidayRepository.GetCompanyHolidaysAsync(organizationId, year);
            
            // Populate EmployeeIds for holidays with applicableTo='employee'
            var holidayList = holidays.ToList();
            foreach (var h in holidayList.Where(h => h.ApplicableTo == "employee"))
            {
                h.EmployeeIds = (await _holidayRepository.GetEmployeeAssignmentsAsync(h.HolidayId)).ToList();
            }
            
            return new ServiceResponse<IEnumerable<CompanyHolidayDto>> { Success = true, Data = holidayList };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company holidays for org: {OrgId}, year: {Year}", organizationId, year);
            return new ServiceResponse<IEnumerable<CompanyHolidayDto>> { Success = false, Message = "Failed to retrieve company holidays" };
        }
    }

    public async Task<ServiceResponse<CompanyHolidayDto>> GetCompanyHolidayByIdAsync(Guid holidayId, Guid organizationId)
    {
        try
        {
            var holiday = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);
            if (holiday == null)
                return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "Holiday not found" };

            // Populate EmployeeIds if applicable
            if (holiday.ApplicableTo == "employee")
            {
                holiday.EmployeeIds = (await _holidayRepository.GetEmployeeAssignmentsAsync(holidayId)).ToList();
            }

            return new ServiceResponse<CompanyHolidayDto> { Success = true, Data = holiday };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company holiday: {HolidayId}", holidayId);
            return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "Failed to retrieve holiday" };
        }
    }

    public async Task<ServiceResponse<CompanyHolidayDto>> CreateCompanyHolidayAsync(CreateCompanyHolidayDto dto, Guid organizationId, Guid createdBy)
    {
        try
        {
            // Check for duplicate
            var exists = await _holidayRepository.CompanyHolidayExistsAsync(organizationId, dto.HolidayName, dto.HolidayDate);
            if (exists)
                return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "A holiday with this name or date already exists for this year" };

            var entity = new CompanyHoliday
            {
                OrganizationId = organizationId,
                HolidayName = dto.HolidayName,
                HolidayDate = dto.HolidayDate,
                HolidayType = dto.HolidayType,
                Description = dto.Description,
                IsHalfDay = dto.IsHalfDay,
                Year = dto.HolidayDate.Year,
                ApplicableTo = dto.ApplicableTo,
                ApplicableValue = dto.ApplicableValue,
                CatalogHolidayId = dto.CatalogHolidayId,
                CreatedBy = createdBy,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var holidayId = await _holidayRepository.CreateCompanyHolidayAsync(entity);

            // Save employee assignments if applicableTo is 'employee'
            if (dto.ApplicableTo == "employee" && dto.EmployeeIds != null && dto.EmployeeIds.Any())
            {
                await _holidayRepository.SaveEmployeeAssignmentsAsync(holidayId, dto.EmployeeIds, createdBy);
            }

            var created = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);

            // Populate EmployeeIds for the response
            if (created != null && created.ApplicableTo == "employee")
            {
                created.EmployeeIds = (await _holidayRepository.GetEmployeeAssignmentsAsync(holidayId)).ToList();
            }

            return new ServiceResponse<CompanyHolidayDto> { Success = true, Data = created, Message = "Holiday created successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company holiday for org: {OrgId}", organizationId);
            return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "Failed to create holiday" };
        }
    }

    public async Task<ServiceResponse<CompanyHolidayDto>> UpdateCompanyHolidayAsync(Guid holidayId, UpdateCompanyHolidayDto dto, Guid organizationId)
    {
        try
        {
            var existing = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);
            if (existing == null)
                return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "Holiday not found" };

            // Check duplicate (excluding current)
            var duplicate = await _holidayRepository.CompanyHolidayExistsAsync(organizationId, dto.HolidayName, dto.HolidayDate, holidayId);
            if (duplicate)
                return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "A holiday with this name or date already exists" };

            var entity = new CompanyHoliday
            {
                HolidayId = holidayId,
                OrganizationId = organizationId,
                HolidayName = dto.HolidayName,
                HolidayDate = dto.HolidayDate,
                HolidayType = dto.HolidayType,
                Description = dto.Description,
                IsHalfDay = dto.IsHalfDay,
                Year = dto.HolidayDate.Year,
                ApplicableTo = dto.ApplicableTo,
                ApplicableValue = dto.ApplicableValue,
                IsActive = dto.IsActive,
                UpdatedAt = DateTime.UtcNow
            };

            await _holidayRepository.UpdateCompanyHolidayAsync(entity);

            // Save/clear employee assignments
            if (dto.ApplicableTo == "employee" && dto.EmployeeIds != null && dto.EmployeeIds.Any())
            {
                await _holidayRepository.SaveEmployeeAssignmentsAsync(holidayId, dto.EmployeeIds, Guid.Empty);
            }
            else if (dto.ApplicableTo != "employee")
            {
                await _holidayRepository.DeleteEmployeeAssignmentsAsync(holidayId);
            }

            var updated = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);

            // Populate EmployeeIds for the response
            if (updated != null && updated.ApplicableTo == "employee")
            {
                updated.EmployeeIds = (await _holidayRepository.GetEmployeeAssignmentsAsync(holidayId)).ToList();
            }

            return new ServiceResponse<CompanyHolidayDto> { Success = true, Data = updated, Message = "Holiday updated successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating holiday: {HolidayId}", holidayId);
            return new ServiceResponse<CompanyHolidayDto> { Success = false, Message = "Failed to update holiday" };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteCompanyHolidayAsync(Guid holidayId, Guid organizationId)
    {
        try
        {
            var existing = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);
            if (existing == null)
                return new ServiceResponse<bool> { Success = false, Message = "Holiday not found", Data = false };

            var result = await _holidayRepository.DeleteCompanyHolidayAsync(holidayId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Holiday deleted successfully" : "Failed to delete holiday" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday: {HolidayId}", holidayId);
            return new ServiceResponse<bool> { Success = false, Message = "Failed to delete holiday", Data = false };
        }
    }

    public async Task<ServiceResponse<List<CompanyHolidayDto>>> ImportFromCatalogAsync(ImportFromCatalogDto dto, Guid organizationId, Guid createdBy)
    {
        try
        {
            var catalogHolidays = await _holidayRepository.GetCatalogHolidaysByIdsAsync(dto.CatalogHolidayIds);
            var imported = new List<CompanyHolidayDto>();
            var skipped = 0;

            foreach (var catalog in catalogHolidays)
            {
                var targetYear = dto.Year ?? catalog.HolidayDate.Year;
                var targetDate = new DateTime(targetYear, catalog.HolidayDate.Month, catalog.HolidayDate.Day);

                // Check if already exists
                var exists = await _holidayRepository.CompanyHolidayExistsAsync(organizationId, catalog.HolidayName, targetDate);
                if (exists)
                {
                    skipped++;
                    continue;
                }

                var entity = new CompanyHoliday
                {
                    OrganizationId = organizationId,
                    HolidayName = catalog.HolidayName,
                    HolidayDate = targetDate,
                    HolidayType = dto.HolidayType,
                    Description = catalog.Description,
                    IsHalfDay = false,
                    Year = targetYear,
                    ApplicableTo = "all",
                    CatalogHolidayId = catalog.CatalogId,
                    CreatedBy = createdBy,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var holidayId = await _holidayRepository.CreateCompanyHolidayAsync(entity);
                var created = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);
                if (created != null)
                    imported.Add(created);
            }

            var message = $"Imported {imported.Count} holidays";
            if (skipped > 0) message += $" ({skipped} skipped as duplicates)";

            return new ServiceResponse<List<CompanyHolidayDto>> { Success = true, Data = imported, Message = message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing holidays from catalog for org: {OrgId}", organizationId);
            return new ServiceResponse<List<CompanyHolidayDto>> { Success = false, Message = "Failed to import holidays" };
        }
    }

    // ========================
    // Holiday Queries
    // ========================

    public async Task<ServiceResponse<IEnumerable<HolidayRangeDto>>> GetHolidaysInRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var holidays = await _holidayRepository.GetHolidaysInRangeAsync(organizationId, startDate, endDate);
            return new ServiceResponse<IEnumerable<HolidayRangeDto>> { Success = true, Data = holidays };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays in range for org: {OrgId}", organizationId);
            return new ServiceResponse<IEnumerable<HolidayRangeDto>> { Success = false, Message = "Failed to retrieve holidays" };
        }
    }

    public async Task<ServiceResponse<IEnumerable<HolidayRangeDto>>> GetHolidaysInRangeForEmployeeAsync(
        Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation,
        DateTime startDate, DateTime endDate)
    {
        try
        {
            // Resolve employee's department and location if not provided
            if (departmentId == null || string.IsNullOrEmpty(workLocation))
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
                if (employee != null)
                {
                    departmentId ??= employee.DepartmentId;
                    workLocation ??= employee.WorkLocation;
                }
            }

            var holidays = await _holidayRepository.GetHolidaysInRangeForEmployeeAsync(
                organizationId, employeeId, departmentId, workLocation, startDate, endDate);
            return new ServiceResponse<IEnumerable<HolidayRangeDto>> { Success = true, Data = holidays };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays in range for employee: {EmployeeId}", employeeId);
            return new ServiceResponse<IEnumerable<HolidayRangeDto>> { Success = false, Message = "Failed to retrieve holidays" };
        }
    }

    public async Task<ServiceResponse<IEnumerable<Guid>>> GetEmployeeAssignmentsAsync(Guid holidayId, Guid organizationId)
    {
        try
        {
            // Verify holiday belongs to org
            var holiday = await _holidayRepository.GetCompanyHolidayByIdAsync(holidayId, organizationId);
            if (holiday == null)
                return new ServiceResponse<IEnumerable<Guid>> { Success = false, Message = "Holiday not found" };

            var employeeIds = await _holidayRepository.GetEmployeeAssignmentsAsync(holidayId);
            return new ServiceResponse<IEnumerable<Guid>> { Success = true, Data = employeeIds };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee assignments for holiday: {HolidayId}", holidayId);
            return new ServiceResponse<IEnumerable<Guid>> { Success = false, Message = "Failed to retrieve assignments" };
        }
    }

    public async Task<ServiceResponse<bool>> IsHolidayAsync(Guid organizationId, DateTime date)
    {
        try
        {
            var isHoliday = await _holidayRepository.IsHolidayAsync(organizationId, date);
            return new ServiceResponse<bool> { Success = true, Data = isHoliday };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking holiday for date: {Date}", date);
            return new ServiceResponse<bool> { Success = false, Message = "Failed to check holiday", Data = false };
        }
    }

    public async Task<ServiceResponse<HolidaySummaryDto>> GetHolidaySummaryAsync(Guid organizationId, int year)
    {
        try
        {
            var summary = await _holidayRepository.GetHolidaySummaryAsync(organizationId, year);
            return new ServiceResponse<HolidaySummaryDto> { Success = true, Data = summary };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holiday summary for org: {OrgId}", organizationId);
            return new ServiceResponse<HolidaySummaryDto> { Success = false, Message = "Failed to retrieve holiday summary" };
        }
    }

    // ========================
    // Employee Restricted Holidays
    // ========================

    public async Task<ServiceResponse<IEnumerable<EmployeeRestrictedHolidayDto>>> GetEmployeeRestrictedHolidaysAsync(Guid employeeId, Guid organizationId, int year)
    {
        try
        {
            var holidays = await _holidayRepository.GetEmployeeRestrictedHolidaysAsync(employeeId, organizationId, year);
            return new ServiceResponse<IEnumerable<EmployeeRestrictedHolidayDto>> { Success = true, Data = holidays };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting restricted holidays for employee: {EmployeeId}", employeeId);
            return new ServiceResponse<IEnumerable<EmployeeRestrictedHolidayDto>> { Success = false, Message = "Failed to retrieve restricted holidays" };
        }
    }

    public async Task<ServiceResponse<IEnumerable<CompanyHolidayDto>>> GetAvailableRestrictedHolidaysAsync(Guid organizationId, int year)
    {
        try
        {
            var holidays = await _holidayRepository.GetAvailableRestrictedHolidaysAsync(organizationId, year);
            return new ServiceResponse<IEnumerable<CompanyHolidayDto>> { Success = true, Data = holidays };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available restricted holidays for org: {OrgId}", organizationId);
            return new ServiceResponse<IEnumerable<CompanyHolidayDto>> { Success = false, Message = "Failed to retrieve restricted holidays" };
        }
    }

    public async Task<ServiceResponse<IEnumerable<CompanyHolidayDto>>> GetAvailableRestrictedHolidaysForEmployeeAsync(
        Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation, int year)
    {
        try
        {
            if (departmentId == null || string.IsNullOrEmpty(workLocation))
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
                if (employee != null)
                {
                    departmentId ??= employee.DepartmentId;
                    workLocation ??= employee.WorkLocation;
                }
            }

            var holidays = await _holidayRepository.GetAvailableRestrictedHolidaysForEmployeeAsync(
                organizationId, employeeId, departmentId, workLocation, year);
            return new ServiceResponse<IEnumerable<CompanyHolidayDto>> { Success = true, Data = holidays };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available restricted holidays for employee: {EmployeeId}", employeeId);
            return new ServiceResponse<IEnumerable<CompanyHolidayDto>> { Success = false, Message = "Failed to retrieve restricted holidays" };
        }
    }

    public async Task<ServiceResponse<EmployeeRestrictedHolidayDto>> SelectRestrictedHolidayAsync(Guid employeeId, Guid organizationId, SelectRestrictedHolidayDto dto)
    {
        try
        {
            // Verify the holiday is a restricted type
            var holiday = await _holidayRepository.GetCompanyHolidayByIdAsync(dto.CompanyHolidayId, organizationId);
            if (holiday == null)
                return new ServiceResponse<EmployeeRestrictedHolidayDto> { Success = false, Message = "Holiday not found" };

            if (holiday.HolidayType != "restricted")
                return new ServiceResponse<EmployeeRestrictedHolidayDto> { Success = false, Message = "This holiday is not a restricted holiday" };

            // Enforce scope applicability at API level to prevent cross-employee selection by crafted requests
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            var applicableRestricted = await _holidayRepository.GetAvailableRestrictedHolidaysForEmployeeAsync(
                organizationId,
                employeeId,
                employee?.DepartmentId,
                employee?.WorkLocation,
                holiday.Year);
            if (!applicableRestricted.Any(h => h.HolidayId == dto.CompanyHolidayId))
            {
                return new ServiceResponse<EmployeeRestrictedHolidayDto>
                {
                    Success = false,
                    Message = "This restricted holiday is not applicable to your profile"
                };
            }

            if (holiday.HolidayDate < DateTime.UtcNow.Date)
                return new ServiceResponse<EmployeeRestrictedHolidayDto> { Success = false, Message = "Cannot select a holiday that has already passed" };

            var entity = new EmployeeRestrictedHoliday
            {
                OrganizationId = organizationId,
                EmployeeId = employeeId,
                CompanyHolidayId = dto.CompanyHolidayId,
                Status = "selected",
                SelectedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _holidayRepository.SelectRestrictedHolidayAsync(entity);

            // Return the latest state
            var selected = (await _holidayRepository.GetEmployeeRestrictedHolidaysAsync(employeeId, organizationId, holiday.Year))
                .FirstOrDefault(h => h.CompanyHolidayId == dto.CompanyHolidayId);

            return new ServiceResponse<EmployeeRestrictedHolidayDto> { Success = true, Data = selected, Message = "Restricted holiday selected successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting restricted holiday for employee: {EmployeeId}", employeeId);
            return new ServiceResponse<EmployeeRestrictedHolidayDto> { Success = false, Message = "Failed to select restricted holiday" };
        }
    }

    public async Task<ServiceResponse<bool>> CancelRestrictedHolidayAsync(Guid id, Guid employeeId)
    {
        try
        {
            var result = await _holidayRepository.CancelRestrictedHolidayAsync(id, employeeId);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Restricted holiday cancelled successfully" : "Failed to cancel restricted holiday"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling restricted holiday: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "Failed to cancel restricted holiday", Data = false };
        }
    }
}
