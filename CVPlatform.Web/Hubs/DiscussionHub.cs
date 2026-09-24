using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CVPlatform.Web.Hubs;

[Authorize]
public class DiscussionHub : Hub
{
    public async Task JoinPositionGroup(int positionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(positionId));
    }

    public async Task LeavePositionGroup(int positionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(positionId));
    }

    public static string GroupName(int positionId) => $"position-{positionId}";
}