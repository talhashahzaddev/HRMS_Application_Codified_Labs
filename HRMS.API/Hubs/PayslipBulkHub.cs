using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HRMS.API.Hubs;

[Authorize]
public class PayslipBulkHub : Hub
{
    public const string ProgressEventName = "PayslipBulkProgress";

    public async Task JoinJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new HubException("Job id is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, jobId.Trim());
    }

    public async Task LeaveJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId.Trim());
    }
}
