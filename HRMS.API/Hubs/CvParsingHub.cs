using Microsoft.AspNetCore.SignalR;

namespace HRMS.API.Hubs;

public class CvParsingHub : Hub
{
    // Clients can join a group named after their jobApplyId to receive updates
    public async Task JoinApplicationGroup(string jobApplyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobApplyId);
    }

    public async Task LeaveApplicationGroup(string jobApplyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobApplyId);
    }
}
