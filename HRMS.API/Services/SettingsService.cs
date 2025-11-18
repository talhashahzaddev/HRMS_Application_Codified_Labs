using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        ISettingsRepository settingsRepository,
        IOrganizationRepository organizationRepository,
        ILogger<SettingsService> logger)
    {
        _settingsRepository = settingsRepository;
        _organizationRepository = organizationRepository;
        _logger = logger;
    }

    public async Task<ServiceResponse<OrganizationSettingsDto>> GetOrganizationSettingsAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation("Getting organization settings for organization: {OrganizationId}", organizationId);

            var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
            {
                return new ServiceResponse<OrganizationSettingsDto>
                {
                    Success = false,
                    Message = "Organization not found"
                };
            }

            var currency = await _settingsRepository.GetCurrencyAsync(organizationId);

            var settings = new OrganizationSettingsDto
            {
                OrganizationId = organizationId,
                Currency = currency,
                OrganizationName = organization.Name ?? string.Empty
            };

            return new ServiceResponse<OrganizationSettingsDto>
            {
                Success = true,
                Data = settings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization settings for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<OrganizationSettingsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving organization settings"
            };
        }
    }

    public async Task<ServiceResponse<bool>> UpdateCurrencyAsync(Guid organizationId, string currency)
    {
        try
        {
            _logger.LogInformation("Updating currency for organization: {OrganizationId} to {Currency}", organizationId, currency);

            // Validate currency code (ISO 4217 format - 3 letters)
            if (string.IsNullOrWhiteSpace(currency))
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Currency code is required"
                };
            }

            if (currency.Length != 3 || !currency.All(char.IsLetter))
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Currency must be a valid ISO 4217 code (3 letters, e.g., USD, PKR, INR)"
                };
            }

            currency = currency.ToUpperInvariant();

            // Verify organization exists
            var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Organization not found"
                };
            }

            var result = await _settingsRepository.UpdateCurrencyAsync(organizationId, currency);

            if (!result)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update currency"
                };
            }

            _logger.LogInformation("Successfully updated currency for organization: {OrganizationId} to {Currency}", organizationId, currency);

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Currency updated successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating currency"
            };
        }
    }
}





