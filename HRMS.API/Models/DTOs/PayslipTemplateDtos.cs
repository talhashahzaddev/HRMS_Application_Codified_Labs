namespace HRMS.API.Models.DTOs;

public class PayslipTemplateSectionConfigDto
{
    public bool Visible { get; set; } = true;
    public string? Title { get; set; }
}

public class PayslipTemplateFieldItemConfigDto
{
    public bool Visible { get; set; } = true;
    public string? Label { get; set; }
}

public class PayslipTemplateOptionsDto
{
    public bool HideZeroAmountLines { get; set; } = true;
    public bool HideEmptyJsonSections { get; set; } = true;
}

public class PayslipTemplateFieldConfigDto
{
    public Dictionary<string, PayslipTemplateSectionConfigDto> Sections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PayslipTemplateFieldItemConfigDto> EmployeeFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PayslipTemplateFieldItemConfigDto> EarningFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PayslipTemplateFieldItemConfigDto> DeductionFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PayslipTemplateFieldItemConfigDto> NetPayFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public PayslipTemplateOptionsDto Options { get; set; } = new();
}

public class PayslipTemplateDto
{
    public Guid? TemplateId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "Default";
    public bool IsActive { get; set; } = true;
    public string? LogoUrl { get; set; }
    public string HeaderTitle { get; set; } = "SALARY SLIP";
    public string? HeaderSubtitle { get; set; }
    public string? CompanyAddress { get; set; }
    public string? FooterText { get; set; }
    public string PrimaryColor { get; set; } = "#1E3A5F";
    public string AccentColor { get; set; } = "#DBEAFE";
    public string SectionTitleColor { get; set; } = "#E2E8F0";
    public bool ShowGeneratedAt { get; set; } = true;
    public bool ShowPayrollCalculatedAt { get; set; } = true;
    public PayslipTemplateFieldConfigDto FieldConfig { get; set; } = PayslipTemplateDefaults.CreateDefaultFieldConfig();
    public bool IsCustom { get; set; }
}

public class UpsertPayslipTemplateRequest
{
    public string Name { get; set; } = "Default";
    public string? LogoUrl { get; set; }
    public string HeaderTitle { get; set; } = "SALARY SLIP";
    public string? HeaderSubtitle { get; set; }
    public string? CompanyAddress { get; set; }
    public string? FooterText { get; set; }
    public string PrimaryColor { get; set; } = "#1E3A5F";
    public string AccentColor { get; set; } = "#DBEAFE";
    public string SectionTitleColor { get; set; } = "#E2E8F0";
    public bool ShowGeneratedAt { get; set; } = true;
    public bool ShowPayrollCalculatedAt { get; set; } = true;
    public PayslipTemplateFieldConfigDto? FieldConfig { get; set; }
}

public class PayslipTemplatePreviewRequest
{
    public UpsertPayslipTemplateRequest? Template { get; set; }
}

public static class PayslipTemplateDefaults
{
    public static PayslipTemplateFieldConfigDto CreateDefaultFieldConfig() => new()
    {
        Sections = new Dictionary<string, PayslipTemplateSectionConfigDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["employeeInfo"] = new() { Visible = true, Title = "Employee Details" },
            ["earnings"] = new() { Visible = true, Title = "EARNINGS" },
            ["deductions"] = new() { Visible = true, Title = "DEDUCTIONS" },
            ["netPay"] = new() { Visible = true, Title = "NET PAY" },
            ["footer"] = new() { Visible = true }
        },
        EmployeeFields = new Dictionary<string, PayslipTemplateFieldItemConfigDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["employeeName"] = new() { Visible = true, Label = "Employee" },
            ["employeeCode"] = new() { Visible = true, Label = "Employee code" },
            ["departmentName"] = new() { Visible = true, Label = "Department" },
            ["payPeriod"] = new() { Visible = true, Label = "Pay period" }
        },
        EarningFields = new Dictionary<string, PayslipTemplateFieldItemConfigDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["basicSalary"] = new() { Visible = true, Label = "Basic salary" },
            ["totalBonuses"] = new() { Visible = true, Label = "Total bonuses" },
            ["performanceBonuses"] = new() { Visible = true, Label = "Performance bonus" },
            ["generalBonuses"] = new() { Visible = true, Label = "General bonus" },
            ["gratuity"] = new() { Visible = true, Label = "Gratuity" },
            ["overtime"] = new() { Visible = true, Label = "Overtime" },
            ["grossPayLine"] = new() { Visible = true, Label = "GROSS PAY (basic + bonuses)" }
        },
        DeductionFields = new Dictionary<string, PayslipTemplateFieldItemConfigDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["loanDeductions"] = new() { Visible = true, Label = "Loan deductions" },
            ["salaryAdvance"] = new() { Visible = true, Label = "Salary advance" },
            ["providentFund"] = new() { Visible = true, Label = "Provident fund" },
            ["socialSecurity"] = new() { Visible = true, Label = "Social security" },
            ["tax"] = new() { Visible = true, Label = "Tax" },
            ["attendance"] = new() { Visible = true, Label = "Attendance" },
            ["lateAttendance"] = new() { Visible = true, Label = "Late attendance" },
            ["leave"] = new() { Visible = true, Label = "Leave" },
            ["totalDeductions"] = new() { Visible = true, Label = "Total deductions" }
        },
        NetPayFields = new Dictionary<string, PayslipTemplateFieldItemConfigDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["netPayable"] = new() { Visible = true, Label = "NET PAYABLE" }
        },
        Options = new PayslipTemplateOptionsDto()
    };

    public static PayslipTemplateDto CreateDefaultTemplate(Guid organizationId, string? fallbackLogoUrl = null) => new()
    {
        OrganizationId = organizationId,
        LogoUrl = fallbackLogoUrl,
        FieldConfig = CreateDefaultFieldConfig(),
        IsCustom = false
    };

    public static PayrollResultItemDto CreateSamplePayrollRow(Guid organizationId) => new()
    {
        ResultId = Guid.NewGuid(),
        OrganizationId = organizationId,
        EmployeeId = Guid.NewGuid(),
        PeriodId = Guid.NewGuid(),
        PeriodName = "March 2025",
        EmployeeName = "Ali Hassan",
        EmployeeCode = "EMP-8821",
        DepartmentName = "Engineering",
        BasicSalary = 76500m,
        TotalBonuses = 28500m,
        TotalDeductions = 18400m,
        NetSalary = 86600m,
        CalculatedAt = DateTime.UtcNow.AddDays(-2)
    };
}
