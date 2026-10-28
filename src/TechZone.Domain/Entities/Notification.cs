using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

public class Notification : BaseEntity, IAggregateRoot
{
    [BsonElement("userId")]
    public string? UserId { get; set; } // Null nếu thông báo dành cho tất cả Admin / Staff

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "Order"; // Order, Promotion, System, InventoryAlert

    [BsonElement("targetUrl")]
    public string? TargetUrl { get; set; }

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }
}
