using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

/// <summary>
/// Repository interface for domain validation operations
/// </summary>
public interface IDomainRepository
{
    /// <summary>
    /// Validates if a domain exists in the organizations table
    /// </summary>
    /// <param name="domain">The domain/subdomain to validate (e.g., "xyz" from "xyz.briskpeople.com")</param>
    /// <returns>Organization if domain exists, null otherwise</returns>
    Task<Organization?> ValidateDomainAsync(string domain);

    /// <summary>
    /// Gets organization by domain
    /// </summary>
    /// <param name="domain">The domain/subdomain</param>
    /// <returns>Organization if found, null otherwise</returns>
    Task<Organization?> GetOrganizationByDomainAsync(string domain);
}


