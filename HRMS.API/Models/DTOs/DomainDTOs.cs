namespace HRMS.API.Models.DTOs;

/// <summary>
/// Response DTO for domain validation
/// </summary>
public class DomainValidationResponse
{
    public bool IsValid { get; set; }
    public string? Domain { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? Message { get; set; }
}


