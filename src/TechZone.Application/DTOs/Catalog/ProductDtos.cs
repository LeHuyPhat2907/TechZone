namespace TechZone.Application.DTOs.Catalog;

public class ProductCardDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public int DiscountRate => OriginalPrice > Price && OriginalPrice > 0
        ? (int)Math.Round((OriginalPrice - Price) / OriginalPrice * 100)
        : 0;
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public int AvailableQuantity { get; set; }
    public int SoldCount { get; set; }
    public bool InStock => AvailableQuantity > 0;
    public List<string> HighlightSpecs { get; set; } = new();
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public decimal InstallmentMonthlyPrice => Price > 0 ? Math.Round(Price / 12 / 1000) * 1000 : 0;
    public string InstallmentText => $"Trả góp 0% từ {InstallmentMonthlyPrice:N0}₫/th";
    public decimal SmemberPrice => Price > 0 ? Math.Round((Price * 0.985m) / 1000m) * 1000m : 0;
    public string SmemberPriceText => $"Smember: {SmemberPrice:N0}₫";
}

public class ProductDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string BrandId { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public int DiscountRate => OriginalPrice > Price && OriginalPrice > 0
        ? (int)Math.Round((OriginalPrice - Price) / OriginalPrice * 100)
        : 0;
    public int WarrantyMonths { get; set; } = 12;
    public int AvailableQuantity { get; set; }
    public bool InStock => AvailableQuantity > 0;
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public List<string> HighlightSpecs { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<SpecificationGroupDto> Specifications { get; set; } = new();
    public List<ProductCardDto> RelatedProducts { get; set; } = new();
    public List<ProductReviewItemDto> Reviews { get; set; } = new();
    public decimal InstallmentMonthlyPrice => Price > 0 ? Math.Round(Price / 12 / 1000) * 1000 : 0;
    public string InstallmentText => $"Trả góp 0% từ {InstallmentMonthlyPrice:N0}₫/tháng";
    public decimal SmemberPrice => Price > 0 ? Math.Round((Price * 0.985m) / 1000m) * 1000m : 0;
    public string SmemberPriceText => $"Smember: {SmemberPrice:N0}₫";
}

public class ProductVariantDto
{
    public string Sku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? ColorHex { get; set; }
    public string? Storage { get; set; }
    public string? Ram { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
    public string? ImageUrl { get; set; }
}

public class SpecificationGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public List<SpecificationItemDto> Items { get; set; } = new();
}

public class SpecificationItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ProductFilterDto
{
    public string? CategorySlug { get; set; }
    public string? BrandSlug { get; set; }
    public string? SearchKeyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public bool InStockOnly { get; set; } = false;
    public string? SortBy { get; set; } = "featured"; // featured, price_asc, price_desc, new, best_seller, rating_desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class QuickSearchResultDto
{
    public List<string> Suggestions { get; set; } = new();
    public List<ProductCardDto> Products { get; set; } = new();
}

public class ProductComparisonDto
{
    public ProductDetailDto Product1 { get; set; } = null!;
    public ProductDetailDto Product2 { get; set; } = null!;
    public List<ComparisonRowDto> Rows { get; set; } = new();
}

public class ComparisonRowDto
{
    public string GroupName { get; set; } = string.Empty;
    public string SpecName { get; set; } = string.Empty;
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
    public bool IsDifferent => !string.Equals(Value1?.Trim(), Value2?.Trim(), StringComparison.OrdinalIgnoreCase);
}

public class ProductReviewItemDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedBuyer { get; set; } = true;
    public string? StaffReply { get; set; }
    public DateTime? StaffReplyAt { get; set; }
}

public class SubmitReviewDto
{
    public string ProductId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
}

