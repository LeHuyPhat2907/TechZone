using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class Review : BaseEntity, IAggregateRoot
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string? VariantSku { get; set; }

    [BsonElement("variantName")]
    public string? VariantName { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("userName")]
    public string UserName { get; set; } = string.Empty;

    [BsonElement("userPhone")]
    public string? UserPhone { get; set; }

    [BsonElement("userAvatar")]
    public string? UserAvatar { get; set; }

    [BsonElement("orderId")]
    public string? OrderId { get; set; }

    [BsonElement("rating")]
    public int Rating { get; set; } = 5; // 1 to 5 stars

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("imageUrls")]
    public List<string> ImageUrls { get; set; } = new();

    [BsonElement("isVerifiedBuyer")]
    public bool IsVerifiedBuyer { get; set; } = true;

    [BsonElement("isApproved")]
    public bool IsApproved { get; set; } = true;

    [BsonElement("helpfulVotes")]
    public int HelpfulVotes { get; set; } = 0;

    [BsonElement("staffReply")]
    public string? StaffReply { get; set; }

    [BsonElement("staffReplyAt")]
    public DateTime? StaffReplyAt { get; set; }

    [BsonElement("staffReplyByName")]
    public string? StaffReplyByName { get; set; }
}
