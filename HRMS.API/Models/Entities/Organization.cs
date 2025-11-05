using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("organizations", Schema = "auth")]
public class Organization : BaseEntity
{
    [Key]
    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }
    
    [Column("name")]
    public string Name { get; set; } 
    
    [Column("domain")]
    public string? Domain { get; set; }
    
    [Column("logo_url")]
    public string? LogoUrl { get; set; }
    
    [Column("subscription_type")]
    public string? SubscriptionType { get; set; } 
    
    [Column("status")]
    public string? Status { get; set; }
    
    [Column("settings")]
    public Dictionary<string, object>? Settings { get; set; } = new();
    
    [Column("currency")]
    public string? Currency { get; set; }
}
