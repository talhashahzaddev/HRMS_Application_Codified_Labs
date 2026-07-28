namespace HRMS.API.Services;

/// <summary>
/// Maps organization ISO 4217 currency codes to display symbols for payslip PDFs
/// (aligned with <c>SettingsService.getAvailableCurrencies()</c> on the frontend).
/// </summary>
public static class PayslipCurrencyDisplay
{
    private static readonly Dictionary<string, string> IsoToSymbol =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = "$",
            ["PKR"] = "₨",
            ["INR"] = "₹",
            ["AED"] = "د.إ",
            ["SAR"] = "﷼",
            ["EUR"] = "€",
            ["GBP"] = "£",
            ["JPY"] = "¥",
            ["CNY"] = "¥",
            ["AUD"] = "A$",
            ["CAD"] = "C$",
            ["CHF"] = "CHF",
            ["SGD"] = "S$",
            ["MYR"] = "RM",
            ["THB"] = "฿",
            ["IDR"] = "Rp",
            ["PHP"] = "₱",
            ["BHD"] = ".د.ب",
            ["KWD"] = "د.ك",
            ["OMR"] = "﷼",
            ["QAR"] = "﷼",
            ["EGP"] = "£",
            ["ZAR"] = "R",
            ["NZD"] = "NZ$",
            ["KRW"] = "₩",
            ["HKD"] = "HK$",
            ["NOK"] = "kr",
            ["SEK"] = "kr",
            ["DKK"] = "kr",
            ["PLN"] = "zł",
            ["TRY"] = "₺",
            ["RUB"] = "₽",
            ["BRL"] = "R$",
            ["MXN"] = "$"
        };

    /// <summary>
    /// Prefix shown before monetary amounts on payslip PDFs. When the org has no currency, uses "$"
    /// (same default as the Angular <c>getCurrencySymbol()</c> fallback).
    /// </summary>
    public static string GetDisplayPrefix(string? currencyCodeFromDb)
    {
        var raw = currencyCodeFromDb?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return "$";
        }

        if (raw.Length == 3 && raw.All(char.IsLetter))
        {
            var upper = raw.ToUpperInvariant();
            return IsoToSymbol.TryGetValue(upper, out var sym) ? sym : upper;
        }

        return raw;
    }

    /// <summary>
    /// Prefix for payslip PDFs built with iTextSharp built-in Helvetica (WinAnsi). Unicode symbols
    /// (₨, ₹, €, Arabic segments, etc.) do not render and appear blank; this returns ASCII/Latin-1
    /// symbols when safe, otherwise the ISO 4217 code.
    /// </summary>
    public static string GetPdfHelveticaSafePrefix(string? currencyCodeFromDb)
    {
        var symbol = GetDisplayPrefix(currencyCodeFromDb);
        if (IsPdfHelveticaWinAnsiSafe(symbol))
        {
            return symbol;
        }

        var raw = currencyCodeFromDb?.Trim();
        if (!string.IsNullOrEmpty(raw) && raw.Length == 3 && raw.All(char.IsLetter))
        {
            return raw.ToUpperInvariant();
        }

        return "$";
    }

    /// <summary>True if every character is likely to render with PDF standard Helvetica (WinAnsi).</summary>
    private static bool IsPdfHelveticaWinAnsiSafe(string s)
    {
        foreach (var c in s)
        {
            if (c is >= ' ' and <= '~')
            {
                continue;
            }

            // Pound and yen are in typical WinAnsi / built-in PDF Latin fonts.
            if (c is '\u00A3' or '\u00A5')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
