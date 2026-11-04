using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using System.Linq.Expressions;

namespace HRMS.API.Repositories;

public class HolidayRepository : BaseRepository, IHolidayRepository
{
    private readonly ILogger<HolidayRepository> _logger;

    public HolidayRepository(IDatabaseConnectionFactory connectionFactory, ILogger<HolidayRepository> logger)
        : base(connectionFactory)
    {
        _logger = logger;
    }

    // ========================
    // Catalog Methods
    // ========================

    public async Task<IReadOnlyList<HolidayCatalogDto>> GetCatalogByCountryAsync(string countryCode)
    {
        const string sql = @"
            SELECT catalogid AS CatalogId, countrycode AS CountryCode, countryname AS CountryName,
                   holidayname AS HolidayName, holidaydate AS HolidayDate, isrecurring AS IsRecurring,
                   description AS Description
            FROM attendance.holiday_catalogs
            WHERE LOWER(countrycode) = LOWER(@CountryCode)
            ORDER BY holidaydate";

        return await QueryAsync<HolidayCatalogDto>(sql, new { CountryCode = countryCode });
    }

    public async Task<IReadOnlyList<CatalogCountryDto>> GetAvailableCountriesAsync()
    {
        const string sql = @"
            SELECT countrycode AS CountryCode, countryname AS CountryName, COUNT(*) AS HolidayCount
            FROM attendance.holiday_catalogs
            GROUP BY countrycode, countryname
            ORDER BY countryname";

        return await QueryAsync<CatalogCountryDto>(sql);
    }

    public async Task<HolidayCatalogDto?> GetCatalogHolidayByIdAsync(Guid catalogId)
    {
        const string sql = @"
            SELECT catalogid AS CatalogId, countrycode AS CountryCode, countryname AS CountryName,
                   holidayname AS HolidayName, holidaydate AS HolidayDate, isrecurring AS IsRecurring,
                   description AS Description
            FROM attendance.holiday_catalogs
            WHERE catalogid = @CatalogId";

        return await QuerySingleOrDefaultAsync<HolidayCatalogDto>(sql, new { CatalogId = catalogId });
    }

    public async Task<IReadOnlyList<HolidayCatalogDto>> GetCatalogHolidaysByIdsAsync(IEnumerable<Guid> catalogIds)
    {
        const string sql = @"
            SELECT catalogid AS CatalogId, countrycode AS CountryCode, countryname AS CountryName,
                   holidayname AS HolidayName, holidaydate AS HolidayDate, isrecurring AS IsRecurring,
                   description AS Description
            FROM attendance.holiday_catalogs
            WHERE catalogid = ANY(@CatalogIds)
            ORDER BY holidaydate";

        return await QueryAsync<HolidayCatalogDto>(sql, new { CatalogIds = catalogIds.ToArray() });
    }

    // ========================
    // Company Holiday Methods
    // ========================

    public async Task<IReadOnlyList<CompanyHolidayDto>> GetCompanyHolidaysAsync(Guid organizationId, int year)
    {
        const string sql = @"
            SELECT holidayid AS HolidayId, organizationid AS OrganizationId, holidayname AS HolidayName,
                   holidaydate AS HolidayDate, holidaytype AS HolidayType, description AS Description,
                   ishalfday AS IsHalfDay, year AS Year, applicableto AS ApplicableTo,
                   applicablevalue AS ApplicableValue, catalogholidayid AS CatalogHolidayId,
                   createdby AS CreatedBy, isactive AS IsActive,
                   createdat AS CreatedAt, updatedat AS UpdatedAt
            FROM attendance.company_holidays
            WHERE organizationid = @OrganizationId AND year = @Year AND isactive = true
            ORDER BY holidaydate";

        return await QueryAsync<CompanyHolidayDto>(sql, new { OrganizationId = organizationId, Year = year });
    }

    public async Task<CompanyHolidayDto?> GetCompanyHolidayByIdAsync(Guid holidayId, Guid organizationId)
    {
        const string sql = @"
            SELECT holidayid AS HolidayId, organizationid AS OrganizationId, holidayname AS HolidayName,
                   holidaydate AS HolidayDate, holidaytype AS HolidayType, description AS Description,
                   ishalfday AS IsHalfDay, year AS Year, applicableto AS ApplicableTo,
                   applicablevalue AS ApplicableValue, catalogholidayid AS CatalogHolidayId,
                   createdby AS CreatedBy, isactive AS IsActive,
                   createdat AS CreatedAt, updatedat AS UpdatedAt
            FROM attendance.company_holidays
            WHERE holidayid = @HolidayId AND organizationid = @OrganizationId";

        return await QuerySingleOrDefaultAsync<CompanyHolidayDto>(sql, new { HolidayId = holidayId, OrganizationId = organizationId });
    }

    public async Task<Guid> CreateCompanyHolidayAsync(CompanyHoliday holiday)
    {
        const string sql = @"
            INSERT INTO attendance.company_holidays
                (organizationid, holidayname, holidaydate, holidaytype, description,
                 ishalfday, year, applicableto, applicablevalue, catalogholidayid,
                 createdby, isactive, createdat, updatedat)
            VALUES
                (@OrganizationId, @HolidayName, @HolidayDate, @HolidayType, @Description,
                 @IsHalfDay, @Year, @ApplicableTo, @ApplicableValue, @CatalogHolidayId,
                 @CreatedBy, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING holidayid";

        return await ExecuteScalarAsync<Guid>(sql, holiday);
    }

    public async Task<bool> UpdateCompanyHolidayAsync(CompanyHoliday holiday)
    {
        const string sql = @"
            UPDATE attendance.company_holidays
            SET holidayname = @HolidayName,
                holidaydate = @HolidayDate,
                holidaytype = @HolidayType,
                description = @Description,
                ishalfday = @IsHalfDay,
                applicableto = @ApplicableTo,
                applicablevalue = @ApplicableValue,
                isactive = @IsActive,
                updatedat = @UpdatedAt
            WHERE holidayid = @HolidayId AND organizationid = @OrganizationId";

        var result = await ExecuteAsync(sql, holiday);
        return result > 0;
    }

    public async Task<bool> DeleteCompanyHolidayAsync(Guid holidayId, Guid organizationId)
    {
        const string sql = @"
            UPDATE attendance.company_holidays
            SET isactive = false, updatedat = CURRENT_TIMESTAMP
            WHERE holidayid = @HolidayId AND organizationid = @OrganizationId";

        var result = await ExecuteAsync(sql, new { HolidayId = holidayId, OrganizationId = organizationId });
        return result > 0;
    }

    public async Task<bool> CompanyHolidayExistsAsync(Guid organizationId, string holidayName, DateTime holidayDate, Guid? excludeId = null)
    {
        var sql = @"
            SELECT COUNT(1) FROM attendance.company_holidays
            WHERE organizationid = @OrganizationId AND isactive = true
              AND (LOWER(holidayname) = LOWER(@HolidayName) OR holidaydate = @HolidayDate)";

        if (excludeId.HasValue)
            sql += " AND holidayid != @ExcludeId";

        var count = await ExecuteScalarAsync<int>(sql, new
        {
            OrganizationId = organizationId,
            HolidayName = holidayName,
            HolidayDate = holidayDate,
            ExcludeId = excludeId
        });

        return count > 0;
    }

    // ========================
    // Holiday Range Queries
    // ========================

    public async Task<IReadOnlyList<HolidayRangeDto>> GetHolidaysInRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT holidayid AS HolidayId, holidayname AS HolidayName, holidaydate AS HolidayDate,
                   holidaytype AS HolidayType, ishalfday AS IsHalfDay, description AS Description
            FROM attendance.company_holidays
            WHERE organizationid = @OrganizationId
              AND isactive = true
              AND holidaydate >= @StartDate
              AND holidaydate <= @EndDate
            ORDER BY holidaydate";

        return await QueryAsync<HolidayRangeDto>(sql, new
        {
            OrganizationId = organizationId,
            StartDate = startDate,
            EndDate = endDate
        });
    }

    /// <summary>
    /// Gets holidays applicable to a specific employee based on their department, location, or direct assignment.
    /// Filters: applicableto='all' OR department match OR location match OR employee assignment.
    /// </summary>
    public async Task<IReadOnlyList<HolidayRangeDto>> GetHolidaysInRangeForEmployeeAsync(
        Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation,
        DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT DISTINCT ch.holidayid AS HolidayId, ch.holidayname AS HolidayName, 
                   ch.holidaydate AS HolidayDate, ch.holidaytype AS HolidayType, 
                   ch.ishalfday AS IsHalfDay, ch.description AS Description
            FROM attendance.company_holidays ch
            LEFT JOIN attendance.holiday_employee_assignments hea 
                ON hea.holiday_id = ch.holidayid AND hea.employee_id = @EmployeeId
            WHERE ch.organizationid = @OrganizationId
              AND ch.isactive = true
              AND ch.holidaydate >= @StartDate
              AND ch.holidaydate <= @EndDate
              AND (
                  ch.applicableto = 'all'
                  OR (ch.applicableto = 'department' AND ch.applicablevalue = @DepartmentId)
                  OR (ch.applicableto = 'location' AND LOWER(ch.applicablevalue) = LOWER(@WorkLocation))
                  OR (ch.applicableto = 'employee' AND hea.employee_id IS NOT NULL)
              )
            ORDER BY ch.holidaydate";

        return await QueryAsync<HolidayRangeDto>(sql, new
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            DepartmentId = departmentId?.ToString(),
            WorkLocation = workLocation ?? "",
            StartDate = startDate,
            EndDate = endDate
        });
    }

    // ========================
    // Employee Holiday Assignments
    // ========================

    public async Task SaveEmployeeAssignmentsAsync(Guid holidayId, IEnumerable<Guid> employeeIds, Guid assignedBy)
    {
        // Delete existing assignments first
        await DeleteEmployeeAssignmentsAsync(holidayId);

        if (employeeIds == null || !employeeIds.Any()) return;

        const string sql = @"
            INSERT INTO attendance.holiday_employee_assignments (holiday_id, employee_id, assigned_by, assigned_at)
            VALUES (@HolidayId, @EmployeeId, @AssignedBy, NOW())
            ON CONFLICT (holiday_id, employee_id) DO NOTHING";

        var parameters = employeeIds.Select(employeeId => new 
        { 
            HolidayId = holidayId, 
            EmployeeId = employeeId, 
            AssignedBy = assignedBy 
        });
        
        await ExecuteAsync(sql, parameters);
    }

    public async Task<IReadOnlyList<Guid>> GetEmployeeAssignmentsAsync(Guid holidayId)
    {
        const string sql = @"
            SELECT employee_id FROM attendance.holiday_employee_assignments
            WHERE holiday_id = @HolidayId
            ORDER BY assigned_at";

        return await QueryAsync<Guid>(sql, new { HolidayId = holidayId });
    }

    public async Task DeleteEmployeeAssignmentsAsync(Guid holidayId)
    {
        const string sql = "DELETE FROM attendance.holiday_employee_assignments WHERE holiday_id = @HolidayId";
        await ExecuteAsync(sql, new { HolidayId = holidayId });
    }

    public async Task<bool> IsHolidayAsync(Guid organizationId, DateTime date)
    {
        const string sql = @"
            SELECT COUNT(1) FROM attendance.company_holidays
            WHERE organizationid = @OrganizationId
              AND isactive = true
              AND holidaydate = @Date";

        var count = await ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId, Date = date.Date });
        return count > 0;
    }

    public async Task<HolidaySummaryDto> GetHolidaySummaryAsync(Guid organizationId, int year)
    {
        const string sql = @"
            SELECT 
                @Year AS Year,
                COUNT(*) AS TotalHolidays,
                COUNT(*) FILTER (WHERE holidaytype = 'mandatory') AS MandatoryCount,
                COUNT(*) FILTER (WHERE holidaytype = 'restricted') AS RestrictedCount,
                COUNT(*) FILTER (WHERE holidaytype = 'optional') AS OptionalCount
            FROM attendance.company_holidays
            WHERE organizationid = @OrganizationId AND year = @Year AND isactive = true";

        var summary = await QuerySingleOrDefaultAsync<HolidaySummaryDto>(sql, new { OrganizationId = organizationId, Year = year });

        if (summary != null)
        {
            // Get upcoming holiday
            const string upcomingSql = @"
                SELECT holidayid AS HolidayId, organizationid AS OrganizationId, holidayname AS HolidayName,
                       holidaydate AS HolidayDate, holidaytype AS HolidayType, description AS Description,
                       ishalfday AS IsHalfDay, year AS Year, applicableto AS ApplicableTo,
                       applicablevalue AS ApplicableValue, isactive AS IsActive,
                       createdat AS CreatedAt, updatedat AS UpdatedAt
                FROM attendance.company_holidays
                WHERE organizationid = @OrganizationId AND isactive = true AND holidaydate >= CURRENT_DATE
                ORDER BY holidaydate
                LIMIT 1";

            summary.UpcomingHoliday = await QuerySingleOrDefaultAsync<CompanyHolidayDto>(upcomingSql, new { OrganizationId = organizationId });
        }

        return summary ?? new HolidaySummaryDto { Year = year };
    }

    // ========================
    // Employee Restricted Holidays
    // ========================

    public async Task<IReadOnlyList<EmployeeRestrictedHolidayDto>> GetEmployeeRestrictedHolidaysAsync(Guid employeeId, Guid organizationId, int year)
    {
        const string sql = @"
            SELECT erh.id AS Id, erh.employeeid AS EmployeeId, erh.companyholidayid AS CompanyHolidayId,
                   ch.holidayname AS HolidayName, ch.holidaydate AS HolidayDate,
                   ch.description AS Description, erh.status AS Status, erh.selectedat AS SelectedAt
            FROM attendance.employee_restricted_holidays erh
            JOIN attendance.company_holidays ch ON ch.holidayid = erh.companyholidayid
            WHERE erh.employeeid = @EmployeeId
              AND erh.organizationid = @OrganizationId
              AND ch.year = @Year
              AND erh.status = 'selected'
            ORDER BY ch.holidaydate";

        return await QueryAsync<EmployeeRestrictedHolidayDto>(sql, new
        {
            EmployeeId = employeeId,
            OrganizationId = organizationId,
            Year = year
        });
    }

    public async Task<IReadOnlyList<CompanyHolidayDto>> GetAvailableRestrictedHolidaysAsync(Guid organizationId, int year)
    {
        const string sql = @"
            SELECT holidayid AS HolidayId, organizationid AS OrganizationId, holidayname AS HolidayName,
                   holidaydate AS HolidayDate, holidaytype AS HolidayType, description AS Description,
                   ishalfday AS IsHalfDay, year AS Year, applicableto AS ApplicableTo,
                   applicablevalue AS ApplicableValue, isactive AS IsActive,
                   createdat AS CreatedAt, updatedat AS UpdatedAt
            FROM attendance.company_holidays
            WHERE organizationid = @OrganizationId
              AND year = @Year
              AND holidaytype = 'restricted'
              AND isactive = true
            ORDER BY holidaydate";

        return await QueryAsync<CompanyHolidayDto>(sql, new { OrganizationId = organizationId, Year = year });
    }

    public async Task<IReadOnlyList<CompanyHolidayDto>> GetAvailableRestrictedHolidaysForEmployeeAsync(
        Guid organizationId, Guid employeeId, Guid? departmentId, string? workLocation, int year)
    {
        const string sql = @"
            SELECT DISTINCT ch.holidayid AS HolidayId, ch.organizationid AS OrganizationId, ch.holidayname AS HolidayName,
                   ch.holidaydate AS HolidayDate, ch.holidaytype AS HolidayType, ch.description AS Description,
                   ch.ishalfday AS IsHalfDay, ch.year AS Year, ch.applicableto AS ApplicableTo,
                   ch.applicablevalue AS ApplicableValue, ch.isactive AS IsActive,
                   ch.createdat AS CreatedAt, ch.updatedat AS UpdatedAt
            FROM attendance.company_holidays ch
            LEFT JOIN attendance.holiday_employee_assignments hea
                ON hea.holiday_id = ch.holidayid AND hea.employee_id = @EmployeeId
            WHERE ch.organizationid = @OrganizationId
              AND ch.year = @Year
              AND ch.holidaytype = 'restricted'
              AND ch.isactive = true
              AND (
                    ch.applicableto = 'all'
                 OR (ch.applicableto = 'department' AND ch.applicablevalue = @DepartmentId)
                 OR (ch.applicableto = 'location' AND LOWER(ch.applicablevalue) = LOWER(@WorkLocation))
                 OR (ch.applicableto = 'employee' AND hea.employee_id IS NOT NULL)
              )
            ORDER BY ch.holidaydate";

        return await QueryAsync<CompanyHolidayDto>(sql, new
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            DepartmentId = departmentId?.ToString(),
            WorkLocation = workLocation ?? "",
            Year = year
        });
    }

    public async Task<Guid> SelectRestrictedHolidayAsync(EmployeeRestrictedHoliday selection)
    {
        try
        {
            const string sql = @"
            INSERT INTO attendance.employee_restricted_holidays
                (organizationid, employeeid, companyholidayid, status, selectedat, createdat)
            VALUES
                (@OrganizationId, @EmployeeId, @CompanyHolidayId, @Status, @SelectedAt, @CreatedAt)
            ON CONFLICT ON CONSTRAINT uq_employee_restricted
            DO UPDATE SET status = 'selected', selectedat = CURRENT_TIMESTAMP
            RETURNING id";

            return await ExecuteScalarAsync<Guid>(sql, selection);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> CancelRestrictedHolidayAsync(Guid id, Guid employeeId)
    {
        const string sql = @"
            UPDATE attendance.employee_restricted_holidays
            SET status = 'cancelled'
            WHERE id = @Id AND employeeid = @EmployeeId";

        var result = await ExecuteAsync(sql, new { Id = id, EmployeeId = employeeId });
        return result > 0;
    }

    public async Task<int> GetEmployeeRestrictedHolidayCountAsync(Guid employeeId, Guid organizationId, int year)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM attendance.employee_restricted_holidays erh
            JOIN attendance.company_holidays ch ON ch.holidayid = erh.companyholidayid
            WHERE erh.employeeid = @EmployeeId
              AND erh.organizationid = @OrganizationId
              AND ch.year = @Year
              AND erh.status = 'selected'";

        return await ExecuteScalarAsync<int>(sql, new
        {
            EmployeeId = employeeId,
            OrganizationId = organizationId,
            Year = year
        });
    }
}
