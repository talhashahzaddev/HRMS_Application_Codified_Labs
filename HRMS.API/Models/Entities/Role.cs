namespace HRMS.API.Models.Entities;

public class Role : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? RoleName { get; set; } 
    public string? Description { get; set; }
    public Dictionary<string, object>? Permissions { get; set; }
    public bool IsSystemRole { get; set; } = false;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
}
