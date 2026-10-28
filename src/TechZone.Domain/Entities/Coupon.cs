using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

public class Coupon : BaseEntity, IAggregateRoot
{
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("scope")]
    public CouponScope Scope { get; set; } = CouponScope.Order;

    [BsonElement("discountType")]
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

    [BsonElement("discountValue")]
    public decimal DiscountValue { get; set; }

    [BsonElement("maxDiscountAmount")]
    public decimal? MaxDiscountAmount { get; set; } // Giới hạn trần giảm giá cho loại %

    [BsonElement("minOrderValue")]
    public decimal MinOrderValue { get; set; } = 0;

    [BsonElement("usageLimit")]
    public int UsageLimit { get; set; } = 100;

    [BsonElement("usedCount")]
    public int UsedCount { get; set; } = 0;

    [BsonElement("usageLimitPerUser")]
    public int UsageLimitPerUser { get; set; } = 1;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isFlashSale")]
    public bool IsFlashSale { get; set; } = false;

    [BsonElement("flashSaleSlot")]
    public string? FlashSaleSlot { get; set; } // Khung giờ flash sale: "09:00 - 12:00", "12:00 - 15:00", "18:00 - 21:00", "20:00 - 24:00"

    [BsonElement("isComboPromotion")]
    public bool IsComboPromotion { get; set; } = false;

    [BsonElement("applicableCategory")]
    public string? ApplicableCategory { get; set; } // "Tất cả", "Điện thoại", "Laptop", "Phụ kiện", "Linh kiện"

    [BsonElement("minItemQuantity")]
    public int MinItemQuantity { get; set; } = 1;

    [BsonIgnore]
    public bool IsValidNow => IsActive && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && UsedCount < UsageLimit;
}

public class CouponUsage : BaseEntity, IAggregateRoot
{
    [BsonElement("couponId")]
    public string CouponId { get; set; } = string.Empty;

    [BsonElement("couponCode")]
    public string CouponCode { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("discountAmount")]
    public decimal DiscountAmount { get; set; }

    [BsonElement("usedAt")]
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
