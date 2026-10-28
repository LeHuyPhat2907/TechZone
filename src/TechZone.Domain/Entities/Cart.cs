using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

public class Cart : BaseEntity, IAggregateRoot
{
    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("guestSessionId")]
    public string? GuestSessionId { get; set; }

    [BsonElement("items")]
    public List<CartItem> Items { get; set; } = new();

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonIgnore]
    public int TotalItems => Items.Sum(i => i.Quantity);

    [BsonIgnore]
    public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
}

public class CartItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("productSlug")]
    public string ProductSlug { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("variantName")]
    public string VariantName { get; set; } = string.Empty;

    [BsonElement("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("originalPrice")]
    public decimal OriginalPrice { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("warrantyMonths")]
    public int WarrantyMonths { get; set; } = 12;
}
