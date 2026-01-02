using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Controller for domain validation operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DomainController : ControllerBase
{
    private readonly IDomainService _domainService;
    private readonly ILogger<DomainController> _logger;

    public DomainController(
        IDomainService domainService,
        ILogger<DomainController> logger)
    {
        _domainService = domainService;
        _logger = logger;
    }

    /// <summary>
    /// Validates if a domain exists and is active
    /// </summary>
    /// <param name="domain">The domain/subdomain to validate (e.g., "xyz" from "xyz.briskpeople.com")</param>
    /// <returns>Validation result with organization information if valid</returns>
    [HttpGet("validate/{domain}")]
    [ProducesResponseType(typeof(HRMS.API.Models.DTOs.ServiceResponse<HRMS.API.Models.DTOs.DomainValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HRMS.API.Models.DTOs.ServiceResponse<HRMS.API.Models.DTOs.DomainValidationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HRMS.API.Models.DTOs.ServiceResponse<HRMS.API.Models.DTOs.DomainValidationResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateDomain(string domain)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return BadRequest(new HRMS.API.Models.DTOs.ServiceResponse<HRMS.API.Models.DTOs.DomainValidationResponse>
                {
                    Success = false,
                    Message = "Domain parameter is required"
                });
            }

            var response = await _domainService.ValidateDomainAsync(domain);

            if (!response.Success || response.Data == null || !response.Data.IsValid)
            {
                _logger.LogWarning("Domain validation failed for: {Domain}", domain);
                return NotFound(response);
            }

            _logger.LogInformation("Domain validation successful for: {Domain}", domain);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating domain: {Domain}", domain);
            return StatusCode(500, new HRMS.API.Models.DTOs.ServiceResponse<HRMS.API.Models.DTOs.DomainValidationResponse>
            {
                Success = false,
                Message = "An error occurred while validating the domain"
            });
        }
    }

    /// <summary>
    /// Health check endpoint for domain service
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "DomainService" });
    }
}

