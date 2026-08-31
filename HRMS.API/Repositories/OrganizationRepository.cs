using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using System.Data;

namespace HRMS.API.Repositories
{
    public class OrganizationRepository : BaseRepository, IOrganizationRepository
    {
        public OrganizationRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<Organization> CreateOrganizationAsync(Organization organization)
        {
            var organizationId = await ExecuteScalarAsync<Guid>(
                "SELECT auth.fn_create_organization(@name, @domain, @subscriptionType,@currency)",
                new { name = organization.Name, domain = organization.Domain, subscriptionType = organization.SubscriptionType, currency=organization.Currency});

            organization.OrganizationId = organizationId;

            // Update additional fields not handled by fn_create_organization
            if (!string.IsNullOrEmpty(organization.AdminEmail) || !string.IsNullOrEmpty(organization.CompanySize) ||
                !string.IsNullOrEmpty(organization.Industry) || !string.IsNullOrEmpty(organization.Country))
            {
                var updateSql = @"UPDATE auth.organizations 
                    SET admin_email = @AdminEmail, company_size = @CompanySize, 
                        industry = @Industry, country = @Country
                    WHERE organizationid = @OrganizationId";
                await ExecuteAsync(updateSql, new 
                { 
                    AdminEmail = organization.AdminEmail, 
                    CompanySize = organization.CompanySize, 
                    Industry = organization.Industry, 
                    Country = organization.Country, 
                    OrganizationId = organizationId 
                });
            }

            return organization;
        }

        public async Task<Organization?> GetOrganizationByIdAsync(Guid organizationId)
        {
            var sql = "SELECT * FROM auth.organizations WHERE organizationid = @organizationId";
            return await QuerySingleOrDefaultAsync<Organization>(sql, new { organizationId });
        }

        public async Task<Organization?> GetOrganizationByNameAsync(string name)
        {
            var sql = "SELECT * FROM auth.organizations WHERE name = @name";
            return await QuerySingleOrDefaultAsync<Organization>(sql, new { name });
        }

        public async Task<bool> UpdateOrganizationAsync(Organization organization)
        {
            var sql = @"UPDATE auth.organizations 
                        SET name = @name, domain = @domain, subscriptiontype = @subscriptionType, updated_at = CURRENT_TIMESTAMP 
                        WHERE organization_id = @organizationId";
            var affectedRows = await ExecuteAsync(sql, new { name = organization.Name, domain = organization.Domain, subscriptionType = organization.SubscriptionType, organizationId = organization.OrganizationId });
            return affectedRows > 0;
        }

        public async Task<bool> DeleteOrganizationAsync(Guid organizationId)
        {
            var sql = "DELETE FROM auth.organizations WHERE organizationid = @organizationId";
            var affectedRows = await ExecuteAsync(sql, new { organizationId });
            return affectedRows > 0;
        }

        public async Task<Organization?> GetOrganizationByDomainAsync(string domain)
        {
            var sql = "SELECT * FROM auth.organizations WHERE domain = @domain";
            return await QuerySingleOrDefaultAsync<Organization>(sql, new { domain });
        }

        public async Task<IReadOnlyList<Organization>> GetAllOrganizationsAsync()
        {
            var sql = "SELECT * FROM auth.organizations";
            return (await QueryAsync<Organization>(sql)).ToArray();
        }
    }
}