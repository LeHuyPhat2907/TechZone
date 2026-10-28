using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.DTOs.Cart;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;

namespace TechZone.Web.Controllers;

[IgnoreAntiforgeryToken]
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IRepository<Coupon> _couponRepo;

    public CartController(ICartService cartService, IRepository<Coupon> couponRepo)
    {
        _cartService = cartService;
        _couponRepo = couponRepo;
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

    public async Task<IActionResult> Index()
    {
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.GetCartAsync(userId, guestSessionId);
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
    {
        if (string.IsNullOrEmpty(dto.ProductId))
        {
            return Json(new { succeeded = false, message = "Thiếu thông tin sản phẩm" });
        }

        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.AddToCartAsync(userId, guestSessionId, dto);

        return Json(new
        {
            succeeded = true,
            totalItems = cart.TotalItems,
            subtotal = cart.Subtotal,
            message = "Đã thêm vào giỏ hàng"
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemQuantityDto dto)
    {
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.UpdateQuantityAsync(userId, guestSessionId, dto.VariantSku, dto.Quantity);

        return Json(new
        {
            succeeded = true,
            totalItems = cart.TotalItems,
            subtotal = cart.Subtotal.ToString("N0") + "₫",
            estimatedTotal = cart.EstimatedTotal.ToString("N0") + "₫"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] UpdateCartItemQuantityDto dto)
    {
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.RemoveItemAsync(userId, guestSessionId, dto.VariantSku);

        return Json(new
        {
            succeeded = true,
            totalItems = cart.TotalItems,
            subtotal = cart.Subtotal.ToString("N0") + "₫",
            estimatedTotal = cart.EstimatedTotal.ToString("N0") + "₫"
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.GetCartAsync(userId, guestSessionId);
        return Json(new { count = cart.TotalItems });
    }

    /// <summary>
    /// 6.2 coupon.simulate: Nhập thử mã voucher công khai để kiểm tra điều kiện giảm giá đơn hàng
    /// Hoạt động cho cả khách vãng lai (Guest Session) và thành viên.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SimulateCoupon([FromBody] SimulateCouponDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            return Json(new { succeeded = false, message = "Vui lòng nhập mã giảm giá cần kiểm tra." });
        }

        var normalized = dto.CouponCode.Trim().ToUpper();
        var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
        var coupon = allCoupons.FirstOrDefault(c => c.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (coupon == null || !coupon.IsActive)
        {
            return Json(new { succeeded = false, message = $"Mã voucher '{normalized}' không tồn tại hoặc đã ngừng hoạt động." });
        }

        var now = DateTime.UtcNow;
        if (now < coupon.StartDate || now > coupon.EndDate)
        {
            return Json(new { succeeded = false, message = $"Mã voucher '{normalized}' chưa đến thời điểm áp dụng hoặc đã hết hạn." });
        }

        if (coupon.UsedCount >= coupon.UsageLimit)
        {
            return Json(new { succeeded = false, message = $"Mã voucher '{normalized}' đã hết lượt sử dụng trong đợt phát hành này." });
        }

        // Lấy giỏ hàng phiên hiện tại
        var (userId, guestSessionId) = GetUserOrGuestIdentity();
        var cart = await _cartService.GetCartAsync(userId, guestSessionId);

        if (cart.Items.Count == 0)
        {
            return Json(new { succeeded = false, message = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thử voucher." });
        }

        if (cart.Subtotal < coupon.MinOrderValue)
        {
            var deficit = coupon.MinOrderValue - cart.Subtotal;
            return Json(new
            {
                succeeded = false,
                message = $"Đơn hàng tối thiểu để dùng mã này là {coupon.MinOrderValue:N0}₫. Bạn cần mua thêm {deficit:N0}₫ để hưởng ưu đãi."
            });
        }

        decimal discount = 0;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            discount = cart.Subtotal * (coupon.DiscountValue / 100m);
            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            {
                discount = coupon.MaxDiscountAmount.Value;
            }
        }
        else
        {
            discount = Math.Min(cart.Subtotal, coupon.DiscountValue);
        }

        var newTotal = Math.Max(0, cart.Subtotal - discount + cart.EstimatedShippingFee);

        return Json(new
        {
            succeeded = true,
            code = coupon.Code,
            title = coupon.Title,
            discountAmount = discount,
            discountAmountText = $"{discount:N0}₫",
            subtotalText = $"{cart.Subtotal:N0}₫",
            newTotalText = $"{newTotal:N0}₫",
            message = $"Áp dụng thành công mã {coupon.Code}! Bạn được giảm {discount:N0}₫."
        });
    }
}

public class SimulateCouponDto
{
    public string CouponCode { get; set; } = string.Empty;
}
