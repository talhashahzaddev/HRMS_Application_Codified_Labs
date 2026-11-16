using System.Globalization;
using System.Text;
using System.Text.Json;
using HRMS.API.Models.DTOs;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HRMS.API.Services.PayslipLayouts;

public static class PayslipLayoutShared
{
    public static readonly BaseColor DefaultHeaderBg = new(30, 58, 95);
    public static readonly BaseColor DefaultSectionBg = new(226, 232, 240);
    public static readonly BaseColor DefaultNetBg = new(219, 234, 254);
    public static readonly BaseColor LabelBg = new(248, 250, 252);
    public static readonly BaseColor BorderGray = new(203, 213, 225);
    public static readonly BaseColor TextDark = new(51, 65, 85);
    public static readonly BaseColor TextMuted = new(100, 116, 139);

    public static PayslipSlipRows BuildSlipRows(
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
            AppendDetailAmounts(earnings, GetFieldLabel(fieldConfig.EarningFields, "performanceBonuses", "Performance bonus"), row.PerformanceBonuses, "BonusAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "generalBonuses"))
        {
            AppendDetailAmounts(earnings, GetFieldLabel(fieldConfig.EarningFields, "generalBonuses", "General bonus"), row.GeneralBonuses, "BonusAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "gratuity"))
        {
            AppendDetailAmounts(earnings, GetFieldLabel(fieldConfig.EarningFields, "gratuity", "Gratuity"), row.Gratuity, "GratuityAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.EarningFields, "overtime"))
        {
            AppendDetailAmounts(earnings, GetFieldLabel(fieldConfig.EarningFields, "overtime", "Overtime"), row.OvertimeDetails, "TotalOvertimeAmount", currencyCode, options);
        }

        var grossPay = row.BasicSalary + row.TotalBonuses;
        if (IsFieldVisible(fieldConfig.EarningFields, "grossPayLine"))
        {
            if (!hideZero || grossPay != 0m)
            {
                earnings.Add((GetFieldLabel(fieldConfig.EarningFields, "grossPayLine", "GROSS PAY (basic + bonuses)"), FormatMoney(grossPay, currencyCode)));
            }
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "loanDeductions"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "loanDeductions", "Loan deductions"), row.LoanDeductions, "InstallmentAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "salaryAdvance"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "salaryAdvance", "Salary advance"), row.SalaryAdvanceDeductions, "DeductedAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "providentFund"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "providentFund", "Provident fund"), row.PfDeductions, "EmployeeAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "socialSecurity"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "socialSecurity", "Social security"), row.SocialSecurityDeductions, "EmployeeAmount", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "tax"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "tax", "Tax"), row.TaxDeductions, "MonthlyTax", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "attendance"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "attendance", "Attendance"), row.AttendanceDeductions, "TotalDeduction", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "lateAttendance"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "lateAttendance", "Late attendance"), row.LateAttendanceDeductions, "TotalDeduction", currencyCode, options);
        }

        if (IsFieldVisible(fieldConfig.DeductionFields, "leave"))
        {
            AppendDetailAmounts(deductions, GetFieldLabel(fieldConfig.DeductionFields, "leave", "Leave"), row.LeaveDeductions, "TotalDeduction", currencyCode, options);
        }

        var netFormatted = FormatMoney(row.NetSalary, currencyCode);
        decimal? totalDeductions = row.TotalDeductions > 0m ? row.TotalDeductions : null;

        return new PayslipSlipRows(earnings, deductions, totalDeductions, netFormatted, grossPay);
    }

    public static void RenderHeader(Document document, PayslipLayoutContext context)
    {
        document.Add(BuildHeaderTable(
            context.Template,
            context.LogoBytes,
            context.PeriodDisplay,
            context.HeaderBg,
            context.Fonts.HeaderTitle,
            context.Fonts.HeaderSub,
            context.Fonts.Address));
    }

    public static void RenderEmployeeSection(Document document, PayslipLayoutContext context)
    {
        if (!IsSectionVisible(context.FieldConfig, "employeeInfo")) return;

        document.Add(BuildEmployeeCard(
            context.Row,
            context.PeriodDisplay,
            context.FieldConfig,
            context.Fonts.Label,
            context.Fonts.Value));
        document.Add(new Paragraph(" ") { SpacingAfter = 4f });
    }

    public static void RenderEarningsSection(Document document, PayslipLayoutContext context)
    {
        if (!IsSectionVisible(context.FieldConfig, "earnings")) return;
        if (context.Slip.Earnings.Count == 0 && context.Options.HideZeroAmountLines) return;

        var earningsTitle = GetSectionTitle(context.FieldConfig, "earnings", "EARNINGS");
        document.Add(SectionTitleTable(earningsTitle, context.Fonts.Section, context.SectionBg));
        document.Add(BuildAmountTable(context.Slip.Earnings, context.Fonts.Label, context.Fonts.Value, context.Fonts.ValueBold, subtleLabelBg: true));
        document.Add(new Paragraph(" ") { SpacingAfter = 6f });
    }

    public static void RenderDeductionsSection(Document document, PayslipLayoutContext context)
    {
        if (!IsSectionVisible(context.FieldConfig, "deductions")) return;
        if (context.Slip.Deductions.Count == 0 && context.Slip.TotalDeductionsMoney == null) return;

        var deductionsTitle = GetSectionTitle(context.FieldConfig, "deductions", "DEDUCTIONS");
        document.Add(SectionTitleTable(deductionsTitle, context.Fonts.Section, context.SectionBg));

        if (context.Slip.Deductions.Count > 0)
        {
            document.Add(BuildAmountTable(context.Slip.Deductions, context.Fonts.Label, context.Fonts.Value, context.Fonts.ValueBold, subtleLabelBg: true));
        }

        if (context.Slip.TotalDeductionsMoney is { } totalDed &&
            IsFieldVisible(context.FieldConfig.DeductionFields, "totalDeductions"))
        {
            var totalLabel = GetFieldLabel(context.FieldConfig.DeductionFields, "totalDeductions", "Total deductions");
            document.Add(BuildSummaryRowTable(totalLabel, totalDed, context.CurrencyCode, context.Fonts.ValueBold, context.SectionBg));
        }

        document.Add(new Paragraph(" ") { SpacingAfter = 6f });
    }

    public static void RenderSideBySideEarningsDeductions(Document document, PayslipLayoutContext context, bool compactFonts = false)
    {
        var showEarnings = IsSectionVisible(context.FieldConfig, "earnings")
            && (context.Slip.Earnings.Count > 0 || !context.Options.HideZeroAmountLines);
        var showDeductions = IsSectionVisible(context.FieldConfig, "deductions")
            && (context.Slip.Deductions.Count > 0 || context.Slip.TotalDeductionsMoney != null);

        if (!showEarnings && !showDeductions)
        {
            return;
        }

        if (!showEarnings || !showDeductions)
        {
            if (showEarnings)
            {
                RenderEarningsSection(document, context);
            }

            if (showDeductions)
            {
                RenderDeductionsSection(document, context);
            }

            return;
        }

        var labelFont = compactFonts ? context.Fonts.CompactLabel : context.Fonts.Label;
        var valueFont = compactFonts ? context.Fonts.CompactValue : context.Fonts.Value;
        var valueBoldFont = context.Fonts.ValueBold;

        var earningsTitle = GetSectionTitle(context.FieldConfig, "earnings", "EARNINGS");
        var deductionsTitle = GetSectionTitle(context.FieldConfig, "deductions", "DEDUCTIONS");
        var earningsRows = context.Slip.Earnings;
        var deductionsRows = context.Slip.Deductions;

        var table = new PdfPTable(4)
        {
            WidthPercentage = 100f,
            SpacingAfter = compactFonts ? 8f : 6f,
            SplitLate = false,
            SplitRows = true,
            KeepTogether = false
        };
        table.SetWidths(new float[] { 27f, 23f, 27f, 23f });

        table.AddCell(SideSectionTitleCell(earningsTitle, context.Fonts.Section, context.SectionBg));
        table.AddCell(SideSectionTitleCell(deductionsTitle, context.Fonts.Section, context.SectionBg));

        var rowCount = Math.Max(earningsRows.Count, deductionsRows.Count);
        for (var i = 0; i < rowCount; i++)
        {
            if (i < earningsRows.Count)
            {
                AddSideAmountCells(table, earningsRows[i], labelFont, valueFont, valueBoldFont);
            }
            else
            {
                table.AddCell(SideEmptyAmountCell());
                table.AddCell(SideEmptyAmountCell());
            }

            if (i < deductionsRows.Count)
            {
                AddSideAmountCells(table, deductionsRows[i], labelFont, valueFont, valueBoldFont);
            }
            else
            {
                table.AddCell(SideEmptyAmountCell());
                table.AddCell(SideEmptyAmountCell());
            }
        }

        if (context.Slip.TotalDeductionsMoney is { } totalDed &&
            IsFieldVisible(context.FieldConfig.DeductionFields, "totalDeductions"))
        {
            var totalLabel = GetFieldLabel(context.FieldConfig.DeductionFields, "totalDeductions", "Total deductions");
            table.AddCell(SideEmptyAmountCell());
            table.AddCell(SideEmptyAmountCell());
            table.AddCell(SideSummaryCell(totalLabel, valueBoldFont, context.SectionBg, Element.ALIGN_LEFT));
            table.AddCell(SideSummaryCell(FormatMoney(totalDed, context.CurrencyCode), valueBoldFont, context.SectionBg, Element.ALIGN_RIGHT));
        }

        document.Add(table);
    }

    public static void RenderSummaryStrip(Document document, PayslipLayoutContext context)
    {
        var showEarnings = IsSectionVisible(context.FieldConfig, "earnings");
        var showDeductions = IsSectionVisible(context.FieldConfig, "deductions");
        var showNet = IsSectionVisible(context.FieldConfig, "netPay")
            && IsFieldVisible(context.FieldConfig.NetPayFields, "netPayable");

        if (!showEarnings && !showDeductions && !showNet)
        {
            return;
        }

        var table = new PdfPTable(3)
        {
            WidthPercentage = 100f,
            SpacingAfter = 12f,
            SplitLate = false,
            KeepTogether = false
        };
        table.SetWidths(new float[] { 33f, 34f, 33f });

        if (showEarnings)
        {
            table.AddCell(BuildSummaryStripCell(
                "Gross pay",
                FormatMoney(context.Slip.GrossPay, context.CurrencyCode),
                context,
                highlight: false));
        }
        else
        {
            table.AddCell(BuildSummaryStripCell(string.Empty, string.Empty, context, highlight: false));
        }

        if (showDeductions)
        {
            var deductions = context.Slip.TotalDeductionsMoney ?? context.Row.TotalDeductions;
            table.AddCell(BuildSummaryStripCell(
                "Total deductions",
                FormatMoney(deductions, context.CurrencyCode),
                context,
                highlight: false));
        }
        else
        {
            table.AddCell(BuildSummaryStripCell(string.Empty, string.Empty, context, highlight: false));
        }

        if (showNet)
        {
            table.AddCell(BuildSummaryStripCell(
                GetFieldLabel(context.FieldConfig.NetPayFields, "netPayable", "Net payable"),
                context.Slip.NetFormatted,
                context,
                highlight: true));
        }
        else
        {
            table.AddCell(BuildSummaryStripCell(string.Empty, string.Empty, context, highlight: false));
        }

        document.Add(table);
    }

    public static void RenderNetPaySection(Document document, PayslipLayoutContext context)
    {
        if (!IsSectionVisible(context.FieldConfig, "netPay")) return;
        if (!IsFieldVisible(context.FieldConfig.NetPayFields, "netPayable")) return;

        var netTitle = GetSectionTitle(context.FieldConfig, "netPay", "NET PAY");
        var netLabel = GetFieldLabel(context.FieldConfig.NetPayFields, "netPayable", "NET PAYABLE");
        document.Add(SectionTitleTable(netTitle, context.Fonts.Section, context.SectionBg));
        document.Add(BuildNetPayTable(context.Slip.NetFormatted, netLabel, context.Fonts.Net, context.Fonts.NetLabel, context.NetBg, context.HeaderBg));
        document.Add(new Paragraph(" ") { SpacingAfter = 10f });
    }

    public static void RenderFooter(Document document, PayslipLayoutContext context)
    {
        if (!IsSectionVisible(context.FieldConfig, "footer")) return;

        var footerParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(context.Template.FooterText))
        {
            footerParts.Add(context.Template.FooterText.Trim());
        }

        if (context.Template.ShowGeneratedAt)
        {
            footerParts.Add($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        }

        if (context.Template.ShowPayrollCalculatedAt && context.Row.CalculatedAt != default)
        {
            footerParts.Add($"Payroll calculated {context.Row.CalculatedAt:yyyy-MM-dd HH:mm} UTC");
        }

        if (footerParts.Count > 0)
        {
            document.Add(new Paragraph(string.Join("  |  ", footerParts), context.Fonts.Footer) { SpacingBefore = 4f });
        }
    }

    public static PdfPTable BuildHeaderTable(
        PayslipTemplateDto template,
        byte[]? logoBytes,
        string periodDisplay,
        BaseColor headerBg,
        Font fontHeaderTitle,
        Font fontHeaderSub,
        Font fontAddress)
    {
        // The logo is downloaded (with SSRF/size/timeout guards) by PayslipLogoLoader before rendering;
        // the renderer never fetches remote URLs itself.
        Image? logo = null;
        if (logoBytes is { Length: > 0 })
        {
            try
            {
                logo = Image.GetInstance(logoBytes);
                logo.ScaleToFit(72f, 72f);
            }
            catch
            {
                logo = null;
            }
        }

        var columns = logo == null ? 1 : 2;
        var headerTable = new PdfPTable(columns) { WidthPercentage = 100f, SpacingAfter = 14f };

        if (logo != null)
        {
            headerTable.SetWidths(new float[] { 22f, 78f });
            var logoCell = new PdfPCell { Border = Rectangle.NO_BORDER, BackgroundColor = headerBg, Padding = 12f, VerticalAlignment = Element.ALIGN_MIDDLE };
            logoCell.AddElement(logo);
            headerTable.AddCell(logoCell);
        }

        var textTable = new PdfPTable(1) { WidthPercentage = 100f };
        var headerTitle = string.IsNullOrWhiteSpace(template.HeaderTitle) ? "SALARY SLIP" : template.HeaderTitle.Trim();
        textTable.AddCell(HeaderTextCell(headerTitle, fontHeaderTitle, headerBg, paddingBottom: 4f));
        if (!string.IsNullOrWhiteSpace(template.HeaderSubtitle))
        {
            textTable.AddCell(HeaderTextCell(template.HeaderSubtitle.Trim(), fontHeaderSub, headerBg, paddingBottom: 2f));
        }

        textTable.AddCell(HeaderTextCell($"Pay period: {periodDisplay}", fontHeaderSub, headerBg, paddingBottom: 10f));
        if (!string.IsNullOrWhiteSpace(template.CompanyAddress))
        {
            textTable.AddCell(HeaderTextCell(template.CompanyAddress.Trim(), fontAddress, headerBg, paddingBottom: 12f));
        }

        var textCell = new PdfPCell
        {
            Border = Rectangle.NO_BORDER,
            BackgroundColor = headerBg,
            Padding = 0f
        };
        textTable.WidthPercentage = 100f;
        textCell.AddElement(textTable);
        headerTable.AddCell(textCell);

        return headerTable;
    }

    public static PdfPTable BuildEmployeeCard(
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

    public static PdfPTable SectionTitleTable(string title, Font font, BaseColor bg)
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

    public static PdfPTable BuildAmountTable(
        List<(string Label, string Value)> rows,
        Font labelFont,
        Font valueFont,
        Font valueBoldFont,
        bool subtleLabelBg,
        float? labelWidthPercent = null)
    {
        var t = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 2f };
        t.SetWidths(new float[] { labelWidthPercent ?? 58f, 100f - (labelWidthPercent ?? 58f) });

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

    public static PdfPTable BuildSummaryRowTable(string label, decimal amount, string currency, Font font, BaseColor bg)
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

    public static PdfPTable BuildNetPayTable(
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

    public static PdfPCell Cell(string text, Font font, BaseColor bg, int align) =>
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

    public static void ApplyRowBorders(PdfPCell c0, PdfPCell c1)
    {
        c0.Border = Rectangle.BOX;
        c1.Border = Rectangle.BOX;
        c0.BorderWidth = 0.5f;
        c1.BorderWidth = 0.5f;
        c0.BorderColor = BorderGray;
        c1.BorderColor = BorderGray;
    }

    public static string FormatMoney(decimal amount, string currencyCode) =>
        $"{currencyCode} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

    public static BaseColor ParseColor(string? hex, BaseColor fallback)
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

    public static bool IsSectionVisible(PayslipTemplateFieldConfigDto config, string key)
    {
        if (!config.Sections.TryGetValue(key, out var section))
        {
            return true;
        }

        return section.Visible;
    }

    public static string GetSectionTitle(PayslipTemplateFieldConfigDto config, string key, string fallback)
    {
        if (config.Sections.TryGetValue(key, out var section) && !string.IsNullOrWhiteSpace(section.Title))
        {
            return section.Title!;
        }

        return fallback;
    }

    public static bool IsFieldVisible(Dictionary<string, PayslipTemplateFieldItemConfigDto> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return true;
        }

        return field.Visible;
    }

    public static string GetFieldLabel(Dictionary<string, PayslipTemplateFieldItemConfigDto> fields, string key, string fallback)
    {
        if (fields.TryGetValue(key, out var field) && !string.IsNullOrWhiteSpace(field.Label))
        {
            return field.Label!;
        }

        return fallback;
    }

    private static void AddSideAmountCells(
        PdfPTable table,
        (string Label, string Value) row,
        Font labelFont,
        Font valueFont,
        Font valueBoldFont)
    {
        var isBold = row.Label.Contains("GROSS", StringComparison.OrdinalIgnoreCase)
            || row.Label.Contains("TOTAL", StringComparison.OrdinalIgnoreCase);
        var lf = isBold ? valueBoldFont : labelFont;
        var vf = isBold ? valueBoldFont : valueFont;

        var labelCell = Cell(row.Label, lf, LabelBg, Element.ALIGN_LEFT);
        var valueCell = Cell(row.Value, vf, BaseColor.WHITE, Element.ALIGN_RIGHT);
        ApplyRowBorders(labelCell, valueCell);
        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }

    private static PdfPCell SideSectionTitleCell(string title, Font font, BaseColor bg)
    {
        return new PdfPCell(new Phrase(title, font))
        {
            Colspan = 2,
            Border = Rectangle.BOX,
            BorderWidth = 0.5f,
            BorderColor = BorderGray,
            BackgroundColor = bg,
            PaddingTop = 7f,
            PaddingBottom = 7f,
            PaddingLeft = 10f,
            PaddingRight = 10f,
            HorizontalAlignment = Element.ALIGN_LEFT,
            VerticalAlignment = Element.ALIGN_MIDDLE
        };
    }

    private static PdfPCell SideEmptyAmountCell()
    {
        return new PdfPCell(new Phrase(" "))
        {
            Border = Rectangle.BOX,
            BorderWidth = 0.5f,
            BorderColor = BorderGray,
            BackgroundColor = BaseColor.WHITE,
            PaddingTop = 6f,
            PaddingBottom = 6f,
            PaddingLeft = 10f,
            PaddingRight = 10f,
            MinimumHeight = 28f
        };
    }

    private static PdfPCell SideSummaryCell(string text, Font font, BaseColor bg, int align)
    {
        return new PdfPCell(new Phrase(text, font))
        {
            Border = Rectangle.BOX,
            BorderWidth = 0.5f,
            BorderColor = BorderGray,
            BackgroundColor = bg,
            HorizontalAlignment = align,
            VerticalAlignment = Element.ALIGN_MIDDLE,
            PaddingTop = 6f,
            PaddingBottom = 6f,
            PaddingLeft = 10f,
            PaddingRight = 10f
        };
    }

    private static PdfPCell BuildSummaryStripCell(string label, string value, PayslipLayoutContext context, bool highlight)
    {
        var p = new Paragraph { Alignment = Element.ALIGN_CENTER };
        if (!string.IsNullOrWhiteSpace(label))
        {
            p.Add(new Chunk(label + "\n", context.Fonts.SummaryLabel));
            p.Add(new Chunk(value, highlight ? context.Fonts.Net : context.Fonts.SummaryValue));
        }

        return new PdfPCell(p)
        {
            Border = Rectangle.BOX,
            BorderWidth = 0.5f,
            BorderColor = BorderGray,
            BackgroundColor = highlight ? context.NetBg : LabelBg,
            HorizontalAlignment = Element.ALIGN_CENTER,
            VerticalAlignment = Element.ALIGN_MIDDLE,
            PaddingTop = 12f,
            PaddingBottom = 12f,
            PaddingLeft = 8f,
            PaddingRight = 8f
        };
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

    private static void AddMoney(List<(string Label, string Value)> lines, string label, decimal amount, string currencyCode, bool hideZero)
    {
        if (hideZero && amount == 0m) return;
        lines.Add((label, FormatMoney(amount, currencyCode)));
    }

    /// <summary>
    /// Adds one payslip line per detail entry, using only the amount that was actually earned / deducted
    /// (<paramref name="amountKey"/>). The stored detail JSON also holds working values (annual tax, employer
    /// contribution, rates, "net salary after X", remaining loan balance…) which must not appear as payslip lines.
    /// </summary>
    private static void AppendDetailAmounts(
        List<(string Label, string Value)> lines,
        string sectionTitle,
        string? json,
        string amountKey,
        string currencyCode,
        PayslipTemplateOptionsDto options)
    {
        var trimmed = json?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed is "[]" or "{}" or "null") return;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

            var amounts = new List<decimal>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                if (!TryGetDecimalProperty(el, amountKey, out var amount)) continue;
                if (options.HideZeroAmountLines && amount == 0m) continue;
                amounts.Add(amount);
            }

            for (var i = 0; i < amounts.Count; i++)
            {
                var label = amounts.Count > 1 ? $"{sectionTitle} ({i + 1})" : sectionTitle;
                lines.Add((label, FormatMoney(amounts[i], currencyCode)));
            }
        }
        catch (JsonException)
        {
            // ignore malformed JSON on slip
        }
    }

    private static bool TryGetDecimalProperty(JsonElement element, string key, out decimal value)
    {
        value = 0m;
        foreach (var prop in element.EnumerateObject())
        {
            if (!prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
            if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDecimal(out value)) return true;
            if (prop.Value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(prop.Value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
            return false;
        }

        return false;
    }
}
