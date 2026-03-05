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
public class MenuDto
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; }
    public string Icon { get; set; }
    public List<SubMenuDto> SubMenus { get; set; } = new();
}

public class SubMenuDto
{
    public Guid SubMenuId { get; set; }
    public string SubMenuName { get; set; }
    public string Icon { get; set; }
}

// Models/DTOs/CreateRoleRequest.cs
public class Create_RoleRequest
{
    public string RoleName { get; set; }
    public string Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public List<MenuPermissions> Menus { get; set; }
}

public class MenuPermissions
{
    public Guid MenuId { get; set; }
    public List<SubMenuPermission> SubMenus { get; set; }
}

public class SubMenuPermission
{
    public Guid SubMenuId { get; set; }
    public bool CanView { get; set; } = false;
    public bool CanAdd { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}
public class CreateRoleResponse
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public bool IsSystemRole { get; set; }
}
public class PermissionDetail
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; }
    public Guid SubMenuId { get; set; }
    public string SubMenuName { get; set; }
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
public class RoleDetailResponse
{
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public bool? IsSystemRole { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<RoleMenuPermission> Menus { get; set; }
}

public class RoleMenuPermission
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; }
    public List<RoleSubMenuPermission> SubMenus { get; set; }
}

public class RoleSubMenuPermission
{
    public Guid SubMenuId { get; set; }
    public string SubMenuName { get; set; }
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}