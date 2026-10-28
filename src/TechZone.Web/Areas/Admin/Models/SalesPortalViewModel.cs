using TechZone.Application.DTOs.Order;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class SalesPortalViewModel
{
    public string ActiveTab { get; set; } = "orders"; // "orders", "crm", "catalog", "refund_return", "create_order"

    // Current Sales Operator Info
    public string CurrentStaffId { get; set; } = string.Empty;
    public string CurrentStaffName { get; set; } = "Nhân viên Bán hàng";

    // 1. KPI Operational Metrics
    public int PendingOrdersCount { get; set; }
    public int MyAssignedOrdersCount { get; set; }
    public int CallVerifiedCount { get; set; }
    public int TodayOrdersCount { get; set; }
    public int RefundRequestsCount { get; set; }
    public int ReturnInitiatedCount { get; set; }

    // 2. Section 4.1: Order Processing (order.view_list, order.view_detail, order.assign_staff, order.update_status, order.print_invoice)
    public List<Order> OrdersList { get; set; } = new();
    public List<User> SalesStaffList { get; set; } = new();
    public Order? SelectedOrder { get; set; }
    public string? SearchQuery { get; set; }
    public string? StatusFilter { get; set; }
    public string? ChannelFilter { get; set; }
    public string? StaffFilter { get; set; }
    public string? CallStatusFilter { get; set; }
    public bool OnlyMyOrders { get; set; } = false;

    // 3. Section 4.2: Customer Support & CRM (customer.view_basic, customer.contact_info, order.update_shipping_info, order.item_modify, order.internal_note)
    public CustomerCrmProfileDto? CustomerProfile { get; set; }
    public string? CustomerLookupQuery { get; set; }

    // 4. Section 4.3: Catalog & Stock Availability (product.view_catalog, inventory.check_available, promotion.apply_code, order.cancel, refund.request_create, return.initiate)
    public List<ProductSalesCatalogDto> CatalogProducts { get; set; } = new();
    public string? CatalogSearchQuery { get; set; }
    public string? CatalogCategoryFilter { get; set; }

    // 5. Section 4.4: Refunds & Return Requests
    public List<Order> RefundRequestsList { get; set; } = new();
    public List<Order> ReturnInitiatedList { get; set; } = new();

    // 6. Section 4.5: Manual Order Creation
    public List<Product> AvailableProducts { get; set; } = new();
    public List<Coupon> ActiveCoupons { get; set; } = new();
    public List<ShippingCarrierConfig> ShippingCarriers { get; set; } = new();
}

public class CustomerCrmProfileDto
{
    public string? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MembershipTier { get; set; } = "Bronze";
    public int RewardPoints { get; set; } = 0;
    public int TotalOrders { get; set; } = 0;
    public decimal TotalSpent { get; set; } = 0;
    public decimal TotalDebt { get; set; } = 0;
    public bool IsBanned { get; set; } = false;
    public string? BannedReason { get; set; }
    public List<UserAddress> Addresses { get; set; } = new();
    public List<Order> OrderHistory { get; set; } = new();
}

public class ProductSalesCatalogDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public int StockQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int MinStockThreshold { get; set; }
    public int WarrantyMonths { get; set; }
    public List<string> HighlightSpecs { get; set; } = new();
    public List<ProductVariantStockDto> Variants { get; set; } = new();
    public List<BranchStockInfoDto> BranchStock { get; set; } = new();
}

public class ProductVariantStockDto
{
    public string Sku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
}

public class BranchStockInfoDto
{
    public string BranchName { get; set; } = "Tổng Kho Tân Bình - TP.HCM";
    public int AvailableQuantity { get; set; }
    public string StatusText => AvailableQuantity > 0 ? "Còn hàng" : "Hết hàng";
}
