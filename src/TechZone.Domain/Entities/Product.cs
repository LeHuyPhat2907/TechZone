using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class Product : BaseEntity, IAggregateRoot
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [BsonElement("brandId")]
    public string BrandId { get; set; } = string.Empty;

    [BsonElement("brandName")]
    public string BrandName { get; set; } = string.Empty;

    [BsonElement("shortDescription")]
    public string? ShortDescription { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [BsonElement("images")]
    public List<ProductImage> Images { get; set; } = new();

    [BsonElement("originalPrice")]
    public decimal OriginalPrice { get; set; }

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("wholesalePrice")]
    public decimal? WholesalePrice { get; set; } // Giá bán buôn/sỉ theo RBAC product.price_override

    [BsonElement("videoUrl")]
    public string? VideoUrl { get; set; } // Video unbox / đánh giá sản phẩm (YouTube URL / MP4)

    [BsonElement("discountRate")]
    public int DiscountRate => OriginalPrice > Price && OriginalPrice > 0
        ? (int)Math.Round((OriginalPrice - Price) / OriginalPrice * 100)
        : 0;

    [BsonElement("warrantyMonths")]
    public int WarrantyMonths { get; set; } = 12; // 12, 24 months

    [BsonElement("hasVariants")]
    public bool HasVariants { get; set; } = false;

    [BsonElement("variants")]
    public List<ProductVariant> Variants { get; set; } = new();

    [BsonElement("specifications")]
    public List<SpecificationGroup> Specifications { get; set; } = new();

    [BsonElement("highlightSpecs")]
    public List<string> HighlightSpecs { get; set; } = new(); // Pills shown at top of PDP (e.g., "OLED 6.7 inch", "A17 Pro", "Camera 48MP")

    [BsonElement("stockQuantity")]
    public int StockQuantity { get; set; } = 0; // Total physical on-hand

    [BsonElement("availableQuantity")]
    public int AvailableQuantity { get; set; } = 0; // StockQuantity - ReservedQuantity

    [BsonElement("reservedQuantity")]
    public int ReservedQuantity { get; set; } = 0;

    [BsonElement("minStockThreshold")]
    public int MinStockThreshold { get; set; } = 5; // Mức tồn an toàn tối thiểu (Safety Stock cảnh báo)

    [BsonElement("ratingAverage")]
    public double RatingAverage { get; set; } = 5.0;

    [BsonElement("ratingCount")]
    public int RatingCount { get; set; } = 0;

    [BsonElement("viewCount")]
    public int ViewCount { get; set; } = 0;

    [BsonElement("soldCount")]
    public int SoldCount { get; set; } = 0;

    [BsonElement("isFeatured")]
    public bool IsFeatured { get; set; } = false;

    [BsonElement("isNewArrival")]
    public bool IsNewArrival { get; set; } = true;

    [BsonElement("isBestSeller")]
    public bool IsBestSeller { get; set; } = false;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class ProductVariant
{
    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("variantName")]
    public string VariantName { get; set; } = string.Empty; // e.g. "Titan Tự Nhiên - 256GB"

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("colorHex")]
    public string? ColorHex { get; set; }

    [BsonElement("storage")]
    public string? Storage { get; set; } // 128GB, 256GB, 512GB, 1TB

    [BsonElement("ram")]
    public string? Ram { get; set; } // 8GB, 16GB, 32GB

    [BsonElement("originalPrice")]
    public decimal OriginalPrice { get; set; }

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("stockQuantity")]
    public int StockQuantity { get; set; } = 0;

    [BsonElement("availableQuantity")]
    public int AvailableQuantity { get; set; } = 0;

    [BsonElement("imageUrl")]
    public string? ImageUrl { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

[BsonIgnoreExtraElements]
public class ProductImage
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("isThumbnail")]
    public bool IsThumbnail { get; set; } = false;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; } = 0;

    [BsonElement("colorTag")]
    public string? ColorTag { get; set; } // Link photo to specific color variant
}

[BsonIgnoreExtraElements]
public class SpecificationGroup
{
    [BsonElement("groupName")]
    public string GroupName { get; set; } = string.Empty; // e.g., "Màn hình", "Hiệu năng & Phần cứng", "Camera"

    [BsonElement("items")]
    public List<SpecificationItem> Items { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class SpecificationItem
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty; // e.g., "Kích thước màn hình"

    [BsonElement("value")]
    public string Value { get; set; } = string.Empty; // e.g., "6.7 inch Super Retina XDR"
}
