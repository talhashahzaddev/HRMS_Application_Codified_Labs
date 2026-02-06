namespace HRMS.API.Models.DTOs;

public class OrganizationSettingsDto
{
    public Guid OrganizationId { get; set; }
    public string? Currency { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int ManualAttendanceTimeLimitDays { get; set; } = 30; // Default 30 days
}

public class UpdateCurrencyRequest
{
    public string Currency { get; set; } = string.Empty;
}










