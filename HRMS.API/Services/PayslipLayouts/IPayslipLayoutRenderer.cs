using iTextSharp.text;

namespace HRMS.API.Services.PayslipLayouts;

public interface IPayslipLayoutRenderer
{
    string RendererKey { get; }
    void Render(Document document, PayslipLayoutContext context);
}
