using HRMS.API.Models.DTOs;
using iTextSharp.text;

namespace HRMS.API.Services.PayslipLayouts;

public class ClassicStackedLayoutRenderer : IPayslipLayoutRenderer
{
    public string RendererKey => PayslipLayoutKeys.ClassicStacked;

    public void Render(Document document, PayslipLayoutContext context)
    {
        PayslipLayoutShared.RenderHeader(document, context);
        PayslipLayoutShared.RenderEmployeeSection(document, context);
        PayslipLayoutShared.RenderEarningsSection(document, context);
        PayslipLayoutShared.RenderDeductionsSection(document, context);
        PayslipLayoutShared.RenderNetPaySection(document, context);
        PayslipLayoutShared.RenderFooter(document, context);
    }
}
