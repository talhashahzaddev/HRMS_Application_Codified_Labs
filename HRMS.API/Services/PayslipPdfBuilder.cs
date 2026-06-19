using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using HRMS.API.Services.PayslipLayouts;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HRMS.API.Services;

public class PayslipPdfBuilder : IPayslipPdfBuilder
{
    private readonly IPayslipLayoutRendererRegistry _rendererRegistry;
    private readonly IPayslipLayoutCatalogRepository _layoutCatalogRepository;

    public PayslipPdfBuilder(
        IPayslipLayoutRendererRegistry rendererRegistry,
        IPayslipLayoutCatalogRepository layoutCatalogRepository)
    {
        _rendererRegistry = rendererRegistry;
        _layoutCatalogRepository = layoutCatalogRepository;
    }

    public async Task<byte[]> BuildAsync(
        PayrollResultItemDto row,
        string periodLabel,
        string currencyCode = "$",
        PayslipTemplateDto? template = null)
    {
        template ??= PayslipTemplateDefaults.CreateDefaultTemplate(row.OrganizationId);
        var fieldConfig = template.FieldConfig ?? PayslipTemplateDefaults.CreateDefaultFieldConfig();
        var options = fieldConfig.Options ?? new PayslipTemplateOptionsDto();

        var headerBg = PayslipLayoutShared.ParseColor(template.PrimaryColor, PayslipLayoutShared.DefaultHeaderBg);
        var sectionBg = PayslipLayoutShared.ParseColor(template.SectionTitleColor, PayslipLayoutShared.DefaultSectionBg);
        var netBg = PayslipLayoutShared.ParseColor(template.AccentColor, PayslipLayoutShared.DefaultNetBg);

        var periodDisplay = string.IsNullOrWhiteSpace(row.PeriodName) ? periodLabel : row.PeriodName;
        var slip = PayslipLayoutShared.BuildSlipRows(row, currencyCode, fieldConfig, options);
        var fonts = PayslipLayoutFonts.Create(headerBg);

        var context = new PayslipLayoutContext
        {
            Template = template,
            Row = row,
            PeriodDisplay = periodDisplay,
            CurrencyCode = currencyCode,
            FieldConfig = fieldConfig,
            Options = options,
            Slip = slip,
            HeaderBg = headerBg,
            SectionBg = sectionBg,
            NetBg = netBg,
            Fonts = fonts
        };

        var layoutKey = string.IsNullOrWhiteSpace(template.LayoutKey)
            ? PayslipLayoutKeys.ClassicStacked
            : template.LayoutKey.Trim();
        var catalog = await _layoutCatalogRepository.GetByLayoutKeyAsync(layoutKey);
        var rendererKey = catalog?.RendererKey ?? layoutKey;
        var renderer = _rendererRegistry.Resolve(rendererKey);

        using var ms = new MemoryStream();
        var document = new Document(PageSize.A4, 36f, 36f, 36f, 36f);
        PdfWriter.GetInstance(document, ms);
        document.Open();
        renderer.Render(document, context);
        document.Close();
        return ms.ToArray();
    }
}
