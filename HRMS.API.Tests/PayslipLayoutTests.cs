using HRMS.API.Models.DTOs;
using HRMS.API.Services;
using HRMS.API.Services.PayslipLayouts;
using Xunit;

namespace HRMS.API.Tests;

/// <summary>Regression tests for payslip PDF content and template handling.</summary>
public sealed class PayslipLayoutTests
{
    private static PayslipSlipRows Build(PayrollResultItemDto row) =>
        PayslipLayoutShared.BuildSlipRows(
            row,
            "PKR",
            PayslipTemplateDefaults.CreateDefaultFieldConfig(),
            new PayslipTemplateOptionsDto());

    [Fact]
    public void Tax_line_shows_only_the_monthly_tax_not_annual_or_working_values()
    {
        var row = new PayrollResultItemDto
        {
            BasicSalary = 100000m,
            NetSalary = 90000m,
            TotalDeductions = 10000m,
            TaxDeductions = """[{"EmployeeId":"00000000-0000-0000-0000-000000000001","MonthlyGross":100000,"PFExemptAmount":0,"MonthlyTaxableIncome":100000,"AnnualTaxableIncome":1200000,"TaxAmount":120000,"MonthlyTax":10000,"NetSalaryAfterTaxDeduction":90000}]"""
        };

        var slip = Build(row);

        var tax = Assert.Single(slip.Deductions);
        Assert.Equal("Tax", tax.Label);
        Assert.Equal("PKR 10,000.00", tax.Value);
    }

    [Fact]
    public void Social_security_line_excludes_employer_share_and_rates()
    {
        var row = new PayrollResultItemDto
        {
            BasicSalary = 50000m,
            SocialSecurityDeductions = """[{"EmployeeRate":1,"EmployerRate":5,"EmployeeAmount":500,"EmployerAmount":2500,"TotalAmount":3000,"NetSalaryAfterSocialSecurityDeduction":49500}]"""
        };

        var slip = Build(row);

        var ss = Assert.Single(slip.Deductions);
        Assert.Equal("PKR 500.00", ss.Value);
    }

    [Fact]
    public void Multiple_entries_are_numbered()
    {
        var row = new PayrollResultItemDto
        {
            LoanDeductions = """[{"InstallmentAmount":1000,"RemainingLoanAmountAfter":9000},{"InstallmentAmount":2000,"RemainingLoanAmountAfter":0}]"""
        };

        var slip = Build(row);

        Assert.Equal(new[] { "Loan deductions (1)", "Loan deductions (2)" }, slip.Deductions.Select(d => d.Label));
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/demo/logo.png", true)]
    [InlineData("http://example.com/logo.png", true)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("ftp://example.com/logo.png", false)]
    [InlineData("/relative/logo.png", false)]
    [InlineData("", false)]
    public void Only_absolute_http_logo_urls_are_allowed(string url, bool allowed)
    {
        Assert.Equal(allowed, PayslipLogoLoader.IsAllowedLogoUrl(url));
    }

    [Fact]
    public void Field_config_lookups_are_case_insensitive_and_null_safe_after_json_round_trip()
    {
        var config = new PayslipTemplateFieldConfigDto
        {
            Sections = new Dictionary<string, PayslipTemplateSectionConfigDto> { ["NETPAY"] = new() { Visible = false } },
            EmployeeFields = null!,
            Options = null!
        };

        var normalized = PayslipTemplateService.NormalizeFieldConfig(config);

        Assert.False(PayslipLayoutShared.IsSectionVisible(normalized, "netPay"));
        Assert.True(PayslipLayoutShared.IsFieldVisible(normalized.EmployeeFields, "employeeName"));
        Assert.NotNull(normalized.Options);
    }
}
