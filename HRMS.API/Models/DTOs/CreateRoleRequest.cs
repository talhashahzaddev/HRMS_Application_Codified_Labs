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
    public List<ActionDto> Actions { get; set; } = new();
}
public class ActionDto 
{
    public Guid ActionId { get; set; }
    public string ActionName { get; set; }
    public string ActionKey { get; set; }
}

// Models/DTOs/CreateRoleRequest.cs
public class UpdateRoleRequest
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public bool IsSystemRole { get; set; }
    public List<MenuPermissionDto> Menus { get; set; }
}
public class Create_RoleRequest
{
    public string RoleName { get; set; }
    public string Description { get; set; }
    public bool IsSystemRole { get; set; }
    public List<MenuPermissionDto> Menus { get; set; }
}

public class MenuPermissionDto
{
    public Guid MenuId { get; set; }
    public List<SubMenuPermissionDto> SubMenus { get; set; }
}

public class SubMenuPermissionDto
{
    public Guid SubMenuId { get; set; }
    public List<ActionPermissionDto> Actions { get; set; } // Changed from CanView/Add/Edit/Delete
}

public class ActionPermissionDto
{
    public Guid ActionId { get; set; }
    public bool HasPermission { get; set; } // Single permission flag per action
}
public class CreateRoleResponse
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public bool IsSystemRole { get; set; }
}
public class UpdateRoleResponse
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public bool IsSystemRole { get; set; }
}
public class DeleteRoleResponse
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; }
    public bool Deleted { get; set; }
}
public class PermissionDetail
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; }
    public Guid SubMenuId { get; set; }
    public string SubMenuName { get; set; }
    public Guid ActionId { get; set; }        // Added
    public string ActionName { get; set; }    // Added
    public string ActionKey { get; set; }     // Added
    public bool HasPermission { get; set; }   // Replaced CanView/Add/Edit/Delete
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
    public List<RoleActionPermission> Actions { get; set; }
}
public class RoleActionPermission
{
    public Guid ActionId { get; set; }
    public string ActionName { get; set; }
    public string ActionKey { get; set; }
    public bool HasPermission { get; set; }
}
public class UserPermissionResponse
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; }
    public List<UserMenuPermission> Menus { get; set; }
}

public class UserMenuPermission
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; }
    public List<UserSubMenuPermission> SubMenus { get; set; }
}

public class UserSubMenuPermission
{
    public Guid SubMenuId { get; set; }
    public string SubMenuName { get; set; }
    public List<UserActionPermission> Actions { get; set; }
}

public class UserActionPermission
{
    public Guid ActionId { get; set; }
    public string ActionName { get; set; }
    public string ActionKey { get; set; }
    public bool HasPermission { get; set; }
}