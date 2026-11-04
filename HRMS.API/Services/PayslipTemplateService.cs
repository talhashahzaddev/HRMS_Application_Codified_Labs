using System.Text.RegularExpressions;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PayslipTemplateService : IPayslipTemplateService
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly IPayslipTemplateRepository _repository;
    private readonly IPayslipLayoutCatalogRepository _layoutCatalogRepository;
    private readonly IPayslipPdfBuilder _pdfBuilder;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<PayslipTemplateService> _logger;

    public PayslipTemplateService(
        IPayslipTemplateRepository repository,
        IPayslipLayoutCatalogRepository layoutCatalogRepository,
        IPayslipPdfBuilder pdfBuilder,
        ISettingsRepository settingsRepository,
        ILogger<PayslipTemplateService> logger)
    {
        _repository = repository;
        _layoutCatalogRepository = layoutCatalogRepository;
        _pdfBuilder = pdfBuilder;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<ServiceResponse<PayslipTemplateDto>> GetActiveTemplateAsync(Guid organizationId)
    {
        try
        {
            var template = await _repository.GetActiveTemplateAsync(organizationId);
            if (template != null)
            {
                template.LogoUrl = ResolveLogoUrl(template.LogoUrl, await _repository.GetOrganizationLogoUrlAsync(organizationId));
                return new ServiceResponse<PayslipTemplateDto> { Success = true, Data = template };
            }

            var orgLogo = await _repository.GetOrganizationLogoUrlAsync(organizationId);
            var defaults = PayslipTemplateDefaults.CreateDefaultTemplate(organizationId, orgLogo);
            defaults.LogoUrl = ResolveLogoUrl(defaults.LogoUrl, orgLogo);
            return new ServiceResponse<PayslipTemplateDto> { Success = true, Data = defaults };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payslip template for organization {OrganizationId}", organizationId);
            return new ServiceResponse<PayslipTemplateDto>
            {
                Success = false,
                Message = "An error occurred while loading the payslip template."
            };
        }
    }

    public async Task<ServiceResponse<PayslipTemplateDto>> UpsertActiveTemplateAsync(
        Guid organizationId,
        UpsertPayslipTemplateRequest request,
        Guid? updatedBy)
    {
        var validation = await ValidateRequestAsync(request);
        if (!validation.Success)
        {
            return validation;
        }

        try
        {
            var templateId = await _repository.UpsertActiveTemplateAsync(organizationId, request, updatedBy);
            if (!templateId.HasValue)
            {
                return new ServiceResponse<PayslipTemplateDto>
                {
                    Success = false,
                    Message = "Failed to save payslip template."
                };
            }

            return await GetActiveTemplateAsync(organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payslip template for organization {OrganizationId}", organizationId);
            return new ServiceResponse<PayslipTemplateDto>
            {
                Success = false,
                Message = "An error occurred while saving the payslip template."
            };
        }
    }

    public async Task<ServiceResponse<IReadOnlyList<PayslipLayoutCatalogDto>>> GetActiveLayoutsAsync()
    {
        try
        {
            var layouts = await _layoutCatalogRepository.GetActiveLayoutsAsync();
            return new ServiceResponse<IReadOnlyList<PayslipLayoutCatalogDto>>
            {
                Success = true,
                Data = layouts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payslip layout catalog");
            return new ServiceResponse<IReadOnlyList<PayslipLayoutCatalogDto>>
            {
                Success = false,
                Message = "An error occurred while loading payslip layout options."
            };
        }
    }

    public async Task<byte[]> BuildPreviewPdfAsync(
        Guid organizationId,
        UpsertPayslipTemplateRequest? draftTemplate,
        string currencyLabel)
    {
        PayslipTemplateDto template;
        if (draftTemplate != null)
        {
            var validation = await ValidateRequestAsync(draftTemplate);
            if (!validation.Success)
            {
                throw new InvalidOperationException(validation.Message ?? "Invalid template configuration.");
            }

            template = MapDraftToDto(organizationId, draftTemplate);
        }
        else
        {
            var response = await GetActiveTemplateAsync(organizationId);
            template = response.Data ?? PayslipTemplateDefaults.CreateDefaultTemplate(organizationId);
        }

        var sampleRow = PayslipTemplateDefaults.CreateSamplePayrollRow(organizationId);
        return await _pdfBuilder.BuildAsync(sampleRow, sampleRow.PeriodName, currencyLabel, template);
    }

    private async Task<ServiceResponse<PayslipTemplateDto>> ValidateRequestAsync(UpsertPayslipTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.HeaderTitle))
        {
            return new ServiceResponse<PayslipTemplateDto>
            {
                Success = false,
                Message = "Header title is required."
            };
        }

        if (request.HeaderTitle.Length > 150)
        {
            return new ServiceResponse<PayslipTemplateDto>
            {
                Success = false,
                Message = "Header title must be 150 characters or fewer."
            };
        }

        foreach (var color in new[] { request.PrimaryColor, request.AccentColor, request.SectionTitleColor })
        {
            if (!string.IsNullOrWhiteSpace(color) && !HexColorRegex.IsMatch(color))
            {
                return new ServiceResponse<PayslipTemplateDto>
                {
                    Success = false,
                    Message = "Colors must use #RRGGBB format."
                };
            }
        }

        var fieldConfig = request.FieldConfig ?? PayslipTemplateDefaults.CreateDefaultFieldConfig();
        var earningsVisible = fieldConfig.Sections.TryGetValue("earnings", out var earnings) && earnings.Visible;
        var netVisible = fieldConfig.Sections.TryGetValue("netPay", out var netPay) && netPay.Visible;
        if (!earningsVisible && !netVisible)
        {
            return new ServiceResponse<PayslipTemplateDto>
            {
                Success = false,
                Message = "At least one of Earnings or Net Pay sections must be visible."
            };
        }

        var layoutKey = string.IsNullOrWhiteSpace(request.LayoutKey)
            ? PayslipLayoutKeys.ClassicStacked
            : request.LayoutKey.Trim();
        if (!await _layoutCatalogRepository.IsActiveLayoutKeyAsync(layoutKey))
        {
            return new ServiceResponse<PayslipTemplateDto>
            {
                Success = false,
                Message = "Selected payslip layout is not available."
            };
        }

        request.LayoutKey = layoutKey;
        return new ServiceResponse<PayslipTemplateDto> { Success = true };
    }

    private static PayslipTemplateDto MapDraftToDto(Guid organizationId, UpsertPayslipTemplateRequest request) => new()
    {
        OrganizationId = organizationId,
        Name = request.Name,
        LogoUrl = request.LogoUrl,
        HeaderTitle = request.HeaderTitle,
        HeaderSubtitle = request.HeaderSubtitle,
        CompanyAddress = request.CompanyAddress,
        FooterText = request.FooterText,
        PrimaryColor = request.PrimaryColor,
        AccentColor = request.AccentColor,
        SectionTitleColor = request.SectionTitleColor,
        ShowGeneratedAt = request.ShowGeneratedAt,
        ShowPayrollCalculatedAt = request.ShowPayrollCalculatedAt,
        LayoutKey = string.IsNullOrWhiteSpace(request.LayoutKey) ? PayslipLayoutKeys.ClassicStacked : request.LayoutKey.Trim(),
        LayoutConfig = request.LayoutConfig,
        FieldConfig = request.FieldConfig ?? PayslipTemplateDefaults.CreateDefaultFieldConfig(),
        IsCustom = true
    };

    private static string? ResolveLogoUrl(string? templateLogo, string? organizationLogo)
    {
        if (!string.IsNullOrWhiteSpace(templateLogo))
        {
            return templateLogo.Trim();
        }

        return string.IsNullOrWhiteSpace(organizationLogo) ? null : organizationLogo.Trim();
    }
}
