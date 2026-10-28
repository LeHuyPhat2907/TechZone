using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace TechZone.Infrastructure.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Add to personal user group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            // If user is Staff or Admin, add to "Admins" broadcast group
            if (user.IsInRole("SuperAdmin") || 
                user.IsInRole("SalesStaff") || 
                user.IsInRole("WarehouseStaff") || 
                user.IsInRole("Technician") ||
                user.IsInRole("Accountant"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
