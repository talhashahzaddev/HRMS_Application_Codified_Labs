using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using System.Globalization;

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
            var LocalizationSetting = await _settingsRepository.GetOrganizationSettingsAsync(organizationId);


            var settings = new OrganizationSettingsDto
            {
                OrganizationId = organizationId,
                Currency = currency,
                OrganizationName = organization.Name ?? string.Empty,
                TimeZone=LocalizationSetting.TimeZone ?? string.Empty,
                Culture=LocalizationSetting.Culture ?? string.Empty
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

    public async Task<ServiceResponse<bool>> UpdateLocalizationAsync(
    Guid organizationId,
    string currency,
    string timeZone,
    string culture)
    {
        try
        {
            _logger.LogInformation(
                "Updating localization for organization: {OrganizationId}",
                organizationId);

            // -----------------------------
            // 1?? Validate Currency (ISO 4217)
            // -----------------------------
            if (string.IsNullOrWhiteSpace(currency))
                return Fail("Currency code is required");

            if (currency.Length != 3 || !currency.All(char.IsLetter))
                return Fail("Currency must be a valid ISO 4217 code (e.g., USD, PKR)");

            currency = currency.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(culture))
                return Fail("Culture is required");

            try
            {
                _ = new CultureInfo(culture);
            }
            catch
            {
                return Fail("Invalid culture code (e.g., en-US, en-PK)");
            }

            // -----------------------------
            // 4?? Verify Organization Exists
            // -----------------------------
            var organization = await _organizationRepository
                .GetOrganizationByIdAsync(organizationId);

            if (organization == null)
                return Fail("Organization not found");

            // -----------------------------
            // 5?? Update Database
            // -----------------------------
            var result = await _settingsRepository
                .UpdateLocalizationAsync(organizationId, currency, timeZone, culture);

            if (!result)
                return Fail("Failed to update localization settings");

            _logger.LogInformation(
                "Successfully updated localization for organization: {OrganizationId}",
                organizationId);

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Localization updated successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating localization for organization: {OrganizationId}",
                organizationId);

            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating localization"
            };
        }
    }

    private ServiceResponse<bool> Fail(string message)
    {
        return new ServiceResponse<bool>
        {
            Success = false,
            Message = message
        };
    }

    public async Task<ServiceResponse<bool>> UpdateCareerPageAsync(Guid organizationId, UpdateCareerPageRequest request)
    {
        try
        {
            _logger.LogInformation("Updating career page settings for organization: {OrganizationId}", organizationId);

            var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Organization not found"
                };
            }

            var result = await _settingsRepository.UpdateCareerPageAsync(
                organizationId,
                request.CareerBgImageUrl,
                request.CareerHeaderText,
                request.CareerDescription,
                request.LogoUrl);

            if (!result)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update career page settings"
                };
            }

            _logger.LogInformation("Successfully updated career page settings for organization: {OrganizationId}", organizationId);

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Career page settings updated successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating career page settings for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating career page settings"
            };
        }
    }

    public async Task<ServiceResponse<CareerPageDto>> GetCareerPageAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation("Getting career page details for organization: {OrganizationId}", organizationId);

            var data = await _settingsRepository.GetCareerPageAsync(organizationId);

            if (data == null)
            {
                return new ServiceResponse<CareerPageDto>
                {
                    Success = false,
                    Message = "Organization not found"
                };
            }

            return new ServiceResponse<CareerPageDto>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting career page details for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<CareerPageDto>
            {
                Success = false,
                Message = "An error occurred while retrieving career page details"
            };
        }
    }
}










