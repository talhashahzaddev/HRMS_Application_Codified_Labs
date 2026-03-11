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










