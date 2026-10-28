using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;

namespace TechZone.Web.Controllers;

public class NotificationController : Controller
{
    private readonly IAppNotificationService _notificationService;

    public NotificationController(IAppNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private string? CurrentUserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        : null;

    [HttpGet]
    public async Task<IActionResult> GetRecent(int limit = 8)
    {
        var items = await _notificationService.GetUserNotificationsAsync(CurrentUserId, limit);
        var unreadCount = await _notificationService.GetUnreadCountAsync(CurrentUserId);

        return Json(new
        {
            unreadCount,
            items
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            await _notificationService.MarkAsReadAsync(id);
        }
        return Json(new { succeeded = true });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(CurrentUserId);
        return Json(new { succeeded = true });
    }
}
