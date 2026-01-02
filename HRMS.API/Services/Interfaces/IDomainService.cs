using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

/// <summary>
/// Service interface for domain validation operations
/// </summary>
public interface IDomainService
{
    /// <summary>
    /// Validates if a domain exists and is active
    /// </summary>
    /// <param name="domain">The domain/subdomain to validate</param>
    /// <returns>ServiceResponse with validation result and organization data if valid</returns>
    Task<ServiceResponse<DomainValidationResponse>> ValidateDomainAsync(string domain);
}

