namespace HRMS.API.Models.Entities;

public class Organization : BaseEntity
{
    public Guid? OrganizationId { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? LogoUrl { get; set; }
    public string? SubscriptionType { get; set; } = "basic";
    public string? Status { get; set; } = "active";
    public Dictionary<string, object>? Settings { get; set; } = new();
}
