using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

/// <summary>
/// Service implementation for domain validation operations
/// </summary>
public class DomainService : IDomainService
{
    private readonly IDomainRepository _domainRepository;
    private readonly ILogger<DomainService> _logger;

    public DomainService(
        IDomainRepository domainRepository,
        ILogger<DomainService> logger)
    {
        _domainRepository = domainRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validates if a domain exists and is active
    /// </summary>
    public async Task<ServiceResponse<DomainValidationResponse>> ValidateDomainAsync(string domain)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return new ServiceResponse<DomainValidationResponse>
                {
                    Success = false,
                    Message = "Domain is required",
                    Data = new DomainValidationResponse
                    {
                        IsValid = false,
                        Message = "Domain cannot be empty"
                    }
                };
            }

            // Normalize domain (extract subdomain)
            var normalizedDomain = NormalizeDomain(domain);
            
            _logger.LogInformation("Validating domain: {Domain} (normalized: {NormalizedDomain})", domain, normalizedDomain);

            // Check if domain exists in database
            var organization = await _domainRepository.ValidateDomainAsync(normalizedDomain);

            if (organization == null)
            {
                _logger.LogWarning("Domain validation failed: {Domain} not found in database", normalizedDomain);
                
                return new ServiceResponse<DomainValidationResponse>
                {
                    Success = false,
                    Message = "Domain not found or inactive",
                    Data = new DomainValidationResponse
                    {
                        IsValid = false,
                        Domain = normalizedDomain,
                        Message = "The requested domain is not available or has been deactivated"
                    }
                };
            }

            // Check if organization is active
            if (organization.Status?.ToLowerInvariant() != "active")
            {
                _logger.LogWarning("Domain validation failed: {Domain} organization is not active", normalizedDomain);
                
                return new ServiceResponse<DomainValidationResponse>
                {
                    Success = false,
                    Message = "Organization is not active",
                    Data = new DomainValidationResponse
                    {
                        IsValid = false,
                        Domain = normalizedDomain,
                        OrganizationId = organization.OrganizationId,
                        OrganizationName = organization.Name,
                        Message = "The organization associated with this domain is not active"
                    }
                };
            }

            _logger.LogInformation("Domain validation successful: {Domain} -> Organization: {OrganizationName} ({OrganizationId})", 
                normalizedDomain, organization.Name, organization.OrganizationId);

            return new ServiceResponse<DomainValidationResponse>
            {
                Success = true,
                Message = "Domain is valid",
                Data = new DomainValidationResponse
                {
                    IsValid = true,
                    Domain = normalizedDomain,
                    OrganizationId = organization.OrganizationId,
                    OrganizationName = organization.Name,
                    Message = "Domain validated successfully"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating domain: {Domain}", domain);
            
            return new ServiceResponse<DomainValidationResponse>
            {
                Success = false,
                Message = "An error occurred while validating the domain",
                Data = new DomainValidationResponse
                {
                    IsValid = false,
                    Message = "Internal server error during domain validation"
                }
            };
        }
    }

    /// <summary>
    /// Normalizes domain string by extracting subdomain
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
        // Example: "xyz.briskpeople.com" -> "xyz"
        var parts = domain.Split('.');
        if (parts.Length > 0)
        {
            return parts[0].ToLowerInvariant();
        }

        return domain.ToLowerInvariant();
    }
}

