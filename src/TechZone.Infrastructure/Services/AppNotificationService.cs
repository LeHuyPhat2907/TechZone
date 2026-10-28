using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Notification;
using TechZone.Application.DTOs.Order;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;
using TechZone.Infrastructure.Hubs;

namespace TechZone.Infrastructure.Services;

public class AppNotificationService : IAppNotificationService
{
    private readonly IRepository<Notification> _notificationRepo;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AppNotificationService> _logger;

    public AppNotificationService(
        IRepository<Notification> notificationRepo,
        IHubContext<NotificationHub> hubContext,
        ILogger<AppNotificationService> logger)
    {
        _notificationRepo = notificationRepo;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewOrderPlacedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                UserId = null, // null = Broadcast to all Admins
                Title = $"Đơn hàng mới: #{order.OrderCode}",
                Message = $"Khách hàng {order.CustomerName} vừa đặt đơn trị giá {order.TotalAmount:N0}₫ ({order.PaymentMethod})",
                Type = "Order",
                TargetUrl = $"/Admin/Order/Detail?id={order.Id}",
                IsRead = false
            };

            await _notificationRepo.AddAsync(notification, cancellationToken);

            var dto = MapToDto(notification);

            // Broadcast real-time to Admins group
            await _hubContext.Clients.Group("Admins").SendAsync("NewOrderNotification", dto, cancellationToken);
            _logger.LogInformation("SignalR real-time notification sent for new order #{OrderCode}", order.OrderCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification for order #{OrderCode}", order.OrderCode);
        }
    }

    public async Task NotifyOrderStatusChangedAsync(string? userId, string orderCode, string newStatus, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = $"Cập nhật đơn hàng #{orderCode}",
                Message = message,
                Type = "Order",
                TargetUrl = $"/Order/Track?orderCode={orderCode}",
                IsRead = false
            };

            await _notificationRepo.AddAsync(notification, cancellationToken);

            var dto = MapToDto(notification);

            if (!string.IsNullOrEmpty(userId))
            {
                // Send to specific user
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("OrderStatusUpdated", dto, cancellationToken);
            }

            // Also notify Admins
            await _hubContext.Clients.Group("Admins").SendAsync("OrderStatusUpdated", dto, cancellationToken);
            _logger.LogInformation("SignalR notification sent for order #{OrderCode} status change to {Status}", orderCode, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status update notification for order #{OrderCode}", orderCode);
        }
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string? userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepo.FindAsync(
            n => n.UserId == userId || n.UserId == null,
            cancellationToken);

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<int> GetUnreadCountAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var count = await _notificationRepo.CountAsync(
            n => (!n.IsRead) && (n.UserId == userId || n.UserId == null),
            cancellationToken);

        return (int)count;
    }

    public async Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var notif = await _notificationRepo.GetByIdAsync(notificationId, cancellationToken);
        if (notif != null && !notif.IsRead)
        {
            notif.IsRead = true;
            notif.ReadAt = DateTime.UtcNow;
            await _notificationRepo.UpdateAsync(notif, cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var unreadList = await _notificationRepo.FindAsync(
            n => (!n.IsRead) && (n.UserId == userId || n.UserId == null),
            cancellationToken);

        foreach (var notif in unreadList)
        {
            notif.IsRead = true;
            notif.ReadAt = DateTime.UtcNow;
            await _notificationRepo.UpdateAsync(notif, cancellationToken);
        }
    }

    private static NotificationDto MapToDto(Notification entity)
    {
        return new NotificationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            Message = entity.Message,
            Type = entity.Type,
            TargetUrl = entity.TargetUrl,
            IsRead = entity.IsRead,
            CreatedAt = entity.CreatedAt,
            TimeAgo = FormatTimeAgo(entity.CreatedAt)
        };
    }

    private static string FormatTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "Vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} ngày trước";
        return dateTime.ToString("dd/MM/yyyy");
    }
}
