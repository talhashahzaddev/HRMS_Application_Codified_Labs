using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("users", Schema = "auth")]
public class User : BaseEntity
{
    [Key]
    [Column("user_id")]
    public Guid? UserId { get; set; }
    
    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }
    
    [Column("email")]
    public string? Email { get; set; } 
    
    [Column("password_hash")]
    public string? PasswordHash { get; set; }
    
    [Column("role_id")]
    public Guid? RoleId { get; set; }
    
    [Column("first_name")]
    public string? FirstName { get; set; }
    
    [Column("last_name")]
    public string? LastName { get; set; } 
    
    [Column("is_active")]
    public bool? IsActive { get; set; } 
    
    [Column("email_verified")]
    public bool? EmailVerified { get; set; } 
    
    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
    
    [Column("password_changed_at")]
    public DateTime? PasswordChangedAt { get; set; } 
    
    [Column("refresh_token")]
    public string? RefreshToken { get; set; }
    
    [Column("refresh_token_expires_at")]
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // Navigation properties
    [NotMapped]
    public Organization? Organization { get; set; }
    
    [NotMapped]
    public Role? Role { get; set; } 
}
