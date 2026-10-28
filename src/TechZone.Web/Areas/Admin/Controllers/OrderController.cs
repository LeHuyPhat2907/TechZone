using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Order;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

public class OrderController : AdminBaseController
{
    private readonly IOrderService _orderService;
    private readonly IAppNotificationService _notificationService;
    private readonly IEmailService _emailService;

    public OrderController(
        IOrderService orderService,
        IAppNotificationService notificationService,
        IEmailService emailService)
    {
        _orderService = orderService;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    /// <summary>
    /// 1.3. Quản lý Đơn hàng & Giao vận (Orders & Fulfillment)
    /// order.view_all: Xem toàn bộ đơn hàng trên hệ thống (không bị giới hạn theo chi nhánh/phân vùng).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "orders",
        int page = 1,
        OrderStatus? status = null,
        string? search = null,
        string? channel = null,
        string? refundStatus = null)
    {
        var pagedOrders = await _orderService.GetOrdersForAdminAsync(page, 15, status, search, channel, refundStatus);
        var metrics = await _orderService.GetDashboardMetricsAsync();
        var refundOrders = await _orderService.GetRefundRequestsAsync();
        var shippingCarriers = await _orderService.GetShippingCarriersAsync();
        var availableProducts = await _orderService.GetProductsForManualOrderAsync();

        var model = new OrderManagementViewModel
        {
            Orders = pagedOrders,
            CurrentStatus = status,
            CurrentSearch = search,
            CurrentChannel = channel,
            CurrentRefundStatus = refundStatus,
            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "orders" : tab.ToLowerInvariant(),
            TotalOrders = metrics.TotalOrders,
            TotalRevenue = metrics.TotalRevenue,
            PendingOrders = metrics.PendingOrdersCount,
            ConfirmedOrders = metrics.ConfirmedOrdersCount,
            DeliveredOrders = metrics.DeliveredOrdersCount,
            CancelledOrders = metrics.CancelledOrdersCount,
            RefundRequestedOrders = refundOrders.Count(r => r.RefundStatus == "Requested"),
            RefundOrders = refundOrders,
            ShippingCarriers = shippingCarriers,
            AvailableProducts = availableProducts
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        ViewBag.ShippingCarriers = await _orderService.GetShippingCarriersAsync();
        return View(order);
    }

    /// <summary>
    /// order.create_manual: Tạo đơn hàng thủ công (hỗ trợ đặt hàng qua điện thoại/fanpage/zalo).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateManual()
    {
        var products = await _orderService.GetProductsForManualOrderAsync();
        var carriers = await _orderService.GetShippingCarriersAsync();
        ViewBag.AvailableProducts = products;
        ViewBag.ShippingCarriers = carriers;
        return View(new ManualOrderCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManual(ManualOrderCreateDto dto)
    {
        // Enforce RBAC: Kỹ thuật viên không có thẩm quyền tạo đơn thương mại
        if (User.IsInRole("Technician"))
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên không có thẩm quyền tạo đơn hàng thương mại theo phân quyền RBAC.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.CustomerPhone))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ họ tên và số điện thoại khách hàng.";
            return RedirectToAction(nameof(Index), new { tab = "create_manual" });
        }

        if (dto.Items == null || dto.Items.Count == 0 || dto.Items.All(i => string.IsNullOrEmpty(i.ProductId)))
        {
            TempData["ErrorMessage"] = "Đơn hàng phải chứa ít nhất một sản phẩm hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab = "create_manual" });
        }

        // Clean empty items
        dto.Items = dto.Items.Where(i => !string.IsNullOrEmpty(i.ProductId) && i.Quantity > 0).ToList();

        var createdBy = User.Identity?.Name ?? "TelesalesStaff";
        var result = await _orderService.CreateManualOrderAsync(dto, createdBy);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "create_manual" });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = result.Data!.Id });
    }

    /// <summary>
    /// order.update_status: Thay đổi trạng thái đơn (Chờ xử lý -> Đã xác nhận -> Đang đóng gói -> Đang giao -> Hoàn tất).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string orderId, OrderStatus newStatus, string? note)
    {
        if (User.IsInRole("Technician"))
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên không có thẩm quyền thay đổi trạng thái đơn hàng thương mại theo đặc tả RBAC.";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        var changedBy = User.Identity?.Name ?? "Admin";
        var result = await _orderService.UpdateOrderStatusAsync(
            orderId,
            newStatus,
            string.IsNullOrWhiteSpace(note) ? $"Chuyển trạng thái sang {newStatus}" : note.Trim(),
            changedBy);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã cập nhật đơn hàng sang trạng thái '{newStatus}'.";

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                _ = _notificationService.NotifyOrderStatusChangedAsync(
                    order.UserId,
                    order.OrderCode,
                    newStatus.ToString(),
                    $"Đơn hàng #{order.OrderCode} đã được chuyển sang trạng thái: '{newStatus}'.");
                _ = _emailService.SendOrderStatusUpdateEmailAsync(order, "Trước đó", newStatus.ToString());
            }
        }

        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    /// <summary>
    /// order.cancel: Hủy đơn hàng và tự động hoàn trả tồn kho khả dụng.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(string orderId, string reason)
    {
        if (User.IsInRole("Technician"))
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên không có quyền can thiệp hủy đơn hàng thương mại.";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        var cancelledBy = User.Identity?.Name ?? "Admin";
        var finalReason = string.IsNullOrWhiteSpace(reason) ? "Khách hàng yêu cầu hủy đơn hoặc không thể liên lạc" : reason.Trim();

        var result = await _orderService.CancelOrderAsync(orderId, finalReason, cancelledBy);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    /// <summary>
    /// order.force_delete: Xóa vết đơn ảo / đơn rác vĩnh viễn khỏi CSDL (Dành cho Quản trị viên cấp cao SuperAdmin).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceDelete(string orderId, string? reason)
    {
        // Enforce RBAC: Chỉ SuperAdmin mới được quyền xóa vĩnh viễn đơn hàng
        if (!User.IsInRole("SuperAdmin"))
        {
            TempData["ErrorMessage"] = "Thao tác xóa vĩnh viễn (force_delete) yêu cầu quyền SuperAdmin. Bạn không đủ thẩm quyền xóa dữ liệu đơn hàng khỏi hệ thống.";
            return RedirectToAction(nameof(Index));
        }

        var deletedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SuperAdmin";
        var deletedByEmail = User.FindFirstValue(ClaimTypes.Email) ?? (User.Identity?.Name ?? "superadmin@techzone.vn");
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var result = await _orderService.ForceDeleteOrderAsync(orderId, deletedById, deletedByEmail, ipAddress);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// refund.approve: Phê duyệt yêu cầu đổi trả hoặc hoàn tiền cho khách.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRefund(string orderId, decimal refundAmount, string reason, string refundMethod, bool restockInventory = true)
    {
        if (User.IsInRole("Technician"))
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên không có thẩm quyền phê duyệt hoàn tiền đơn hàng thương mại.";
            return RedirectToAction(nameof(Index), new { tab = "refunds" });
        }

        var approvedBy = User.Identity?.Name ?? "Admin";
        var finalReason = string.IsNullOrWhiteSpace(reason) ? "Khách hàng đổi trả sản phẩm đúng quy định bảo hành" : reason.Trim();
        var finalMethod = string.IsNullOrWhiteSpace(refundMethod) ? "BankTransfer" : refundMethod.Trim();

        var result = await _orderService.ApproveRefundAsync(orderId, refundAmount, finalReason, finalMethod, approvedBy, restockInventory);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "refunds" });
    }

    /// <summary>
    /// refund.approve: Từ chối yêu cầu đổi trả / hoàn tiền.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRefund(string orderId, string reason)
    {
        if (User.IsInRole("Technician"))
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên không có thẩm quyền xử lý đổi trả/hoàn tiền đơn hàng thương mại.";
            return RedirectToAction(nameof(Index), new { tab = "refunds" });
        }

        var rejectedBy = User.Identity?.Name ?? "Admin";
        var finalReason = string.IsNullOrWhiteSpace(reason) ? "Sản phẩm vi phạm chính sách bảo hành / đổi trả" : reason.Trim();

        var result = await _orderService.RejectRefundAsync(orderId, finalReason, rejectedBy);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "refunds" });
    }

    /// <summary>
    /// refund.approve: Tạo yêu cầu đổi trả / hoàn tiền từ trang chi tiết đơn.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestRefund(string orderId, decimal amount, string reason)
    {
        var requestedBy = User.Identity?.Name ?? "Admin";
        var finalReason = string.IsNullOrWhiteSpace(reason) ? "Tiếp nhận yêu cầu hoàn tiền từ khách hàng" : reason.Trim();

        var result = await _orderService.RequestRefundAsync(orderId, amount, finalReason, requestedBy);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    /// <summary>
    /// shipping.manage: Cập nhật cấu hình kết nối API & biểu phí đơn vị vận chuyển.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingCarrier(ShippingCarrierConfig config)
    {
        if (User.IsInRole("Technician"))
        {
            TempData["ErrorMessage"] = "Bạn không có thẩm quyền thay đổi cấu hình đối tác vận chuyển.";
            return RedirectToAction(nameof(Index), new { tab = "shipping" });
        }

        var result = await _orderService.UpdateShippingCarrierConfigAsync(config);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "shipping" });
    }

    /// <summary>
    /// shipping.manage: Kiểm tra kết nối API sandbox/live tới đối tác vận chuyển.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TestCarrierConnection(string carrierCode)
    {
        if (string.IsNullOrWhiteSpace(carrierCode))
        {
            return Json(new { success = false, message = "Mã đơn vị vận chuyển không hợp lệ." });
        }

        var result = await _orderService.TestCarrierConnectionAsync(carrierCode.Trim().ToUpperInvariant());
        return Json(new { success = result.Succeeded, message = result.Succeeded ? result.Data : result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignCarrier(string orderId, string carrierName, string? trackingNumber)
    {
        var changedBy = User.Identity?.Name ?? "Admin";

        var finalTrackingNumber = !string.IsNullOrWhiteSpace(trackingNumber)
            ? trackingNumber.Trim().ToUpper()
            : carrierName switch
            {
                var c when c.Contains("GHTK") => $"S22.{Random.Shared.Next(10000000, 99999999)}",
                var c when c.Contains("Viettel") => $"VT{Random.Shared.Next(100000000, 999999999)}VN",
                var c when c.Contains("Grab") => $"GRB{Random.Shared.Next(10000000, 99999999)}",
                _ => $"GHN{Random.Shared.Next(10000000, 99999999)}"
            };

        var result = await _orderService.AssignCarrierAndTrackingAsync(orderId, carrierName, finalTrackingNumber, changedBy);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã tạo vận đơn thành công qua đơn vị '{carrierName}'. Mã vận đơn đối soát: {finalTrackingNumber}.";
        }

        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueVatInvoice(string orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null) return NotFound();

        if (!order.RequireVatInvoice)
        {
            TempData["ErrorMessage"] = "Đơn hàng này không yêu cầu xuất hóa đơn VAT điện tử.";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        _ = _emailService.SendVatInvoiceEmailAsync(order);

        TempData["SuccessMessage"] = $"Đã phát hành Hóa đơn VAT điện tử thành công cho công ty {order.CompanyName} (MST: {order.TaxId}). HĐĐT đã được gửi tới email {order.InvoiceEmail}.";
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }
}
