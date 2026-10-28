using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Order;
using TechZone.Application.Services;
using TechZone.Domain.Enums;
using TechZone.Web.Models;

namespace TechZone.Web.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IAuthService _authService;
    private readonly IAppNotificationService _notificationService;
    private readonly IEmailService _emailService;

    public CheckoutController(
        ICartService cartService,
        IOrderService orderService,
        IAuthService authService,
        IAppNotificationService notificationService,
        IEmailService emailService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _authService = authService;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    private (string? userId, string guestSessionId) GetUserOrGuestIdentity()
    {
        string? userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        string? guestSessionId = HttpContext.Session.GetString("GuestSessionId");
        if (string.IsNullOrEmpty(guestSessionId))
        {
            guestSessionId = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("GuestSessionId", guestSessionId);
        }

        return (userId, guestSessionId);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.GetCartAsync(userId, guestSessionId);

        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var checkoutDto = new CheckoutDto
        {
            UserId = userId,
            PaymentMethod = PaymentMethod.COD
        };

        if (!string.IsNullOrEmpty(userId))
        {
            var profile = await _authService.GetUserProfileAsync(userId);
            if (profile != null)
            {
                checkoutDto.CustomerName = profile.FullName;
                checkoutDto.CustomerEmail = profile.Email;
                checkoutDto.CustomerPhone = profile.PhoneNumber ?? string.Empty;

                var defaultAddr = profile.Addresses.FirstOrDefault(a => a.IsDefault) ?? profile.Addresses.FirstOrDefault();
                if (defaultAddr != null)
                {
                    checkoutDto.Province = defaultAddr.Province;
                    checkoutDto.District = defaultAddr.District;
                    checkoutDto.Ward = defaultAddr.Ward;
                    checkoutDto.DetailAddress = defaultAddr.DetailAddress;
                }
            }
        }

        var viewModel = new CheckoutViewModel
        {
            Cart = cart,
            Checkout = checkoutDto
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.GetCartAsync(userId, guestSessionId);

        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        bool isStorePickup = model.Checkout.DeliveryType == "StorePickup";
        if (string.IsNullOrWhiteSpace(model.Checkout.CustomerName) ||
            string.IsNullOrWhiteSpace(model.Checkout.CustomerPhone) ||
            (!isStorePickup && (string.IsNullOrWhiteSpace(model.Checkout.DetailAddress) || string.IsNullOrWhiteSpace(model.Checkout.Province))) ||
            (isStorePickup && string.IsNullOrWhiteSpace(model.Checkout.PickupStoreAddress)))
        {
            ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin nhận hàng.");
            model.Cart = cart;
            return View("Index", model);
        }

        if (model.Checkout.RequireVatInvoice && (string.IsNullOrWhiteSpace(model.Checkout.CompanyName) || string.IsNullOrWhiteSpace(model.Checkout.TaxId)))
        {
            ModelState.AddModelError("", "Vui lòng nhập Tên công ty và Mã số thuế nếu yêu cầu xuất hóa đơn VAT.");
            model.Cart = cart;
            return View("Index", model);
        }

        model.Checkout.UserId = userId;
        var result = await _orderService.CheckoutAsync(model.Checkout, cart);

        if (!result.Succeeded || result.Data == null)
        {
            ModelState.AddModelError("", result.Message);
            model.Cart = cart;
            return View("Index", model);
        }

        // Real-Time SignalR Notification & Transactional Email (Task 14)
        _ = _notificationService.NotifyNewOrderPlacedAsync(result.Data);
        _ = _emailService.SendOrderConfirmationEmailAsync(result.Data);

        return RedirectToAction("OrderConfirmation", new { orderCode = result.Data.OrderCode });
    }

    [HttpGet]
    public async Task<IActionResult> OrderConfirmation(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return RedirectToAction("Index", "Home");
        }

        var order = await _orderService.GetOrderByCodeAsync(orderCode);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulatePaymentSuccess(string orderCode, PaymentMethod gateway)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return RedirectToAction("Index", "Home");
        }

        var transactionId = $"{gateway.ToString().ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var result = await _orderService.ConfirmPaymentAsync(orderCode, transactionId, gateway);

        TempData["PaymentNotification"] = result.Succeeded
            ? $"Thanh toán thành công qua cổng {gateway}! Mã giao dịch đối soát: {transactionId}"
            : result.Message;

        return RedirectToAction("OrderConfirmation", new { orderCode });
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCallback(string orderCode, PaymentMethod gateway = PaymentMethod.VnPay, string? vnp_ResponseCode = "00", string? vnp_TransactionNo = null)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return RedirectToAction("Index", "Home");
        }

        // VNPAY standard response code "00" = Giao dịch thành công
        if (vnp_ResponseCode == "00" || string.IsNullOrEmpty(vnp_ResponseCode))
        {
            var transactionId = !string.IsNullOrEmpty(vnp_TransactionNo)
                ? vnp_TransactionNo
                : $"{gateway.ToString().ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            await _orderService.ConfirmPaymentAsync(orderCode, transactionId, gateway);
            TempData["PaymentNotification"] = $"Cổng thanh toán {gateway} phản hồi: Giao dịch thanh toán thành công!";
        }
        else
        {
            TempData["PaymentError"] = $"Giao dịch qua cổng {gateway} không thành công. Mã phản hồi lỗi: {vnp_ResponseCode}";
        }

        return RedirectToAction("OrderConfirmation", new { orderCode });
    }
}
