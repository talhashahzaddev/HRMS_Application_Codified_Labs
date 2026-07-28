using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HRMS.API.Hubs;

public class PermissionHub : Hub
{
    public const string RoleUpdatedEvent = "RoleUpdated";
public const string EmployeeUpdatedEvent = "EmployeeUpdated";

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

    // Join group for a specific employee within an organization
    public async Task JoinEmployee(string organizationId, string userId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(userId))
            throw new HubException("OrganizationId and EmployeeId are required.");

        var groupName = GetEmployeeGroupName(organizationId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public static string GetEmployeeGroupName(string organizationId, string? userId)
    {
        return $"employee_{organizationId.Trim()}_{userId?.Trim()}";
    }
}
