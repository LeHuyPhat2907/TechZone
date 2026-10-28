using TechZone.Application.DTOs.Notification;
using TechZone.Application.DTOs.Order;

namespace TechZone.Application.Common.Interfaces;

public interface IAppNotificationService
{
    Task NotifyNewOrderPlacedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyOrderStatusChangedAsync(string? userId, string orderCode, string newStatus, string message, CancellationToken cancellationToken = default);
    Task<List<NotificationDto>> GetUserNotificationsAsync(string? userId, int limit = 10, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string? userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string? userId, CancellationToken cancellationToken = default);
}
