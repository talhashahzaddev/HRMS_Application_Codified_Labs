using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HRMS.API.Hubs;

[Authorize]
public sealed class PermissionHub : Hub
{
    public const string RoleUpdatedEvent = "RoleUpdated";
    public const string EmployeeUpdatedEvent = "EmployeeUpdated";

    // Join group for a specific role within an organization
    public Task JoinRole(string organizationId, string roleId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(roleId))
            throw new HubException("OrganizationId and RoleId are required.");

        return Groups.AddToGroupAsync(Context.ConnectionId, GetRoleGroupName(organizationId, roleId));
    }

    // Join group for a specific employee within an organization
    public Task JoinEmployee(string organizationId, string userId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(userId))
            throw new HubException("OrganizationId and EmployeeId are required.");

        return Groups.AddToGroupAsync(Context.ConnectionId, GetEmployeeGroupName(organizationId, userId));
    }

    public static string GetRoleGroupName(string organizationId, string roleId) =>
        $"role_{organizationId.Trim()}_{roleId.Trim()}";

    public static string GetEmployeeGroupName(string organizationId, string? userId) =>
        $"employee_{organizationId.Trim()}_{userId?.Trim()}";
}
