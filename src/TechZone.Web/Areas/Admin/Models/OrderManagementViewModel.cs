using TechZone.Application.Common.Models;
using TechZone.Application.DTOs.Order;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class OrderManagementViewModel
{
    public PagedResult<OrderDto> Orders { get; set; } = new(new List<OrderDto>(), 0, 1, 20);
    public OrderStatus? CurrentStatus { get; set; }
    public string? CurrentSearch { get; set; }
    public string? CurrentChannel { get; set; }
    public string? CurrentRefundStatus { get; set; }
    public string ActiveTab { get; set; } = "orders"; // "orders", "create_manual", "refunds", "shipping"

    // KPI Metrics for order fulfillment
    public long TotalOrders { get; set; }
    public long PendingOrders { get; set; }
    public long ConfirmedOrders { get; set; }
    public long PreparingOrders { get; set; }
    public long ShippingOrders { get; set; }
    public long DeliveredOrders { get; set; }
    public long CancelledOrders { get; set; }
    public long RefundRequestedOrders { get; set; }
    public decimal TotalRevenue { get; set; }

    // Refund Management list
    public List<OrderDto> RefundOrders { get; set; } = new();

    // Shipping Carrier Integrations
    public List<ShippingCarrierConfig> ShippingCarriers { get; set; } = new();

    // Catalog products for manual order creation
    public List<ManualOrderProductOptionDto> AvailableProducts { get; set; } = new();

    // Input DTO for manual creation
    public ManualOrderCreateDto NewManualOrder { get; set; } = new();
}
