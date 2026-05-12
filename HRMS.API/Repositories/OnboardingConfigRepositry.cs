using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories
{
    // OnboardingConfigRepository.cs
    public class OnboardingConfigRepository : BaseRepository, IOnboardingConfigRepository
    {
        public OnboardingConfigRepository(IDatabaseConnectionFactory connectionFactory)
            : base(connectionFactory) { }

        public async Task<List<FieldConfigRecord>> GetByOrganizationAsync(Guid organizationId)
        {
            const string sql = @"
            SELECT organizationid, section, fieldkey, enabled, required
            FROM   onboarding.field_configurations
            WHERE  organizationid = @OrganizationId";

            var results = await QueryAsync<FieldConfigRecord>(sql, new { OrganizationId = organizationId });
            return results.ToList();
        }

        public async Task<bool> BulkUpsertAsync(Guid organizationId, List<FieldConfigRecord> records)
        {
            const string sql = @"
            INSERT INTO onboarding.field_configurations
                (organizationid, section, fieldkey, enabled, required, updatedat)
            VALUES
                (@OrganizationId, @Section, @FieldKey, @Enabled, @Required, CURRENT_TIMESTAMP)
            ON CONFLICT (organizationid, section, fieldkey)
            DO UPDATE SET
                enabled   = EXCLUDED.enabled,
                required  = EXCLUDED.required,
                updatedat = CURRENT_TIMESTAMP";

            foreach (var record in records)
            {
                await ExecuteAsync(sql, new
                {
                    OrganizationId = organizationId,
                    record.Section,
                    record.FieldKey,
                    record.Enabled,
                    record.Required
                });
            }
            return true;
        }

        public async Task<bool> UpsertConfigAsync(Guid organizationId, string section,
            string fieldKey, bool enabled, bool required)
        {
            return await BulkUpsertAsync(organizationId, new List<FieldConfigRecord>
        {
            new() { OrganizationId = organizationId, Section = section,
                    FieldKey = fieldKey, Enabled = enabled, Required = required }
        });
        }
    }
}
