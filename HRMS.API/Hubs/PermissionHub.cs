using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HRMS.API.Hubs;

[Authorize]
public sealed class PermissionHub : Hub
{
    public Task JoinRole(string organizationId, string roleId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(roleId))
        {
            throw new HubException("Organization and role are required.");
        }

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"role_{organizationId.Trim()}_{roleId.Trim()}");
    }

    public Task JoinEmployee(string organizationId, string employeeId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(employeeId))
        {
            throw new HubException("Organization and employee are required.");
        }

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"employee_{organizationId.Trim()}_{employeeId.Trim()}");
    }
}
