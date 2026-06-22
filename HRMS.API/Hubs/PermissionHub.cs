using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HRMS.API.Hubs;

public class PermissionHub : Hub
{
    public const string RoleUpdatedEvent = "RoleUpdated";

    // Join group for a specific role within an organization
    public async Task JoinRole(string organizationId, string roleId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(roleId))
            throw new HubException("OrganizationId and RoleId are required.");

        var groupName = GetRoleGroupName(organizationId, roleId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public static string GetRoleGroupName(string organizationId, string roleId)
    {
        return $"role_{organizationId.Trim()}_{roleId.Trim()}";
    }

}
