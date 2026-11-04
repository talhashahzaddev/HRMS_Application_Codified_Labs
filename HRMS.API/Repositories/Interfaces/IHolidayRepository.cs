using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IHolidayRepository
{
    // Catalog
    Task<IReadOnlyList<HolidayCatalogDto>> GetCatalogByCountryAsync(string countryCode);
    Task<IReadOnlyList<CatalogCountryDto>> GetAvailableCountriesAsync();
    Task<HolidayCatalogDto?> GetCatalogHolidayByIdAsync(Guid catalogId);
    Task<IReadOnlyList<HolidayCatalogDto>> GetCatalogHolidaysByIdsAsync(IEnumerable<Guid> catalogIds);

    // Company Holidays
    Task<IReadOnlyList<CompanyHolidayDto>> GetCompanyHolidaysAsync(Guid organizationId, int year);
    Task<CompanyHolidayDto?> GetCompanyHolidayByIdAsync(Guid holidayId, Guid organizationId);
    Task<Guid> CreateCompanyHolidayAsync(CompanyHoliday holiday);
    Task<bool> UpdateCompanyHolidayAsync(CompanyHoliday holiday);
    Task<bool> DeleteCompanyHolidayAsync(Guid holidayId, Guid organizationId);
    Task<bool> CompanyHolidayExistsAsync(Guid organizationId, string holidayName, DateTime holidayDate, Guid? excludeId = null);

    // Holiday Range Queries (for calendar/attendance integration)
    Task<IReadOnlyList<HolidayRangeDto>> GetHolidaysInRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<HolidayRangeDto>> GetHolidaysInRangeForEmployeeAsync(Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation, DateTime startDate, DateTime endDate);
    Task<bool> IsHolidayAsync(Guid organizationId, DateTime date);
    Task<HolidaySummaryDto> GetHolidaySummaryAsync(Guid organizationId, int year);

    // Employee Holiday Assignments
    Task SaveEmployeeAssignmentsAsync(Guid holidayId, IEnumerable<Guid> employeeIds, Guid assignedBy);
    Task<IReadOnlyList<Guid>> GetEmployeeAssignmentsAsync(Guid holidayId);
    Task DeleteEmployeeAssignmentsAsync(Guid holidayId);

    // Employee Restricted Holidays
    Task<IReadOnlyList<EmployeeRestrictedHolidayDto>> GetEmployeeRestrictedHolidaysAsync(Guid employeeId, Guid organizationId, int year);
    Task<IReadOnlyList<CompanyHolidayDto>> GetAvailableRestrictedHolidaysAsync(Guid organizationId, int year);
    Task<IReadOnlyList<CompanyHolidayDto>> GetAvailableRestrictedHolidaysForEmployeeAsync(Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation, int year);
    Task<Guid> SelectRestrictedHolidayAsync(EmployeeRestrictedHoliday selection);
    Task<bool> CancelRestrictedHolidayAsync(Guid id, Guid employeeId);
    Task<int> GetEmployeeRestrictedHolidayCountAsync(Guid employeeId, Guid organizationId, int year);
}
