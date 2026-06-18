using System.Text.Json;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class PayslipTemplateRepository : BaseRepository, IPayslipTemplateRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PayslipTemplateRepository(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<PayslipTemplateDto?> GetActiveTemplateAsync(Guid organizationId)
    {
        const string sql = @"
            SELECT templateid          AS TemplateId,
                   organizationid      AS OrganizationId,
                   name                AS Name,
                   isactive            AS IsActive,
                   logourl             AS LogoUrl,
                   headertitle         AS HeaderTitle,
                   headersubtitle      AS HeaderSubtitle,
                   companyaddress      AS CompanyAddress,
                   footertext          AS FooterText,
                   primarycolor        AS PrimaryColor,
                   accentcolor         AS AccentColor,
                   sectiontitlecolor   AS SectionTitleColor,
                   showgeneratedat     AS ShowGeneratedAt,
                   showpayrollcalculatedat AS ShowPayrollCalculatedAt,
                   fieldconfig::text   AS FieldConfigJson
            FROM payroll.paysliptemplates
            WHERE organizationid = @organizationId
              AND isactive = true
            ORDER BY updatedat DESC
            LIMIT 1;";

        var row = await QuerySingleOrDefaultAsync<PayslipTemplateRow>(sql, new { organizationId });
        if (row == null)
        {
            return null;
        }

        return MapRow(row, isCustom: true);
    }

    public async Task<string?> GetOrganizationLogoUrlAsync(Guid organizationId)
    {
        const string sql = "SELECT logourl FROM auth.organizations WHERE organizationid = @organizationId;";
        return await QuerySingleOrDefaultAsync<string?>(sql, new { organizationId });
    }

    public async Task<Guid?> UpsertActiveTemplateAsync(Guid organizationId, UpsertPayslipTemplateRequest request, Guid? updatedBy)
    {
        var fieldConfig = request.FieldConfig ?? PayslipTemplateDefaults.CreateDefaultFieldConfig();
        var fieldConfigJson = JsonSerializer.Serialize(fieldConfig, JsonOptions);

        const string existingSql = @"
            SELECT templateid
            FROM payroll.paysliptemplates
            WHERE organizationid = @organizationId
              AND isactive = true
            ORDER BY updatedat DESC
            LIMIT 1;";

        var existingId = await QuerySingleOrDefaultAsync<Guid?>(existingSql, new { organizationId });

        if (existingId.HasValue)
        {
            const string updateSql = @"
                UPDATE payroll.paysliptemplates
                SET name = @name,
                    logourl = @logoUrl,
                    headertitle = @headerTitle,
                    headersubtitle = @headerSubtitle,
                    companyaddress = @companyAddress,
                    footertext = @footerText,
                    primarycolor = @primaryColor,
                    accentcolor = @accentColor,
                    sectiontitlecolor = @sectionTitleColor,
                    showgeneratedat = @showGeneratedAt,
                    showpayrollcalculatedat = @showPayrollCalculatedAt,
                    fieldconfig = @fieldConfigJson::jsonb,
                    updatedat = CURRENT_TIMESTAMP,
                    updatedby = @updatedBy
                WHERE templateid = @templateId;";

            await ExecuteAsync(updateSql, new
            {
                templateId = existingId.Value,
                name = request.Name,
                logoUrl = request.LogoUrl,
                headerTitle = request.HeaderTitle,
                headerSubtitle = request.HeaderSubtitle,
                companyAddress = request.CompanyAddress,
                footerText = request.FooterText,
                primaryColor = request.PrimaryColor,
                accentColor = request.AccentColor,
                sectionTitleColor = request.SectionTitleColor,
                showGeneratedAt = request.ShowGeneratedAt,
                showPayrollCalculatedAt = request.ShowPayrollCalculatedAt,
                fieldConfigJson,
                updatedBy
            });

            return existingId.Value;
        }

        const string insertSql = @"
            INSERT INTO payroll.paysliptemplates (
                organizationid, name, isactive, logourl, headertitle, headersubtitle,
                companyaddress, footertext, primarycolor, accentcolor, sectiontitlecolor,
                showgeneratedat, showpayrollcalculatedat, fieldconfig, createdby, updatedby
            )
            VALUES (
                @organizationId, @name, true, @logoUrl, @headerTitle, @headerSubtitle,
                @companyAddress, @footerText, @primaryColor, @accentColor, @sectionTitleColor,
                @showGeneratedAt, @showPayrollCalculatedAt, @fieldConfigJson::jsonb, @updatedBy, @updatedBy
            )
            RETURNING templateid;";

        return await QuerySingleOrDefaultAsync<Guid?>(insertSql, new
        {
            organizationId,
            name = request.Name,
            logoUrl = request.LogoUrl,
            headerTitle = request.HeaderTitle,
            headerSubtitle = request.HeaderSubtitle,
            companyAddress = request.CompanyAddress,
            footerText = request.FooterText,
            primaryColor = request.PrimaryColor,
            accentColor = request.AccentColor,
            sectionTitleColor = request.SectionTitleColor,
            showGeneratedAt = request.ShowGeneratedAt,
            showPayrollCalculatedAt = request.ShowPayrollCalculatedAt,
            fieldConfigJson,
            updatedBy
        });
    }

    private static PayslipTemplateDto MapRow(PayslipTemplateRow row, bool isCustom)
    {
        PayslipTemplateFieldConfigDto fieldConfig;
        try
        {
            fieldConfig = string.IsNullOrWhiteSpace(row.FieldConfigJson)
                ? PayslipTemplateDefaults.CreateDefaultFieldConfig()
                : JsonSerializer.Deserialize<PayslipTemplateFieldConfigDto>(row.FieldConfigJson, JsonOptions)
                  ?? PayslipTemplateDefaults.CreateDefaultFieldConfig();
        }
        catch
        {
            fieldConfig = PayslipTemplateDefaults.CreateDefaultFieldConfig();
        }

        return new PayslipTemplateDto
        {
            TemplateId = row.TemplateId,
            OrganizationId = row.OrganizationId,
            Name = row.Name,
            IsActive = row.IsActive,
            LogoUrl = row.LogoUrl,
            HeaderTitle = row.HeaderTitle ?? "SALARY SLIP",
            HeaderSubtitle = row.HeaderSubtitle,
            CompanyAddress = row.CompanyAddress,
            FooterText = row.FooterText,
            PrimaryColor = row.PrimaryColor ?? "#1E3A5F",
            AccentColor = row.AccentColor ?? "#DBEAFE",
            SectionTitleColor = row.SectionTitleColor ?? "#E2E8F0",
            ShowGeneratedAt = row.ShowGeneratedAt,
            ShowPayrollCalculatedAt = row.ShowPayrollCalculatedAt,
            FieldConfig = fieldConfig,
            IsCustom = isCustom
        };
    }

    private sealed class PayslipTemplateRow
    {
        public Guid TemplateId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = "Default";
        public bool IsActive { get; set; }
        public string? LogoUrl { get; set; }
        public string? HeaderTitle { get; set; }
        public string? HeaderSubtitle { get; set; }
        public string? CompanyAddress { get; set; }
        public string? FooterText { get; set; }
        public string? PrimaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? SectionTitleColor { get; set; }
        public bool ShowGeneratedAt { get; set; }
        public bool ShowPayrollCalculatedAt { get; set; }
        public string? FieldConfigJson { get; set; }
    }
}
