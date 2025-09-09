namespace HRMS.API.Models.DTOs;

public class CreateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object>? Permissions { get; set; }
    public bool IsSystemRole { get; set; } = false;
}

public class RoleDto
{
    public Guid RoleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object>? Permissions { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAt { get; set; }
}