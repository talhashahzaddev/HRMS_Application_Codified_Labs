using HRMS.API.Models.DTOs;
using iTextSharp.text;

namespace HRMS.API.Services.PayslipLayouts;

public class SplitColumnsLayoutRenderer : IPayslipLayoutRenderer
{
    public string RendererKey => PayslipLayoutKeys.SplitColumns;

    public void Render(Document document, PayslipLayoutContext context)
    {
        PayslipLayoutShared.RenderHeader(document, context);
        PayslipLayoutShared.RenderEmployeeSection(document, context);
        PayslipLayoutShared.RenderSideBySideEarningsDeductions(document, context);
        PayslipLayoutShared.RenderNetPaySection(document, context);
        PayslipLayoutShared.RenderFooter(document, context);
    }
}
