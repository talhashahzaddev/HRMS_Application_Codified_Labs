using HRMS.API.Models.DTOs;
using iTextSharp.text;

namespace HRMS.API.Services.PayslipLayouts;

public class CompactCorporateLayoutRenderer : IPayslipLayoutRenderer
{
    public string RendererKey => PayslipLayoutKeys.CompactCorporate;

    public void Render(Document document, PayslipLayoutContext context)
    {
        PayslipLayoutShared.RenderHeader(document, context);
        PayslipLayoutShared.RenderSummaryStrip(document, context);
        PayslipLayoutShared.RenderEmployeeSection(document, context);
        PayslipLayoutShared.RenderSideBySideEarningsDeductions(document, context, compactFonts: true);
        PayslipLayoutShared.RenderFooter(document, context);
    }
}
