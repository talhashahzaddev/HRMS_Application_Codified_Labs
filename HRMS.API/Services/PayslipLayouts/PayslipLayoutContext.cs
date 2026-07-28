using HRMS.API.Models.DTOs;
using iTextSharp.text;

namespace HRMS.API.Services.PayslipLayouts;

public sealed class PayslipLayoutContext
{
    public required PayslipTemplateDto Template { get; init; }
    public required PayrollResultItemDto Row { get; init; }
    public required string PeriodDisplay { get; init; }
    public required string CurrencyCode { get; init; }
    public required PayslipTemplateFieldConfigDto FieldConfig { get; init; }
    public required PayslipTemplateOptionsDto Options { get; init; }
    public required PayslipSlipRows Slip { get; init; }
    public required BaseColor HeaderBg { get; init; }
    public required BaseColor SectionBg { get; init; }
    public required BaseColor NetBg { get; init; }
    public required PayslipLayoutFonts Fonts { get; init; }
}
