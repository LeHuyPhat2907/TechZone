using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Models;

namespace TechZone.Web.Controllers;

[Authorize]
public class MemberController : Controller
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Coupon> _couponRepo;
    private readonly IRepository<Warranty> _warrantyRepo;
    private readonly IRepository<RmaRequest> _rmaRepo;
    private readonly IRepository<Review> _reviewRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOrderService _orderService;

    public MemberController(
        IRepository<User> userRepo,
        IRepository<Order> orderRepo,
        IRepository<Product> productRepo,
        IRepository<Coupon> couponRepo,
        IRepository<Warranty> warrantyRepo,
        IRepository<RmaRequest> rmaRepo,
        IRepository<Review> reviewRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        IPasswordHasher passwordHasher,
        IOrderService orderService)
    {
        _userRepo = userRepo;
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _couponRepo = couponRepo;
        _warrantyRepo = warrantyRepo;
        _rmaRepo = rmaRepo;
        _reviewRepo = reviewRepo;
        _auditLogRepo = auditLogRepo;
        _passwordHasher = passwordHasher;
        _orderService = orderService;
    }

    private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "dashboard", string? orderCode = null, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        // Owner-Only: Lấy dữ liệu thuộc sở hữu của chính User này
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var userOrders = allOrders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();

        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var wishlistProducts = allProducts.Where(p => user.WishlistProductIds.Contains(p.Id)).ToList();

        var allWarranties = await _warrantyRepo.GetAllAsync(cancellationToken);
        var userWarranties = allWarranties.Where(w => w.CustomerId == userId || (!string.IsNullOrEmpty(user.PhoneNumber) && w.CustomerPhone == user.PhoneNumber)).OrderByDescending(w => w.StartDate).ToList();

        var allRmas = await _rmaRepo.GetAllAsync(cancellationToken);
        var userRmas = allRmas.Where(r => r.CustomerId == userId || (!string.IsNullOrEmpty(user.PhoneNumber) && r.CustomerPhone == user.PhoneNumber)).OrderByDescending(r => r.CreatedAt).ToList();

        var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
        var availableCoupons = allCoupons.Where(c => c.IsActive && c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow).ToList();

        Order? selectedOrder = null;
        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            selectedOrder = userOrders.FirstOrDefault(o => o.OrderCode.Equals(orderCode.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var model = new MemberPortalViewModel
        {
            ActiveTab = tab,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            DateOfBirth = user.DateOfBirth,
            MembershipTier = user.MembershipTier,
            MembershipTierName = user.MembershipTierName,
            RewardPoints = user.RewardPoints,
            TwoFactorEnabled = user.TwoFactorEnabled,
            TwoFactorMethod = user.TwoFactorMethod,
            DeleteRequestedAt = user.DeleteRequestedAt,

            Addresses = user.Addresses,

            TotalOrdersCount = userOrders.Count,
            PendingOrdersCount = userOrders.Count(o => o.OrderStatus == OrderStatus.Pending),
            ShippingOrdersCount = userOrders.Count(o => o.OrderStatus == OrderStatus.Shipping || o.OrderStatus == OrderStatus.Preparing),
            DeliveredOrdersCount = userOrders.Count(o => o.OrderStatus == OrderStatus.Delivered),
            TotalSpentAmount = userOrders.Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount),
            SavedWishlistCount = user.WishlistProductIds.Count,
            SavedCouponsCount = user.SavedCouponCodes.Count,
            ActiveWarrantiesCount = userWarranties.Count(w => w.IsActive),

            OrdersList = userOrders,
            SelectedOrder = selectedOrder,
            WishlistProducts = wishlistProducts,
            AvailableCoupons = availableCoupons,
            SavedCouponCodes = user.SavedCouponCodes,
            MyWarranties = userWarranties,
            MyRmaRequests = userRmas
        };

        return View(model);
    }

    // =========================================================================
    // 5.1 QUẢN LÝ HỒ SƠ & BẢO MẬT (PROFILE & SECURITY)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        string fullName,
        string? phoneNumber,
        DateTime? dateOfBirth,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        user.FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : user.FullName;
        user.PhoneNumber = phoneNumber?.Trim();
        user.DateOfBirth = dateOfBirth;
        user.AvatarUrl = !string.IsNullOrWhiteSpace(avatarUrl) ? avatarUrl.Trim() : user.AvatarUrl;

        await _userRepo.UpdateAsync(user, cancellationToken);
        await LogAuditAsync("ProfileUpdateOwn", "Member", $"Cập nhật hồ sơ cá nhân: {user.FullName}", cancellationToken);

        TempData["SuccessMessage"] = "Đã cập nhật thông tin hồ sơ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(
        string receiverName,
        string phoneNumber,
        string province,
        string district,
        string ward,
        string detailAddress,
        bool isDefault = false,
        string addressType = "Home",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        if (isDefault || !user.Addresses.Any())
        {
            foreach (var addr in user.Addresses)
            {
                addr.IsDefault = false;
            }
        }

        var newAddress = new UserAddress
        {
            ReceiverName = receiverName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Province = province.Trim(),
            District = district.Trim(),
            Ward = ward.Trim(),
            DetailAddress = detailAddress.Trim(),
            IsDefault = isDefault || !user.Addresses.Any(),
            AddressType = addressType
        };

        user.Addresses.Add(newAddress);
        await _userRepo.UpdateAsync(user, cancellationToken);

        TempData["SuccessMessage"] = "Đã thêm địa chỉ giao hàng mới vào sổ địa chỉ cá nhân.";
        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(string addressId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        var addr = user.Addresses.FirstOrDefault(a => a.Id == addressId);
        if (addr != null)
        {
            user.Addresses.Remove(addr);
            if (addr.IsDefault && user.Addresses.Any())
            {
                user.Addresses.First().IsDefault = true;
            }
            await _userRepo.UpdateAsync(user, cancellationToken);
            TempData["SuccessMessage"] = "Đã xóa địa chỉ khỏi sổ địa chỉ.";
        }

        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(string addressId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        foreach (var addr in user.Addresses)
        {
            addr.IsDefault = (addr.Id == addressId);
        }

        await _userRepo.UpdateAsync(user, cancellationToken);
        TempData["SuccessMessage"] = "Đã đặt địa chỉ mặc định thành công.";
        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        string currentPassword,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới phải có tối thiểu 6 ký tự.";
            return RedirectToAction(nameof(Index), new { tab = "security" });
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "Xác nhận mật khẩu mới không trùng khớp.";
            return RedirectToAction(nameof(Index), new { tab = "security" });
        }

        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác.";
            return RedirectToAction(nameof(Index), new { tab = "security" });
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _userRepo.UpdateAsync(user, cancellationToken);

        await LogAuditAsync("SecurityChangePassword", "Member", "Người dùng tự đổi mật khẩu tài khoản cá nhân", cancellationToken);

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Tài khoản của bạn đã được bảo vệ.";
        return RedirectToAction(nameof(Index), new { tab = "security" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle2FA(bool enable, string method = "Email", CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        user.TwoFactorEnabled = enable;
        user.TwoFactorMethod = method;
        await _userRepo.UpdateAsync(user, cancellationToken);

        await LogAuditAsync("SecurityChangePassword", "Member", $"Thay đổi 2FA: {(enable ? "Bật" : "Tắt")} qua {method}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(enable ? "bật" : "tắt")} thành công tính năng xác thực 2 lớp (2FA) qua {method}.";
        return RedirectToAction(nameof(Index), new { tab = "security" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDeleteAccount(string reason, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        user.DeleteRequestedAt = DateTime.UtcNow;
        user.DeleteRequestReason = reason;
        await _userRepo.UpdateAsync(user, cancellationToken);

        await LogAuditAsync("AccountDeleteRequest", "Member", $"Gửi yêu cầu xóa tài khoản: {reason}", cancellationToken);

        TempData["WarningMessage"] = "Hệ thống đã ghi nhận yêu cầu xóa/vô hiệu hóa tài khoản cá nhân của bạn. Ban Quản Trị sẽ xử lý trong 7 ngày làm việc.";
        return RedirectToAction(nameof(Index), new { tab = "security" });
    }

    // =========================================================================
    // 5.2 MUA SẮM, ĐẶT HÀNG & THANH TOÁN (SHOPPING, WISHLIST, WALLET, POINTS)
    // =========================================================================

    [HttpPost]
    public async Task<IActionResult> ToggleWishlist([FromBody] string productId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để lưu sản phẩm yêu thích." });
        }

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

        bool isSaved;
        if (user.WishlistProductIds.Contains(productId))
        {
            user.WishlistProductIds.Remove(productId);
            isSaved = false;
        }
        else
        {
            user.WishlistProductIds.Add(productId);
            isSaved = true;
        }

        await _userRepo.UpdateAsync(user, cancellationToken);
        return Json(new { success = true, isSaved, count = user.WishlistProductIds.Count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCoupon(string couponCode, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        var normalized = couponCode.Trim().ToUpper();
        var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
        var coupon = allCoupons.FirstOrDefault(c => c.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (coupon == null || !coupon.IsActive || DateTime.UtcNow > coupon.EndDate)
        {
            TempData["ErrorMessage"] = "Mã voucher không tồn tại hoặc đã hết hạn áp dụng.";
            return RedirectToAction(nameof(Index), new { tab = "wallet" });
        }

        if (!user.SavedCouponCodes.Contains(normalized))
        {
            user.SavedCouponCodes.Add(normalized);
            await _userRepo.UpdateAsync(user, cancellationToken);
            TempData["SuccessMessage"] = $"Đã lưu mã giảm giá {normalized} vào Ví voucher cá nhân!";
        }
        else
        {
            TempData["WarningMessage"] = $"Mã voucher {normalized} đã có sẵn trong Ví voucher của bạn.";
        }

        return RedirectToAction(nameof(Index), new { tab = "wallet" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPointsToOrder(string orderId, int pointsToUse, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);

        if (user == null || order == null)
        {
            return NotFound();
        }

        // RBAC Owner-Only Check (Chống IDOR)
        if (order.UserId != userId)
        {
            return Forbid();
        }

        if (order.PaymentStatus == PaymentStatus.Completed || order.OrderStatus != OrderStatus.Pending)
        {
            TempData["ErrorMessage"] = "Đơn hàng đã thanh toán hoặc đã qua bước tiếp nhận, không thể cấn trừ điểm tích lũy.";
            return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
        }

        if (pointsToUse <= 0 || pointsToUse > user.RewardPoints)
        {
            TempData["ErrorMessage"] = "Số điểm tích lũy sử dụng không hợp lệ hoặc vượt quá số dư hiện có.";
            return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
        }

        // Tỷ lệ quy đổi: 1 điểm = 1.000 VNĐ
        var discountFromPoints = pointsToUse * 1000m;
        if (discountFromPoints > order.TotalAmount)
        {
            discountFromPoints = order.TotalAmount;
            pointsToUse = (int)Math.Ceiling(discountFromPoints / 1000m);
        }

        user.RewardPoints -= pointsToUse;
        order.OrderDiscountAmount += discountFromPoints;
        order.TotalAmount = Math.Max(0, order.TotalAmount - discountFromPoints);

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"Khách hàng dùng {pointsToUse} Xu TechZone, cấn trừ {discountFromPoints:N0} VNĐ vào đơn hàng"
        });

        await _userRepo.UpdateAsync(user, cancellationToken);
        await _orderRepo.UpdateAsync(order, cancellationToken);

        TempData["SuccessMessage"] = $"Đã áp dụng thành công {pointsToUse} điểm tích lũy, giảm trừ {discountFromPoints:N0} VNĐ vào đơn hàng {order.OrderCode}.";
        return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
    }

    // =========================================================================
    // 5.3 THEO DÕI ĐƠN HÀNG, ĐỔI TRẢ & BẢO HÀNH (ORDER, WARRANTY, RETURN, REVIEW)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(string orderId, string reason, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);

        // RBAC Owner-Only Check (Chống IDOR)
        if (order == null || order.UserId != userId)
        {
            return Forbid();
        }

        // Khách hàng chỉ được tự hủy khi đơn còn Chờ xác nhận (chưa đóng gói / chuẩn bị)
        if (order.OrderStatus != OrderStatus.Pending)
        {
            TempData["ErrorMessage"] = "Đơn hàng của bạn đã được xác nhận và chuyển sang khâu đóng gói/vận chuyển, không thể tự hủy. Vui lòng liên hệ Hotline 1900 8888 để được hỗ trợ.";
            return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
        }

        var result = await _orderService.CancelOrderAsync(orderId, $"Khách hàng tự hủy đơn: {reason}", User.Identity?.Name ?? "Customer", cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            await LogAuditAsync("OrderCancelOwn", "Member", $"Khách tự hủy đơn {order.OrderCode}: {reason}", cancellationToken);
            TempData["SuccessMessage"] = $"Đã hủy đơn hàng {order.OrderCode} thành công.";
        }

        return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReceived(string orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);

        if (user == null || order == null)
        {
            return NotFound();
        }

        // RBAC Owner-Only Check (Chống IDOR)
        if (order.UserId != userId)
        {
            return Forbid();
        }

        if (order.OrderStatus != OrderStatus.Shipping)
        {
            TempData["ErrorMessage"] = "Chỉ có thể xác nhận đã nhận hàng khi đơn hàng đang ở trạng thái 'Đang giao hàng'.";
            return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
        }

        order.OrderStatus = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        order.PaymentStatus = PaymentStatus.Completed;

        // Tích điểm thưởng thành viên: 1 điểm cho mỗi 100.000 VNĐ chi tiêu
        var pointsEarned = (int)Math.Floor(order.TotalAmount / 100000m);
        if (pointsEarned > 0)
        {
            user.RewardPoints += pointsEarned;
            await _userRepo.UpdateAsync(user, cancellationToken);
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Delivered,
            Note = $"Khách hàng xác nhận đã nhận máy nguyên vẹn seal. Tích lũy +{pointsEarned} điểm thưởng."
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderConfirmReceived", "Member", $"Khách xác nhận nhận đơn {order.OrderCode}, tích +{pointsEarned} điểm", cancellationToken);

        TempData["SuccessMessage"] = $"Xác nhận đã nhận máy thành công! Bạn nhận được +{pointsEarned} điểm thưởng TechZone và thời hạn bảo hành điện tử chính thức bắt đầu kích hoạt.";
        return RedirectToAction(nameof(Index), new { tab = "orders", orderCode = order.OrderCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReturn(
        string orderId,
        string sku,
        string serialNumber,
        string reason,
        string? evidenceVideoUrl,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);

        if (user == null || order == null)
        {
            return NotFound();
        }

        // RBAC Owner-Only Check (Chống IDOR)
        if (order.UserId != userId)
        {
            return Forbid();
        }

        order.ReturnInitiationStatus = "Requested";
        order.ReturnInitiationReason = reason;
        order.ReturnInitiationNotes = description;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"Khách hàng tạo yêu cầu đổi trả / hoàn tiền: {reason}. Bằng chứng: {evidenceVideoUrl}"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);

        // Tạo Ticket RMA tiếp nhận tại Hub Kỹ thuật viên
        var item = order.Items.FirstOrDefault(i => i.VariantSku.Equals(sku, StringComparison.OrdinalIgnoreCase)) ?? order.Items.FirstOrDefault();
        var rma = new RmaRequest
        {
            RmaCode = $"RMA-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            ProductId = item?.ProductId ?? "",
            ProductName = item?.ProductName ?? "Thiết bị đổi trả",
            VariantSku = item?.VariantSku ?? "",
            SerialNumber = !string.IsNullOrWhiteSpace(serialNumber) ? serialNumber : (item?.AssignedSerialNumbers.FirstOrDefault() ?? "CHỜ_ĐỐI_SOÁT"),
            CustomerId = user.Id,
            CustomerName = user.FullName,
            CustomerPhone = user.PhoneNumber ?? order.CustomerPhone,
            CustomerDescription = $"{reason} | Chi tiết: {description}",
            PhysicalConditionNotes = "Khách hàng tạo yêu cầu trực tuyến kèm video bóc hộp",
            EvidenceMediaUrls = !string.IsNullOrWhiteSpace(evidenceVideoUrl) ? new List<string> { evidenceVideoUrl } : new List<string>(),
            Status = RmaStatus.Submitted,
            Priority = "Normal"
        };

        await _rmaRepo.AddAsync(rma, cancellationToken);
        await LogAuditAsync("ReturnRequestCreate", "Member", $"Khách tạo yêu cầu đổi trả đơn {order.OrderCode} -> RMA {rma.RmaCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã gửi yêu cầu đổi trả/hoàn tiền và tải lên bằng chứng thành công! Mã phiếu bảo hành tiếp nhận: {rma.RmaCode}. Bộ phận Kỹ thuật & CSKH sẽ phản hồi trong 24 giờ.";
        return RedirectToAction(nameof(Index), new { tab = "warranty" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitVerifiedReview(
        string productId,
        string? orderId,
        int rating,
        string content,
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);

        if (user == null || product == null) return NotFound();

        // RBAC GIỚI HẠN BẢO MẬT: Chỉ cho phép đánh giá nếu khách ĐÃ MUA sản phẩm và đơn hàng ĐÃ GIAO THÀNH CÔNG (Verified Buyer Only)
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var isVerified = allOrders.Any(o => 
            o.UserId == userId && 
            o.OrderStatus == OrderStatus.Delivered && 
            o.Items.Any(i => i.ProductId == productId));

        if (!isVerified)
        {
            TempData["ErrorMessage"] = "Bạn chỉ có thể viết đánh giá cho sản phẩm sau khi đã mua và nhận hàng thành công (Verified Purchase Only).";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        var review = new Review
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UserId = user.Id,
            UserName = user.FullName,
            UserPhone = user.PhoneNumber,
            UserAvatar = user.AvatarUrl,
            OrderId = orderId,
            Rating = Math.Clamp(rating, 1, 5),
            Content = content.Trim(),
            ImageUrls = !string.IsNullOrWhiteSpace(imageUrl) ? new List<string> { imageUrl } : new List<string>(),
            IsVerifiedBuyer = true,
            IsApproved = true
        };

        await _reviewRepo.AddAsync(review, cancellationToken);

        // Update product rating
        var allProductReviews = (await _reviewRepo.GetAllAsync(cancellationToken)).Where(r => r.ProductId == product.Id && r.IsApproved).ToList();
        product.RatingCount = allProductReviews.Count;
        product.RatingAverage = allProductReviews.Any() ? Math.Round(allProductReviews.Average(r => r.Rating), 1) : 5.0;
        await _productRepo.UpdateAsync(product, cancellationToken);

        await LogAuditAsync("ReviewCreateVerified", "Member", $"Đánh giá {rating} sao cho {product.Name} (Verified Buyer)", cancellationToken);

        TempData["SuccessMessage"] = $"Đánh giá của bạn ({rating} sao) cho {product.Name} đã được ghi nhận thành công!";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    private async Task LogAuditAsync(string action, string module, string details, CancellationToken cancellationToken)
    {
        try
        {
            var audit = new SecurityAuditLog
            {
                UserId = GetCurrentUserId(),
                UserEmail = User.Identity?.Name,
                Action = action,
                Module = module,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                Details = details
            };
            await _auditLogRepo.AddAsync(audit, cancellationToken);
        }
        catch
        {
            // Non-blocking
        }
    }
}
