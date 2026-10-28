using TechZone.Domain.Enums;

namespace TechZone.Application.DTOs.Order;

public class CheckoutDto
{
    public string? UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public string? AppliedOrderCouponCode { get; set; }
    public string? AppliedShippingCouponCode { get; set; }
    public string? CustomerNotes { get; set; }

    // FPT Shop End-to-End Fulfillment & VAT Invoice Enhancements
    public string DeliveryType { get; set; } = "HomeDelivery"; // "HomeDelivery" or "StorePickup"
    public string? PickupStoreAddress { get; set; }
    public bool RequireVatInvoice { get; set; } = false;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? CompanyAddress { get; set; }
    public string? InvoiceEmail { get; set; }
    public bool IsStudent { get; set; } = false;
}

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;

    public List<OrderItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal OrderDiscountAmount { get; set; }
    public decimal ShippingDiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public OrderStatus OrderStatus { get; set; }

    public string CarrierName { get; set; } = "Giao Hàng Nhanh (GHN)";
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string DeliveryType { get; set; } = "HomeDelivery";
    public string? PickupStoreAddress { get; set; }
    public bool RequireVatInvoice { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? CompanyAddress { get; set; }
    public string? InvoiceEmail { get; set; }
    public string? CustomerNotes { get; set; }
    public string? AdminNotes { get; set; }
    public bool IsManualOrder { get; set; }
    public string SourceChannel { get; set; } = "Website";
    public string RefundStatus { get; set; } = "None";
    public decimal? RefundAmount { get; set; }
    public string? RefundReason { get; set; }
    public string? RefundMethod { get; set; }
    public string? RefundApprovedBy { get; set; }
    public DateTime? RefundApprovedAt { get; set; }
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
}

public class OrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public int WarrantyMonths { get; set; }
    public List<string> AssignedSerialNumbers { get; set; } = new();
}

public class OrderStatusHistoryDto
{
    public OrderStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class OrderTrackingDto
{
    public string OrderCode { get; set; } = string.Empty;
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public string MaskedAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CarrierName { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string DeliveryType { get; set; } = "HomeDelivery";
    public string? PickupStoreAddress { get; set; }
    public bool RequireVatInvoice { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    public List<OrderItemDto> Items { get; set; } = new();
}

public class AdminDashboardMetricsDto
{
    public long TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public long PendingOrdersCount { get; set; }
    public long ConfirmedOrdersCount { get; set; }
    public long DeliveredOrdersCount { get; set; }
    public long CancelledOrdersCount { get; set; }
    public long LowStockCount { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
}

public class ManualOrderCreateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string SourceChannel { get; set; } = "Telesales"; // Telesales, Fanpage, Zalo, DirectStore

    public string DeliveryType { get; set; } = "HomeDelivery"; // HomeDelivery, StorePickup
    public string? PickupStoreAddress { get; set; }
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;

    public List<ManualOrderItemDto> Items { get; set; } = new();

    public decimal ShippingFee { get; set; } = 30000m;
    public decimal OrderDiscountAmount { get; set; } = 0m;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string CarrierName { get; set; } = "Giao Hàng Nhanh (GHN)";
    public string? TrackingNumber { get; set; }

    public string? CustomerNotes { get; set; }
    public string? AdminNotes { get; set; }

    public bool RequireVatInvoice { get; set; } = false;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? CompanyAddress { get; set; }
    public string? InvoiceEmail { get; set; }
}

public class ManualOrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string? VariantSku { get; set; }
    public string? ProductName { get; set; }
    public string? VariantName { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public int WarrantyMonths { get; set; } = 12;
}

public class ManualOrderProductOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public List<ManualOrderVariantOptionDto> Variants { get; set; } = new();
}

public class ManualOrderVariantOptionDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}

