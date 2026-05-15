using System.Globalization;
using System.Text;
using System.Text.Json;
using HRMS.API.Models.DTOs;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HRMS.API.Services;

/// <summary>Builds a single-page payslip PDF from <see cref="PayrollResultItemDto"/>; omits empty / zero / null fields where noted.</summary>
public static class PayslipPdfBuilder
{
    private static readonly BaseColor HeaderBg = new(30, 58, 95);
    private static readonly BaseColor SectionBg = new(226, 232, 240);
    private static readonly BaseColor NetBg = new(219, 234, 254);
    private static readonly BaseColor LabelBg = new(248, 250, 252);
    private static readonly BaseColor BorderGray = new(203, 213, 225);
    private static readonly BaseColor TextDark = new(51, 65, 85);
    private static readonly BaseColor TextMuted = new(100, 116, 139);

    public static byte[] Build(PayrollResultItemDto row, string periodLabel, string currencyCode = "$")
    {
        var periodDisplay = string.IsNullOrWhiteSpace(row.PeriodName) ? periodLabel : row.PeriodName;
        var slip = BuildSlipRows(row, currencyCode);

        using var ms = new MemoryStream();
        var document = new Document(PageSize.A4, 36f, 36f, 36f, 36f);
        PdfWriter.GetInstance(document, ms);
        document.Open();

        var fontHeaderTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22f, BaseColor.WHITE);
        var fontHeaderSub = FontFactory.GetFont(FontFactory.HELVETICA, 10f, BaseColor.WHITE);
        var fontSection = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, TextDark);
        var fontLabel = FontFactory.GetFont(FontFactory.HELVETICA, 10f, TextDark);
        var fontValue = FontFactory.GetFont(FontFactory.HELVETICA, 10f, TextDark);
        var fontValueBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, TextDark);
        var fontNet = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13f, HeaderBg);
        var fontFooter = FontFactory.GetFont(FontFactory.HELVETICA, 8f, TextMuted);

        // --- Header strip ---
        var headerTable = new PdfPTable(1) { WidthPercentage = 100f, SpacingAfter = 14f };
        var titleCell = new PdfPCell(new Phrase("SALARY SLIP", fontHeaderTitle))
        {
            Border = Rectangle.NO_BORDER,
            BackgroundColor = HeaderBg,
            PaddingLeft = 18f,
            PaddingRight = 18f,
            PaddingTop = 16f,
            PaddingBottom = 6f
        };
        headerTable.AddCell(titleCell);
        var subCell = new PdfPCell(new Phrase($"Pay period: {periodDisplay}", fontHeaderSub))
        {
            Border = Rectangle.NO_BORDER,
            BackgroundColor = HeaderBg,
            PaddingLeft = 18f,
            PaddingRight = 18f,
            PaddingBottom = 14f,
            PaddingTop = 0f
        };
        headerTable.AddCell(subCell);
        document.Add(headerTable);

        // --- Employee card ---
        document.Add(BuildEmployeeCard(row, periodDisplay, fontLabel, fontValue));

        document.Add(new Paragraph(" ") { SpacingAfter = 4f });

        // --- Earnings ---
        document.Add(SectionTitleTable("EARNINGS", fontSection));
        document.Add(BuildAmountTable(slip.Earnings, fontLabel, fontValue, fontValueBold, subtleLabelBg: true));

        document.Add(new Paragraph(" ") { SpacingAfter = 6f });

        // --- Deductions ---
        if (slip.Deductions.Count > 0 || slip.TotalDeductionsMoney != null)
        {
            document.Add(SectionTitleTable("DEDUCTIONS", fontSection));
            if (slip.Deductions.Count > 0)
            {
                document.Add(BuildAmountTable(slip.Deductions, fontLabel, fontValue, fontValueBold, subtleLabelBg: true));
            }

            if (slip.TotalDeductionsMoney is { } totalDed)
            {
                document.Add(BuildSummaryRowTable("Total deductions", totalDed, currencyCode, fontValueBold, SectionBg));
            }

            document.Add(new Paragraph(" ") { SpacingAfter = 6f });
        }

        // --- Net pay (prominent) ---
        var fontNetLabel = FontFactory.GetFont(FontFactory.HELVETICA, 9f, TextMuted);
        document.Add(SectionTitleTable("NET PAY", fontSection));
        document.Add(BuildNetPayTable(slip.NetFormatted, fontNet, fontNetLabel));

        document.Add(new Paragraph(" ") { SpacingAfter = 10f });

        // --- Footer ---
        var gen = $"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        var calc = row.CalculatedAt != default
            ? $"  |  Payroll calculated {row.CalculatedAt:yyyy-MM-dd HH:mm} UTC"
            : string.Empty;
        var footer = new Paragraph(gen + calc, fontFooter) { SpacingBefore = 4f };
        document.Add(footer);

        document.Close();
        return ms.ToArray();
    }

    private static PdfPTable BuildEmployeeCard(PayrollResultItemDto row, string periodDisplay, Font labelFont, Font valueFont)
    {
        var t = new PdfPTable(2) { WidthPercentage = 100f };
        t.SetWidths(new float[] { 32f, 68f });

        void AddRow(string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var c0 = Cell(label + ":", labelFont, LabelBg, Element.ALIGN_LEFT);
            var c1 = Cell(value.Trim(), valueFont, BaseColor.WHITE, Element.ALIGN_LEFT);
            c0.Border = Rectangle.BOX;
            c1.Border = Rectangle.BOX;
            c0.BorderColor = BorderGray;
            c1.BorderColor = BorderGray;
            c0.BorderWidth = 0.5f;
            c1.BorderWidth = 0.5f;
            t.AddCell(c0);
            t.AddCell(c1);
        }

        AddRow("Employee", row.EmployeeName);
        AddRow("Employee code", row.EmployeeCode);
        AddRow("Department", row.DepartmentName);
        AddRow("Pay period", periodDisplay);

        return t;
    }

    private static PdfPTable SectionTitleTable(string title, Font font)
    {
        var t = new PdfPTable(1) { WidthPercentage = 100f, SpacingAfter = 4f };
        var c = new PdfPCell(new Phrase(title, font))
        {
            Border = Rectangle.BOX,
            BorderWidth = 0.5f,
            BorderColor = BorderGray,
            BackgroundColor = SectionBg,
            PaddingTop = 7f,
            PaddingBottom = 7f,
            PaddingLeft = 10f,
            PaddingRight = 10f
        };
        t.AddCell(c);
        return t;
    }

    private static PdfPTable BuildAmountTable(
        List<(string Label, string Value)> rows,
        Font labelFont,
        Font valueFont,
        Font valueBoldFont,
        bool subtleLabelBg)
    {
        var t = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 2f };
        t.SetWidths(new float[] { 58f, 42f });

        foreach (var (label, value) in rows)
        {
            var isBold = label.Contains("GROSS", StringComparison.OrdinalIgnoreCase)
                || label.Contains("TOTAL", StringComparison.OrdinalIgnoreCase);
            var lf = isBold ? valueBoldFont : labelFont;
            var vf = isBold ? valueBoldFont : valueFont;
            var bg = subtleLabelBg ? LabelBg : BaseColor.WHITE;
            var c0 = Cell(label, lf, bg, Element.ALIGN_LEFT);
            var c1 = Cell(value, vf, BaseColor.WHITE, Element.ALIGN_RIGHT);
            ApplyRowBorders(c0, c1);
            t.AddCell(c0);
            t.AddCell(c1);
        }

        return t;
    }

    private static PdfPTable BuildSummaryRowTable(string label, decimal amount, string currency, Font font, BaseColor bg)
    {
        var t = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 4f };
        t.SetWidths(new float[] { 58f, 42f });
        var val = FormatMoney(amount, currency);
        var c0 = Cell(label, font, bg, Element.ALIGN_LEFT);
        var c1 = Cell(val, font, bg, Element.ALIGN_RIGHT);
        c0.Border = Rectangle.BOX;
        c1.Border = Rectangle.BOX;
        c0.BorderWidth = 0.5f;
        c1.BorderWidth = 0.5f;
        c0.BorderColor = BorderGray;
        c1.BorderColor = BorderGray;
        t.AddCell(c0);
        t.AddCell(c1);
        return t;
    }

    private static PdfPTable BuildNetPayTable(string netFormatted, Font netFont, Font netLabelFont)
    {
        var t = new PdfPTable(1) { WidthPercentage = 100f, SpacingAfter = 2f };
        var p = new Paragraph { Alignment = Element.ALIGN_CENTER };
        p.Add(new Chunk("NET PAYABLE\n", netLabelFont));
        p.Add(new Chunk(netFormatted, netFont));
        var c = new PdfPCell(p)
        {
            Border = Rectangle.BOX,
            BorderWidth = 1f,
            BorderColor = HeaderBg,
            BackgroundColor = NetBg,
            HorizontalAlignment = Element.ALIGN_CENTER,
            VerticalAlignment = Element.ALIGN_MIDDLE,
            PaddingTop = 12f,
            PaddingBottom = 14f,
            PaddingLeft = 12f,
            PaddingRight = 12f
        };
        t.AddCell(c);
        return t;
    }

    private static PdfPCell Cell(string text, Font font, BaseColor bg, int align) =>
        new PdfPCell(new Phrase(text, font))
        {
            BackgroundColor = bg,
            HorizontalAlignment = align,
            VerticalAlignment = Element.ALIGN_MIDDLE,
            PaddingTop = 6f,
            PaddingBottom = 6f,
            PaddingLeft = 10f,
            PaddingRight = 10f
        };

    private static void ApplyRowBorders(PdfPCell c0, PdfPCell c1)
    {
        c0.Border = Rectangle.BOX;
        c1.Border = Rectangle.BOX;
        c0.BorderWidth = 0.5f;
        c1.BorderWidth = 0.5f;
        c0.BorderColor = BorderGray;
        c1.BorderColor = BorderGray;
    }

    private static SlipRows BuildSlipRows(PayrollResultItemDto row, string currencyCode)
    {
        var earnings = new List<(string, string)>();
        var deductions = new List<(string, string)>();

        AddMoney(earnings, "Basic salary", row.BasicSalary, currencyCode);
        AddMoney(earnings, "Total bonuses", row.TotalBonuses, currencyCode);

        AppendJsonArray(earnings, "Performance bonus", row.PerformanceBonuses, currencyCode);
        AppendJsonArray(earnings, "General bonus", row.GeneralBonuses, currencyCode);
        AppendJsonArray(earnings, "Gratuity", row.Gratuity, currencyCode);
        AppendJsonArray(earnings, "Overtime", row.OvertimeDetails, currencyCode);

        earnings.Add(("GROSS PAY (basic + bonuses)", FormatMoney(row.BasicSalary + row.TotalBonuses, currencyCode)));

        AppendJsonArray(deductions, "Loan deductions", row.LoanDeductions, currencyCode);
        AppendJsonArray(deductions, "Salary advance", row.SalaryAdvanceDeductions, currencyCode);
        AppendJsonArray(deductions, "Provident fund", row.PfDeductions, currencyCode);
        AppendJsonArray(deductions, "Social security", row.SocialSecurityDeductions, currencyCode);
        AppendJsonArray(deductions, "Tax", row.TaxDeductions, currencyCode);
        AppendJsonArray(deductions, "Attendance", row.AttendanceDeductions, currencyCode);
        AppendJsonArray(deductions, "Late attendance", row.LateAttendanceDeductions, currencyCode);
        AppendJsonArray(deductions, "Leave", row.LeaveDeductions, currencyCode);

        var netFormatted = FormatMoney(row.NetSalary, currencyCode);

        return new SlipRows(earnings, deductions, row.TotalDeductions > 0m ? row.TotalDeductions : null, netFormatted);
    }

    private sealed record SlipRows(
        List<(string Label, string Value)> Earnings,
        List<(string Label, string Value)> Deductions,
        decimal? TotalDeductionsMoney,
        string NetFormatted);

    private static void AddMoney(List<(string Label, string Value)> lines, string label, decimal amount, string currencyCode)
    {
        if (amount == 0m) return;
        lines.Add((label, FormatMoney(amount, currencyCode)));
    }

    private static string FormatMoney(decimal amount, string currencyCode) =>
        $"{currencyCode} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

    private static void AppendJsonArray(List<(string Label, string Value)> lines, string sectionTitle, string? json, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        var trimmed = json.Trim();
        if (trimmed is "[]" or "{}" or "null") return;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return;

            var any = false;
            var idx = 0;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                idx++;
                foreach (var prop in el.EnumerateObject())
                {
                    var formatted = FormatJsonProperty(prop, currencyCode);
                    if (formatted == null) continue;
                    any = true;
                    var label = idx > 1 ? $"{sectionTitle} ({idx}) - {Humanize(prop.Name)}" : $"{sectionTitle} - {Humanize(prop.Name)}";
                    lines.Add((label, formatted));
                }
            }

            if (!any) return;
        }
        catch
        {
            // ignore malformed JSON on slip
        }
    }

    private static string? FormatJsonProperty(JsonProperty prop, string currencyCode)
    {
        if (ShouldSkipJsonKey(prop.Name)) return null;

        switch (prop.Value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.String:
            {
                var s = prop.Value.GetString();
                return string.IsNullOrWhiteSpace(s) ? null : s;
            }
            case JsonValueKind.Number:
            {
                if (!prop.Value.TryGetDecimal(out var d) || d == 0m) return null;
                var n = prop.Name;
                if (n.Contains("Hour", StringComparison.OrdinalIgnoreCase) || n.Contains("Minutes", StringComparison.OrdinalIgnoreCase) || n.Contains("Days", StringComparison.OrdinalIgnoreCase) || n.Contains("Score", StringComparison.OrdinalIgnoreCase))
                    return d.ToString(CultureInfo.InvariantCulture);
                if (n.Contains("Amount", StringComparison.OrdinalIgnoreCase) || n.Contains("Deduction", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("Salary", StringComparison.OrdinalIgnoreCase) || n.Contains("Tax", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("Bonus", StringComparison.OrdinalIgnoreCase) || n.Contains("Installment", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("Net", StringComparison.OrdinalIgnoreCase) || n.Contains("Per", StringComparison.OrdinalIgnoreCase))
                    return FormatMoney(d, currencyCode);
                if (d == Math.Truncate(d))
                    return d.ToString("0", CultureInfo.InvariantCulture);
                return FormatMoney(d, currencyCode);
            }
            case JsonValueKind.True:
                return "Yes";
            case JsonValueKind.False:
                return null;
            default:
                return null;
        }
    }

    private static bool ShouldSkipJsonKey(string name) =>
        name.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase)
        || name.Equals("RuleId", StringComparison.OrdinalIgnoreCase)
        || name.Equals("PfId", StringComparison.OrdinalIgnoreCase)
        || name.Equals("LoanId", StringComparison.OrdinalIgnoreCase)
        || name.Equals("AdvanceId", StringComparison.OrdinalIgnoreCase)
        || name.Equals("TransactionId", StringComparison.OrdinalIgnoreCase);

    private static string Humanize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1]))
                sb.Append(' ');
            if (c == '_') sb.Append(' ');
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
