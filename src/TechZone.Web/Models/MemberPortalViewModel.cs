using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Models;

public class MemberPortalViewModel
{
    public string ActiveTab { get; set; } = "dashboard"; // "dashboard", "profile", "orders", "wishlist", "wallet", "warranty", "security"

    // User Profile
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string MembershipTier { get; set; } = "Bronze";
    public string MembershipTierName { get; set; } = "Đồng (Bronze)";
    public int RewardPoints { get; set; } = 0;
    public bool TwoFactorEnabled { get; set; } = false;
    public string TwoFactorMethod { get; set; } = "Email";
    public DateTime? DeleteRequestedAt { get; set; }

    // Sổ địa chỉ
    public List<UserAddress> Addresses { get; set; } = new();

    // 1. KPI Counts
    public int TotalOrdersCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ShippingOrdersCount { get; set; }
    public int DeliveredOrdersCount { get; set; }
    public decimal TotalSpentAmount { get; set; }
    public int SavedWishlistCount { get; set; }
    public int SavedCouponsCount { get; set; }
    public int ActiveWarrantiesCount { get; set; }

    // 2. Orders History & Detail
    public List<Order> OrdersList { get; set; } = new();
    public Order? SelectedOrder { get; set; }

    // 3. Wishlist Products
    public List<Product> WishlistProducts { get; set; } = new();

    // 4. Wallet Coupons
    public List<Coupon> AvailableCoupons { get; set; } = new();
    public List<string> SavedCouponCodes { get; set; } = new();

    // 5. Warranty & Service RMA
    public List<Warranty> MyWarranties { get; set; } = new();
    public List<RmaRequest> MyRmaRequests { get; set; } = new();
}

public class OrderTimelineStepDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
}
