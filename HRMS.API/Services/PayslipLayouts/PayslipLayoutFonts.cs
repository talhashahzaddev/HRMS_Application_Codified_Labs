using iTextSharp.text;

namespace HRMS.API.Services.PayslipLayouts;

public sealed class PayslipLayoutFonts
{
    public Font HeaderTitle { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20f, BaseColor.WHITE);
    public Font HeaderSub { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 10f, BaseColor.WHITE);
    public Font Section { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, PayslipLayoutShared.TextDark);
    public Font Label { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 10f, PayslipLayoutShared.TextDark);
    public Font Value { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 10f, PayslipLayoutShared.TextDark);
    public Font ValueBold { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, PayslipLayoutShared.TextDark);
    public Font Net { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13f, PayslipLayoutShared.DefaultHeaderBg);
    public Font NetLabel { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 9f, PayslipLayoutShared.TextMuted);
    public Font Footer { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 8f, PayslipLayoutShared.TextMuted);
    public Font Address { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 9f, BaseColor.WHITE);
    public Font CompactLabel { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 9f, PayslipLayoutShared.TextDark);
    public Font CompactValue { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 9f, PayslipLayoutShared.TextDark);
    public Font SummaryValue { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, PayslipLayoutShared.TextDark);
    public Font SummaryLabel { get; init; } = FontFactory.GetFont(FontFactory.HELVETICA, 8f, PayslipLayoutShared.TextMuted);

    public static PayslipLayoutFonts Create(BaseColor headerBg)
    {
        return new PayslipLayoutFonts
        {
            Net = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13f, headerBg)
        };
    }
}
