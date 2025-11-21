using HRMS.API.Configuration;
using HRMS.API.Repositories.Interfaces;
using Dapper;
using Npgsql;

namespace HRMS.API.Repositories;

public class SettingsRepository : BaseRepository, ISettingsRepository
{
    public SettingsRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

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
}









