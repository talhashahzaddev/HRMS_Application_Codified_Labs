using System.Drawing;

namespace HRMS.API.Models.DTOs;

public class OrganizationSettingsDto
{
    public Guid OrganizationId { get; set; }
    public string? Currency { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int ManualAttendanceTimeLimitDays { get; set; } = 30; // Default 30 days
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
}

public class UpdateCurrencyRequest
{
    public string Currency { get; set; } = string.Empty;
}

public class UpdateLocalizationSettingsRequest
{
    public string Currency { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
}

public class GetLocaalizationSettings
{
    public string? Currency { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
}
public class UpdateCareerPageRequest
{
    public string? CareerBgImageUrl { get; set; }
    public string? CareerHeaderText { get; set; }
    public string? CareerDescription { get; set; }
    public string? LogoUrl { get; set; }
}

public class CareerPageDto
{
    public Guid OrganizationId { get; set; }
    public string? Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? CareerBgImageUrl { get; set; }
    public string? CareerHeaderText { get; set; }
    public string? CareerDescription { get; set; }
}

public class CompanyPolicyDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCompanyPolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

public class UpdateCompanyPolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}










