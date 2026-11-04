namespace HRMS.API.Services.PayslipLayouts;

public sealed class PayslipSlipRows
{
    public PayslipSlipRows(
        List<(string Label, string Value)> earnings,
        List<(string Label, string Value)> deductions,
        decimal? totalDeductionsMoney,
        string netFormatted,
        decimal grossPay)
    {
        Earnings = earnings;
        Deductions = deductions;
        TotalDeductionsMoney = totalDeductionsMoney;
        NetFormatted = netFormatted;
        GrossPay = grossPay;
    }

    public List<(string Label, string Value)> Earnings { get; }
    public List<(string Label, string Value)> Deductions { get; }
    public decimal? TotalDeductionsMoney { get; }
    public string NetFormatted { get; }
    public decimal GrossPay { get; }
}
