using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

/// <summary>
/// Repository implementation for domain validation operations
/// </summary>
public class DomainRepository : BaseRepository, IDomainRepository
{
    public DomainRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    /// <summary>
    /// Validates if a domain exists in the organizations table
    /// Checks if the domain matches exactly or if the stored domain starts with the subdomain
    /// Example: Stored domain "acmecorp.com" should match subdomain "acmecorp"
    /// </summary>
    public async Task<Organization?> ValidateDomainAsync(string domain)
    {
        // Normalize domain: remove protocol, www, and extract subdomain
        var normalizedDomain = NormalizeDomain(domain);

        // Query to check if domain exists in the database
        // We check:
        // 1. Exact match: domain = "acmecorp"
        // 2. Starts with subdomain: domain LIKE "acmecorp.%"
        // 3. Contains subdomain at start: domain LIKE "acmecorp%"
        var sql = @"
            SELECT * 
            FROM auth.organizations 
            WHERE (LOWER(domain) = LOWER(@normalizedDomain)
               OR LOWER(domain) LIKE LOWER(@startsWithPattern)
               OR LOWER(domain) LIKE LOWER(@containsPattern))
            AND status = 'active'
            LIMIT 1";

        // Pattern for domains that start with the subdomain (e.g., "acmecorp.com", "acmecorp.org")
        var startsWithPattern = $"{normalizedDomain}.%";
        // Pattern for domains that contain the subdomain (fallback)
        var containsPattern = $"{normalizedDomain}%";

        return await QuerySingleOrDefaultAsync<Organization>(sql, new 
        { 
            normalizedDomain, 
            startsWithPattern,
            containsPattern
        });
    }

    /// <summary>
    /// Gets organization by domain
    /// </summary>
    public async Task<Organization?> GetOrganizationByDomainAsync(string domain)
    {
        var normalizedDomain = NormalizeDomain(domain);
        
        var sql = @"
            SELECT * 
            FROM auth.organizations 
            WHERE (LOWER(domain) = LOWER(@normalizedDomain)
               OR LOWER(domain) LIKE LOWER(@pattern))
            AND status = 'active'
            LIMIT 1";

        var pattern = $"%{normalizedDomain}%";

        return await QuerySingleOrDefaultAsync<Organization>(sql, new 
        { 
            normalizedDomain,
            pattern
        });
    }

    /// <summary>
    /// Normalizes domain string by removing protocol, www, and extracting subdomain
    /// </summary>
    private string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return string.Empty;

        // Remove protocol
        domain = domain.Replace("https://", "").Replace("http://", "");

        // Remove www
        domain = domain.Replace("www.", "");

        // Remove trailing slash
        domain = domain.TrimEnd('/');

        // Extract subdomain (first part before first dot)
        var parts = domain.Split('.');
        if (parts.Length > 0)
        {
            // If it's a subdomain like "xyz.briskpeople.com", return "xyz"
            // If it's just "xyz", return "xyz"
            return parts[0].ToLowerInvariant();
        }

        return domain.ToLowerInvariant();
    }
}

