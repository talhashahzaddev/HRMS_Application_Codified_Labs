namespace HRMS.API.Models.DTOs;

public class OrganizationSettingsDto
{
    public Guid OrganizationId { get; set; }
    public string? Currency { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}

public class UpdateCurrencyRequest
{
    public string Currency { get; set; } = string.Empty;
}









