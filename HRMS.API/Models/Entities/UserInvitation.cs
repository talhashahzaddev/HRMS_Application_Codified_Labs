using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("userinvitations", Schema = "auth")]
public class UserInvitation : BaseEntity
{
    [Key]
    [Column("invitationid")]
    public Guid InvitationId { get; set; }

    [Column("userid")]
    public Guid UserId { get; set; }

    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("expiresat")]
    public DateTime ExpiresAt { get; set; }

    [Column("used")]
    public bool Used { get; set; } = false;

    // Optional: store org name for new users without an organization
    [Column("organizationname")]
    public string? OrganizationName { get; set; }
}
