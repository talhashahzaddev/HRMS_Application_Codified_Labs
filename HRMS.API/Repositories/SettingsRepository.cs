using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using Dapper;
using Npgsql;

namespace HRMS.API.Repositories;

public class SettingsRepository : BaseRepository, ISettingsRepository
{
    public SettingsRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<int> GetManualEntryTimeLimitAsync(Guid organizationId)
    {
        var sql = "SELECT COALESCE((settings->>'manual_entry_time_limit_days')::int, 30) FROM auth.organizations WHERE organizationid = @organizationId";
        return await ExecuteScalarAsync<int>(sql, new { organizationId });
    }

    public async Task<string?> GetCurrencyAsync(Guid organizationId)
    {
        var sql = "SELECT currency FROM auth.organizations WHERE organizationid = @organizationId";
        return await QuerySingleOrDefaultAsync<string?>(sql, new { organizationId });
    }

    public async Task<bool> UpdateCurrencyAsync(Guid organizationId, string currency)
    {
        var sql = @"UPDATE auth.organizations 
                    SET currency = @currency, updatedat = CURRENT_TIMESTAMP 
                    WHERE organizationid = @organizationId";
        var affectedRows = await ExecuteAsync(sql, new { currency, organizationId });
        return affectedRows > 0;
    }

    public async Task<bool> UpdateCareerPageAsync(Guid organizationId, string? careerBgImageUrl, string? careerHeaderText, string? careerDescription, string? logoUrl)
    {
        var sql = @"UPDATE auth.organizations
                    SET careerbgimageurl   = @careerBgImageUrl,
                        careerheadertext   = @careerHeaderText,
                        careerdescription  = @careerDescription,
                        logourl            = @logoUrl,
                        updatedat          = CURRENT_TIMESTAMP
                    WHERE organizationid = @organizationId";
        var affectedRows = await ExecuteAsync(sql, new { careerBgImageUrl, careerHeaderText, careerDescription, logoUrl, organizationId });
        return affectedRows > 0;
    }

    public async Task<CareerPageDto?> GetCareerPageAsync(Guid organizationId)
    {
        var sql = @"SELECT organizationid  AS OrganizationId,
                           name            AS Name,
                           logourl         AS LogoUrl,
                           careerbgimageurl  AS CareerBgImageUrl,
                           careerheadertext  AS CareerHeaderText,
                           careerdescription AS CareerDescription
                    FROM auth.organizations
                    WHERE organizationid = @organizationId";
        return await QuerySingleOrDefaultAsync<CareerPageDto?>(sql, new { organizationId });
    }
}










