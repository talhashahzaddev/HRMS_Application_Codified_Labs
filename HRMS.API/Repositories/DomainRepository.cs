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
    /// Uses EXACT matching only (case-insensitive)
    /// Domains are normalized (lowercase, no special characters, no spaces) before comparison
    /// </summary>
    public async Task<Organization?> ValidateDomainAsync(string domain)
    {
        // Normalize domain: lowercase, remove special characters, spaces, and extract subdomain
        var normalizedDomain = NormalizeDomain(domain);

        if (string.IsNullOrWhiteSpace(normalizedDomain))
        {
            return null;
        }

        // Query to check if domain exists in the database using EXACT match only
        // Extract subdomain from stored domain (first part before dot) and compare exactly
        // Since DB domains are already normalized (lowercase, no special chars), we just extract subdomain
        // Handle both cases: domain with dots (e.g., "myorg.com") and without (e.g., "myorg")
        var sql = @"
            SELECT * 
            FROM auth.organizations 
            WHERE (
                LOWER(TRIM(SPLIT_PART(LOWER(TRIM(COALESCE(domain, ''))), '.', 1))) = LOWER(@normalizedDomain)
                OR LOWER(TRIM(COALESCE(domain, ''))) = LOWER(@normalizedDomain)
            )
            AND status = 'active'
            AND domain IS NOT NULL
            AND TRIM(domain) != ''
            LIMIT 1";

        return await QuerySingleOrDefaultAsync<Organization>(sql, new 
        { 
            normalizedDomain
        });
    }

    /// <summary>
    /// Gets organization by domain using exact match
    /// </summary>
    public async Task<Organization?> GetOrganizationByDomainAsync(string domain)
    {
        var normalizedDomain = NormalizeDomain(domain);
        
        if (string.IsNullOrWhiteSpace(normalizedDomain))
        {
            return null;
        }
        
        var sql = @"
            SELECT * 
            FROM auth.organizations 
            WHERE LOWER(TRIM(domain)) = LOWER(@normalizedDomain)
            AND status = 'active'
            LIMIT 1";

        return await QuerySingleOrDefaultAsync<Organization>(sql, new 
        { 
            normalizedDomain
        });
    }

    /// <summary>
    /// Normalizes domain string by:
    /// 1. Removing protocol, www, and extracting subdomain
    /// 2. Converting to lowercase
    /// 3. Removing special characters (keeping only alphanumeric and hyphens)
    /// 4. Removing spaces
    /// 5. Removing leading/trailing hyphens
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
            // If it's a subdomain like "xyz.briskpeople.com", use "xyz"
            // If it's just "xyz", use "xyz"
            domain = parts[0];
        }

        // Convert to lowercase
        domain = domain.ToLowerInvariant();

        // Remove all special characters except hyphens and alphanumeric
        domain = System.Text.RegularExpressions.Regex.Replace(domain, @"[^a-z0-9-]", "");

        // Remove spaces (though regex above should have handled this)
        domain = domain.Replace(" ", "");

        // Remove consecutive hyphens
        while (domain.Contains("--"))
        {
            domain = domain.Replace("--", "-");
        }

        // Remove leading and trailing hyphens
        domain = domain.Trim('-');

        return domain;
    }
}

