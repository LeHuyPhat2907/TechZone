namespace TechZone.Application.DTOs.Notification;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Order"; // Order, Promotion, System
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}
