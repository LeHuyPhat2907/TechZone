using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

public class SecurityAuditLog : BaseEntity, IAggregateRoot
{
    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("userEmail")]
    public string? UserEmail { get; set; }

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // e.g. "UpdateRole", "CancelOrder", "LoginFailed"

    [BsonElement("module")]
    public string Module { get; set; } = string.Empty; // e.g. "Auth", "Order", "Inventory"

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("details")]
    public string? Details { get; set; }
}
