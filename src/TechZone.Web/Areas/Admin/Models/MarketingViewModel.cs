using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class MarketingViewModel
{
    public List<Coupon> Coupons { get; set; } = new();
    public List<User> Customers { get; set; } = new();
    public List<Banner> Banners { get; set; } = new();
    public List<Banner> SeoCampaigns { get; set; } = new();

    // KPIs
    public int TotalCoupons { get; set; }
    public int ActiveCoupons { get; set; }
    public int TotalVoucherUses { get; set; }
    public int TotalFlashSaleCoupons { get; set; }

    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int VipCustomers { get; set; }

    public int TotalBanners { get; set; }
    public int ActiveBanners { get; set; }
    public int TotalSeoCampaigns { get; set; }

    // Navigation & Filter
    public string ActiveTab { get; set; } = "coupons"; // "coupons", "banners", "seo", "customers"
    public string? SearchQuery { get; set; }
    public bool? IsActiveFilter { get; set; }
    public string? CouponTypeFilter { get; set; } = "All"; // All, Order, Shipping, FlashSale, Combo
    public BannerPosition? BannerPositionFilter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalFilteredCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalFilteredCount / PageSize);
}

public class CreateCouponInputModel
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CouponScope Scope { get; set; } = CouponScope.Order;
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderValue { get; set; } = 0;
    public int UsageLimit { get; set; } = 100;
    public int UsageLimitPerUser { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);
    
    // Flash Sale & Combo Promotions
    public bool IsFlashSale { get; set; } = false;
    public string? FlashSaleSlot { get; set; }
    public bool IsComboPromotion { get; set; } = false;
    public string? ApplicableCategory { get; set; } = "Tất cả";
    public int MinItemQuantity { get; set; } = 1;
}

public class EditCouponInputModel
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CouponScope Scope { get; set; } = CouponScope.Order;
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderValue { get; set; } = 0;
    public int UsageLimit { get; set; } = 100;
    public int UsageLimitPerUser { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    public bool IsFlashSale { get; set; } = false;
    public string? FlashSaleSlot { get; set; }
    public bool IsComboPromotion { get; set; } = false;
    public string? ApplicableCategory { get; set; } = "Tất cả";
    public int MinItemQuantity { get; set; } = 1;
}

public class CreateBannerInputModel
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public BannerPosition Position { get; set; } = BannerPosition.HomeMainSlider;
    public int DisplayOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class EditBannerInputModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public BannerPosition Position { get; set; } = BannerPosition.HomeMainSlider;
    public int DisplayOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SeoCampaignInputModel
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventSlug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // SEO Meta Tags
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgImageUrl { get; set; }
    public string? HtmlContent { get; set; }
    public bool IsActive { get; set; } = true;
}
