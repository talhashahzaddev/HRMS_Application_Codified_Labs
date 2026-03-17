using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IHolidayService
{
    // Catalog
    Task<ServiceResponse<IEnumerable<HolidayCatalogDto>>> GetCatalogByCountryAsync(string countryCode);
    Task<ServiceResponse<IEnumerable<CatalogCountryDto>>> GetAvailableCountriesAsync();

    // Company Holidays
    Task<ServiceResponse<IEnumerable<CompanyHolidayDto>>> GetCompanyHolidaysAsync(Guid organizationId, int year);
    Task<ServiceResponse<CompanyHolidayDto>> GetCompanyHolidayByIdAsync(Guid holidayId, Guid organizationId);
    Task<ServiceResponse<CompanyHolidayDto>> CreateCompanyHolidayAsync(CreateCompanyHolidayDto dto, Guid organizationId, Guid createdBy);
    Task<ServiceResponse<CompanyHolidayDto>> UpdateCompanyHolidayAsync(Guid holidayId, UpdateCompanyHolidayDto dto, Guid organizationId);
    Task<ServiceResponse<bool>> DeleteCompanyHolidayAsync(Guid holidayId, Guid organizationId);
    Task<ServiceResponse<List<CompanyHolidayDto>>> ImportFromCatalogAsync(ImportFromCatalogDto dto, Guid organizationId, Guid createdBy);

    // Holiday Queries (for calendar/attendance integration)
    Task<ServiceResponse<IEnumerable<HolidayRangeDto>>> GetHolidaysInRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    Task<ServiceResponse<IEnumerable<HolidayRangeDto>>> GetHolidaysInRangeForEmployeeAsync(Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation, DateTime startDate, DateTime endDate);
    Task<ServiceResponse<bool>> IsHolidayAsync(Guid organizationId, DateTime date);
    Task<ServiceResponse<HolidaySummaryDto>> GetHolidaySummaryAsync(Guid organizationId, int year);

    // Employee Holiday Assignments
    Task<ServiceResponse<IEnumerable<Guid>>> GetEmployeeAssignmentsAsync(Guid holidayId, Guid organizationId);

    // Employee Restricted Holidays
    Task<ServiceResponse<IEnumerable<EmployeeRestrictedHolidayDto>>> GetEmployeeRestrictedHolidaysAsync(Guid employeeId, Guid organizationId, int year);
    Task<ServiceResponse<IEnumerable<CompanyHolidayDto>>> GetAvailableRestrictedHolidaysAsync(Guid organizationId, int year);
    Task<ServiceResponse<IEnumerable<CompanyHolidayDto>>> GetAvailableRestrictedHolidaysForEmployeeAsync(Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation, int year);
    Task<ServiceResponse<EmployeeRestrictedHolidayDto>> SelectRestrictedHolidayAsync(Guid employeeId, Guid organizationId, SelectRestrictedHolidayDto dto);
    Task<ServiceResponse<bool>> CancelRestrictedHolidayAsync(Guid id, Guid employeeId);
}
