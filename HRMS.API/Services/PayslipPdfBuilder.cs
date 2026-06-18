using System.Globalization;
using System.Text;
using System.Text.Json;
using HRMS.API.Models.DTOs;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HRMS.API.Services;

/// <summary>Builds a single-page payslip PDF from payroll data and optional organization template.</summary>
public static class PayslipPdfBuilder
{
    private static readonly BaseColor DefaultHeaderBg = new(30, 58, 95);
    private static readonly BaseColor DefaultSectionBg = new(226, 232, 240);
    private static readonly BaseColor DefaultNetBg = new(219, 234, 254);
    private static readonly BaseColor LabelBg = new(248, 250, 252);
    private static readonly BaseColor BorderGray = new(203, 213, 225);
    private static readonly BaseColor TextDark = new(51, 65, 85);
    private static readonly BaseColor TextMuted = new(100, 116, 139);

    public static byte[] Build(
        PayrollResultItemDto row,
        string periodLabel,
        string currencyCode = "$",
        PayslipTemplateDto? template = null)
    {
        template ??= PayslipTemplateDefaults.CreateDefaultTemplate(row.OrganizationId);
        var fieldConfig = template.FieldConfig ?? PayslipTemplateDefaults.CreateDefaultFieldConfig();
        var options = fieldConfig.Options ?? new PayslipTemplateOptionsDto();

        var headerBg = ParseColor(template.PrimaryColor, DefaultHeaderBg);
        var sectionBg = ParseColor(template.SectionTitleColor, DefaultSectionBg);
        var netBg = ParseColor(template.AccentColor, DefaultNetBg);

        var periodDisplay = string.IsNullOrWhiteSpace(row.PeriodName) ? periodLabel : row.PeriodName;
        var slip = BuildSlipRows(row, currencyCode, fieldConfig, options);

        using var ms = new MemoryStream();
        var document = new Document(PageSize.A4, 36f, 36f, 36f, 36f);
        PdfWriter.GetInstance(document, ms);
        document.Open();

        var fontHeaderTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20f, BaseColor.WHITE);
        var fontHeaderSub = FontFactory.GetFont(FontFactory.HELVETICA, 10f, BaseColor.WHITE);
        var fontSection = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, TextDark);
        var fontLabel = FontFactory.GetFont(FontFactory.HELVETICA, 10f, TextDark);
        var fontValue = FontFactory.GetFont(FontFactory.HELVETICA, 10f, TextDark);
        var fontValueBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, TextDark);
        var fontNet = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13f, headerBg);
        var fontFooter = FontFactory.GetFont(FontFactory.HELVETICA, 8f, TextMuted);
        var fontAddress = FontFactory.GetFont(FontFactory.HELVETICA, 9f, BaseColor.WHITE);

        document.Add(BuildHeaderTable(template, periodDisplay, headerBg, fontHeaderTitle, fontHeaderSub, fontAddress));

        if (IsSectionVisible(fieldConfig, "employeeInfo"))
        {
            document.Add(BuildEmployeeCard(row, periodDisplay, fieldConfig, fontLabel, fontValue));
            document.Add(new Paragraph(" ") { SpacingAfter = 4f });
        }

        if (IsSectionVisible(fieldConfig, "earnings") && (slip.Earnings.Count > 0 || !options.HideZeroAmountLines))
        {
            var earningsTitle = GetSectionTitle(fieldConfig, "earnings", "EARNINGS");
            document.Add(SectionTitleTable(earningsTitle, fontSection, sectionBg));
            document.Add(BuildAmountTable(slip.Earnings, fontLabel, fontValue, fontValueBold, subtleLabelBg: true));
            document.Add(new Paragraph(" ") { SpacingAfter = 6f });
        }

        if (IsSectionVisible(fieldConfig, "deductions") &&
            (slip.Deductions.Count > 0 || slip.TotalDeductionsMoney != null))
        {
            var deductionsTitle = GetSectionTitle(fieldConfig, "deductions", "DEDUCTIONS");
            document.Add(SectionTitleTable(deductionsTitle, fontSection, sectionBg));
            if (slip.Deductions.Count > 0)
            {
                document.Add(BuildAmountTable(slip.Deductions, fontLabel, fontValue, fontValueBold, subtleLabelBg: true));
            }

            if (slip.TotalDeductionsMoney is { } totalDed && IsFieldVisible(fieldConfig.DeductionFields, "totalDeductions"))
            {
                var totalLabel = GetFieldLabel(fieldConfig.DeductionFields, "totalDeductions", "Total deductions");
                document.Add(BuildSummaryRowTable(totalLabel, totalDed, currencyCode, fontValueBold, sectionBg));
            }

            document.Add(new Paragraph(" ") { SpacingAfter = 6f });
        }

        if (IsSectionVisible(fieldConfig, "netPay") && IsFieldVisible(fieldConfig.NetPayFields, "netPayable"))
        {
            var netTitle = GetSectionTitle(fieldConfig, "netPay", "NET PAY");
            var netLabel = GetFieldLabel(fieldConfig.NetPayFields, "netPayable", "NET PAYABLE");
            var fontNetLabel = FontFactory.GetFont(FontFactory.HELVETICA, 9f, TextMuted);
            document.Add(SectionTitleTable(netTitle, fontSection, sectionBg));
            document.Add(BuildNetPayTable(slip.NetFormatted, netLabel, fontNet, fontNetLabel, netBg, headerBg));
            document.Add(new Paragraph(" ") { SpacingAfter = 10f });
        }

        if (IsSectionVisible(fieldConfig, "footer"))
        {
            var footerParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(template.FooterText))
            {
                footerParts.Add(template.FooterText.Trim());
            }

            if (template.ShowGeneratedAt)
            {
                footerParts.Add($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            }

            if (template.ShowPayrollCalculatedAt && row.CalculatedAt != default)
            {
                footerParts.Add($"Payroll calculated {row.CalculatedAt:yyyy-MM-dd HH:mm} UTC");
            }

            if (footerParts.Count > 0)
            {
                document.Add(new Paragraph(string.Join("  |  ", footerParts), fontFooter) { SpacingBefore = 4f });
            }
        }

        document.Close();
        return ms.ToArray();
    }

    private static PdfPTable BuildHeaderTable(
        PayslipTemplateDto template,
        string periodDisplay,
        BaseColor headerBg,
        Font fontHeaderTitle,
        Font fontHeaderSub,
        Font fontAddress)
    {
        var columns = string.IsNullOrWhiteSpace(template.LogoUrl) ? 1 : 2;
        var headerTable = new PdfPTable(columns) { WidthPercentage = 100f, SpacingAfter = 14f };

        if (columns == 2)
        {
            headerTable.SetWidths(new float[] { 22f, 78f });
            var logoCell = new PdfPCell { Border = Rectangle.NO_BORDER, BackgroundColor = headerBg, Padding = 12f, VerticalAlignment = Element.ALIGN_MIDDLE };
            try
            {
                var image = Image.GetInstance(new Uri(template.LogoUrl!));
                image.ScaleToFit(72f, 72f);
                logoCell.AddElement(image);
            }
            catch
            {
                logoCell.AddElement(new Phrase(" ", fontHeaderSub));
            }

            headerTable.AddCell(logoCell);
        }

        var textTable = new PdfPTable(1) { WidthPercentage = 100f };
        textTable.AddCell(HeaderTextCell(template.HeaderTitle ?? "SALARY SLIP", fontHeaderTitle, headerBg, paddingBottom: 4f));
        if (!string.IsNullOrWhiteSpace(template.HeaderSubtitle))
        {
            textTable.AddCell(HeaderTextCell(template.HeaderSubtitle.Trim(), fontHeaderSub, headerBg, paddingBottom: 2f));
        }

        textTable.AddCell(HeaderTextCell($"Pay period: {periodDisplay}", fontHeaderSub, headerBg, paddingBottom: 10f));
        if (!string.IsNullOrWhiteSpace(template.CompanyAddress))
        {
            textTable.AddCell(HeaderTextCell(template.CompanyAddress.Trim(), fontAddress, headerBg, paddingBottom: 12f));
        }

        var textCell = new PdfPCell(textTable)
        {
            Border = Rectangle.NO_BORDER,
            BackgroundColor = headerBg,
            Padding = 0f
        };
        headerTable.AddCell(textCell);

        return headerTable;
    }

    private static PdfPCell HeaderTextCell(string text, Font font, BaseColor bg, float paddingBottom)
    {
        return new PdfPCell(new Phrase(text, font))
        {
            Border = Rectangle.NO_BORDER,
            BackgroundColor = bg,
            PaddingLeft = 18f,
            PaddingRight = 18f,
            PaddingTop = 0f,
            PaddingBottom = paddingBottom
        };
    }

    private static PdfPTable BuildEmployeeCard(
        PayrollResultItemDto row,
        string periodDisplay,
        PayslipTemplateFieldConfigDto fieldConfig,
        Font labelFont,
        Font valueFont)
    {
        var t = new PdfPTable(2) { WidthPercentage = 100f };
        t.SetWidths(new float[] { 32f, 68f });

        void AddRow(string fieldKey, string defaultLabel, string? value)
        {
            if (!IsFieldVisible(fieldConfig.EmployeeFields, fieldKey)) return;
            if (string.IsNullOrWhiteSpace(value)) return;
            var label = GetFieldLabel(fieldConfig.EmployeeFields, fieldKey, defaultLabel);
            var c0 = Cell(label + ":", labelFont, LabelBg, Element.ALIGN_LEFT);
            var c1 = Cell(value.Trim(), valueFont, BaseColor.WHITE, Element.ALIGN_LEFT);
            ApplyRowBorders(c0, c1);
            t.AddCell(c0);
            t.AddCell(c1);
        }

        AddRow("employeeName", "Employee", row.EmployeeName);
        AddRow("employeeCode", "Employee code", row.EmployeeCode);
        AddRow("departmentName", "Department", row.DepartmentName);
        AddRow("payPeriod", "Pay period", periodDisplay);

        return t;
    }

    private static PdfPTable SectionTitleTable(string title, Font font, BaseColor bg)
    {
        var t = new PdfPTable(1) { WidthPercentage = 100f, SpacingAfter = 4f };
        var c = new PdfPCell(new Phrase(title, font))
        {
            Border = Rectangle.BOX,
            BorderWidth = 0.5f,
            BorderColor = BorderGray,
            BackgroundColor = bg,
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

    private static PdfPTable BuildNetPayTable(
        string netFormatted,
        string netLabel,
        Font netFont,
        Font netLabelFont,
        BaseColor netBg,
        BaseColor borderColor)
    {
        var t = new PdfPTable(1) { WidthPercentage = 100f, SpacingAfter = 2f };
        var p = new Paragraph { Alignment = Element.ALIGN_CENTER };
        p.Add(new Chunk(netLabel + "\n", netLabelFont));
        p.Add(new Chunk(netFormatted, netFont));
        var c = new PdfPCell(p)
        {
            Border = Rectangle.BOX,
            BorderWidth = 1f,
            BorderColor = borderColor,
            BackgroundColor = netBg,
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
        new(new Phrase(text, font))
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

    private static SlipRows BuildSlipRows(
        PayrollResultItemDto row,
        string currencyCode,
        PayslipTemplateFieldConfigDto fieldConfig,
        PayslipTemplateOptionsDto options)
    {
        var earnings = new List<(string, string)>();
        var deductions = new List<(string, string)>();
        var hideZero = options.HideZeroAmountLines;

        if (IsFieldVisible(fieldConfig.EarningFields, "basicSalary"))
        {
            AddMoney(earnings, GetFieldLabel(fieldConfig.EarningFields, "basicSalary", "Basic salary"), row.BasicSalary, currencyCode, hideZero);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "totalBonuses"))
        {
            AddMoney(earnings, GetFieldLabel(fieldConfig.EarningFields, "totalBonuses", "Total bonuses"), row.TotalBonuses, currencyCode, hideZero);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "performanceBonuses"))
        {
            AppendJsonArray(earnings, GetFieldLabel(fieldConfig.EarningFields, "performanceBonuses", "Performance bonus"), row.PerformanceBonuses, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "generalBonuses"))
        {
            AppendJsonArray(earnings, GetFieldLabel(fieldConfig.EarningFields, "generalBonuses", "General bonus"), row.GeneralBonuses, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "gratuity"))
        {
            AppendJsonArray(earnings, GetFieldLabel(fieldConfig.EarningFields, "gratuity", "Gratuity"), row.Gratuity, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "overtime"))
        {
            AppendJsonArray(earnings, GetFieldLabel(fieldConfig.EarningFields, "overtime", "Overtime"), row.OvertimeDetails, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "grossPayLine"))
        {
            var gross = row.BasicSalary + row.TotalBonuses;
            if (!hideZero || gross != 0m)
            {
                earnings.Add((GetFieldLabel(fieldConfig.EarningFields, "grossPayLine", "GROSS PAY (basic + bonuses)"), FormatMoney(gross, currencyCode)));
            }
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "loanDeductions"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "loanDeductions", "Loan deductions"), row.LoanDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "salaryAdvance"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "salaryAdvance", "Salary advance"), row.SalaryAdvanceDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "providentFund"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "providentFund", "Provident fund"), row.PfDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "socialSecurity"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "socialSecurity", "Social security"), row.SocialSecurityDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "tax"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "tax", "Tax"), row.TaxDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "attendance"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "attendance", "Attendance"), row.AttendanceDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "lateAttendance"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "lateAttendance", "Late attendance"), row.LateAttendanceDeductions, currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "leave"))
        {
            AppendJsonArray(deductions, GetFieldLabel(fieldConfig.DeductionFields, "leave", "Leave"), row.LeaveDeductions, currencyCode, options);
        }

        var netFormatted = FormatMoney(row.NetSalary, currencyCode);
        decimal? totalDeductions = row.TotalDeductions > 0m ? row.TotalDeductions : null;

        return new SlipRows(earnings, deductions, totalDeductions, netFormatted);
    }

    private sealed record SlipRows(
        List<(string Label, string Value)> Earnings,
        List<(string Label, string Value)> Deductions,
        decimal? TotalDeductionsMoney,
        string NetFormatted);

    private static void AddMoney(List<(string Label, string Value)> lines, string label, decimal amount, string currencyCode, bool hideZero)
    {
        if (hideZero && amount == 0m) return;
        lines.Add((label, FormatMoney(amount, currencyCode)));
    }

    private static string FormatMoney(decimal amount, string currencyCode) =>
        $"{currencyCode} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

    private static void AppendJsonArray(
        List<(string Label, string Value)> lines,
        string sectionTitle,
        string? json,
        string currencyCode,
        PayslipTemplateOptionsDto options)
    {
        if (options.HideEmptyJsonSections && string.IsNullOrWhiteSpace(json)) return;
        var trimmed = json?.Trim();
        if (options.HideEmptyJsonSections && trimmed is "[]" or "{}" or "null") return;

        try
        {
            using var doc = JsonDocument.Parse(trimmed ?? "[]");
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
                    var formatted = FormatJsonProperty(prop, currencyCode, options.HideZeroAmountLines);
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

    private static string? FormatJsonProperty(JsonProperty prop, string currencyCode, bool hideZero)
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
                if (!prop.Value.TryGetDecimal(out var d)) return null;
                if (hideZero && d == 0m) return null;
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

    private static bool IsSectionVisible(PayslipTemplateFieldConfigDto config, string key)
    {
        if (!config.Sections.TryGetValue(key, out var section))
        {
            return true;
        }

        return section.Visible;
    }

    private static string GetSectionTitle(PayslipTemplateFieldConfigDto config, string key, string fallback)
    {
        if (config.Sections.TryGetValue(key, out var section) && !string.IsNullOrWhiteSpace(section.Title))
        {
            return section.Title!;
        }

        return fallback;
    }

    private static bool IsFieldVisible(Dictionary<string, PayslipTemplateFieldItemConfigDto> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return true;
        }

        return field.Visible;
    }

    private static string GetFieldLabel(Dictionary<string, PayslipTemplateFieldItemConfigDto> fields, string key, string fallback)
    {
        if (fields.TryGetValue(key, out var field) && !string.IsNullOrWhiteSpace(field.Label))
        {
            return field.Label!;
        }

        return fallback;
    }

    private static BaseColor ParseColor(string? hex, BaseColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex.Length != 7 || hex[0] != '#')
        {
            return fallback;
        }

        try
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return new BaseColor(r, g, b);
        }
        catch
        {
            return fallback;
        }
    }
}
