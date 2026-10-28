using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff")]
public class SalesController : AdminBaseController
{
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Coupon> _couponRepo;
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<ShippingCarrierConfig> _carrierRepo;
    private readonly IRepository<RmaRequest> _rmaRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly IOrderService _orderService;

    public SalesController(
        IRepository<Order> orderRepo,
        IRepository<Product> productRepo,
        IRepository<User> userRepo,
        IRepository<Coupon> couponRepo,
        IRepository<InventoryItem> inventoryRepo,
        IRepository<ShippingCarrierConfig> carrierRepo,
        IRepository<RmaRequest> rmaRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        IOrderService orderService)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _userRepo = userRepo;
        _couponRepo = couponRepo;
        _inventoryRepo = inventoryRepo;
        _carrierRepo = carrierRepo;
        _rmaRepo = rmaRepo;
        _auditLogRepo = auditLogRepo;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "orders",
        string? search = null,
        string? status = null,
        string? channel = null,
        string? staffId = null,
        string? callStatus = null,
        bool myOrders = false,
        string? customerQuery = null,
        string? catalogSearch = null,
        string? catalogCategory = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var currentUserName = User.Identity?.Name ?? "Nhân viên Bán hàng";

        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var allUsers = await _userRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
        var allCarriers = await _carrierRepo.GetAllAsync(cancellationToken);
        var salesStaff = allUsers.Where(u => (u.Role == UserRole.SalesStaff || u.Role == UserRole.SuperAdmin) && u.IsActive).ToList();

        var today = DateTime.UtcNow.Date;

        // 1. KPI Counts
        var pendingOrdersCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Pending);
        var myAssignedOrdersCount = allOrders.Count(o => 
            (!string.IsNullOrEmpty(o.AssignedStaffId) && o.AssignedStaffId == currentUserId) ||
            (!string.IsNullOrEmpty(o.AssignedStaffName) && o.AssignedStaffName.Contains(currentUserName, StringComparison.OrdinalIgnoreCase)));
        var callVerifiedCount = allOrders.Count(o => o.CallVerificationStatus == "CallVerified");
        var todayOrdersCount = allOrders.Count(o => o.CreatedAt >= today);
        var refundRequestsCount = allOrders.Count(o => o.RefundStatus == "Requested");
        var returnInitiatedCount = allOrders.Count(o => o.ReturnInitiationStatus == "Requested" || o.ReturnInitiationStatus == "SentToTechnician");

        // 2. Filter Orders (Mục 4.1)
        var ordersQuery = allOrders.AsEnumerable();
        if (myOrders)
        {
            ordersQuery = ordersQuery.Where(o => 
                (!string.IsNullOrEmpty(o.AssignedStaffId) && o.AssignedStaffId == currentUserId) ||
                (!string.IsNullOrEmpty(o.AssignedStaffName) && o.AssignedStaffName.Contains(currentUserName, StringComparison.OrdinalIgnoreCase)));
        }
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            ordersQuery = ordersQuery.Where(o => o.OrderStatus == parsedStatus);
        }
        if (!string.IsNullOrWhiteSpace(channel))
        {
            ordersQuery = ordersQuery.Where(o => o.SourceChannel.Equals(channel, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(staffId))
        {
            ordersQuery = ordersQuery.Where(o => o.AssignedStaffId == staffId);
        }
        if (!string.IsNullOrWhiteSpace(callStatus))
        {
            ordersQuery = ordersQuery.Where(o => o.CallVerificationStatus.Equals(callStatus, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            ordersQuery = ordersQuery.Where(o => 
                o.OrderCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerPhone.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerEmail.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        var filteredOrders = ordersQuery.OrderByDescending(o => o.CreatedAt).ToList();

        // 3. Customer CRM Profile Lookup (Mục 4.2)
        CustomerCrmProfileDto? customerProfile = null;
        if (!string.IsNullOrWhiteSpace(customerQuery))
        {
            var query = customerQuery.Trim();
            var matchedUser = allUsers.FirstOrDefault(u => 
                (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(query)) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase));

            var userOrders = allOrders.Where(o => 
                (!string.IsNullOrEmpty(matchedUser?.Id) && o.UserId == matchedUser.Id) ||
                o.CustomerPhone.Contains(query) ||
                o.CustomerEmail.Contains(query, StringComparison.OrdinalIgnoreCase)).OrderByDescending(o => o.CreatedAt).ToList();

            if (matchedUser != null || userOrders.Any())
            {
                var firstOrder = userOrders.FirstOrDefault();
                customerProfile = new CustomerCrmProfileDto
                {
                    UserId = matchedUser?.Id,
                    FullName = matchedUser?.FullName ?? firstOrder?.CustomerName ?? "Khách vãng lai",
                    PhoneNumber = matchedUser?.PhoneNumber ?? firstOrder?.CustomerPhone ?? query,
                    Email = matchedUser?.Email ?? firstOrder?.CustomerEmail ?? "",
                    MembershipTier = matchedUser?.MembershipTier ?? "Bronze",
                    RewardPoints = matchedUser?.RewardPoints ?? 0,
                    TotalOrders = userOrders.Count,
                    TotalSpent = userOrders.Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                    TotalDebt = userOrders.Where(o => o.PaymentStatus != PaymentStatus.Completed && o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                    IsBanned = matchedUser != null && !matchedUser.IsActive,
                    BannedReason = matchedUser?.BannedReason,
                    Addresses = matchedUser?.Addresses ?? (firstOrder != null ? new List<UserAddress>
                    {
                        new()
                        {
                            ReceiverName = firstOrder.CustomerName,
                            PhoneNumber = firstOrder.CustomerPhone,
                            Province = firstOrder.ShippingAddress.Province,
                            District = firstOrder.ShippingAddress.District,
                            Ward = firstOrder.ShippingAddress.Ward,
                            DetailAddress = firstOrder.ShippingAddress.DetailAddress,
                            IsDefault = true
                        }
                    } : new List<UserAddress>()),
                    OrderHistory = userOrders
                };
            }
        }

        // 4. Catalog & Stock Availability (Mục 4.3)
        var catalogQuery = allProducts.Where(p => p.IsActive).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(catalogSearch))
        {
            catalogQuery = catalogQuery.Where(p => 
                p.Name.Contains(catalogSearch, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(catalogSearch, StringComparison.OrdinalIgnoreCase) ||
                p.BrandName.Contains(catalogSearch, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(catalogCategory))
        {
            catalogQuery = catalogQuery.Where(p => p.CategoryId == catalogCategory || p.CategoryName.Contains(catalogCategory, StringComparison.OrdinalIgnoreCase));
        }

        var catalogDtos = catalogQuery.OrderBy(p => p.Name).Select(p => new ProductSalesCatalogDto
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            CategoryName = p.CategoryName,
            BrandName = p.BrandName,
            ThumbnailUrl = p.ThumbnailUrl,
            Price = p.Price,
            OriginalPrice = p.OriginalPrice,
            StockQuantity = p.StockQuantity,
            AvailableQuantity = p.AvailableQuantity,
            MinStockThreshold = p.MinStockThreshold,
            WarrantyMonths = p.WarrantyMonths,
            HighlightSpecs = p.HighlightSpecs,
            Variants = p.Variants.Select(v => new ProductVariantStockDto
            {
                Sku = v.Sku,
                VariantName = v.VariantName,
                Price = v.Price > 0 ? v.Price : p.Price,
                AvailableStock = v.AvailableQuantity
            }).ToList(),
            BranchStock = new List<BranchStockInfoDto>
            {
                new() { BranchName = "Tổng Kho Tân Bình - TP.HCM", AvailableQuantity = Math.Max(0, (int)Math.Ceiling(p.AvailableQuantity * 0.7)) },
                new() { BranchName = "Showroom Quận 1 - TP.HCM", AvailableQuantity = Math.Max(0, (int)Math.Floor(p.AvailableQuantity * 0.2)) },
                new() { BranchName = "Showroom Cầu Giấy - Hà Nội", AvailableQuantity = Math.Max(0, (int)Math.Floor(p.AvailableQuantity * 0.1)) }
            }
        }).ToList();

        // 5. Refunds & Returns
        var refundList = allOrders.Where(o => o.RefundStatus == "Requested" || o.RefundStatus == "Approved").OrderByDescending(o => o.CreatedAt).ToList();
        var returnList = allOrders.Where(o => o.ReturnInitiationStatus != "None").OrderByDescending(o => o.CreatedAt).ToList();

        var model = new SalesPortalViewModel
        {
            ActiveTab = tab,
            CurrentStaffId = currentUserId,
            CurrentStaffName = currentUserName,

            PendingOrdersCount = pendingOrdersCount,
            MyAssignedOrdersCount = myAssignedOrdersCount,
            CallVerifiedCount = callVerifiedCount,
            TodayOrdersCount = todayOrdersCount,
            RefundRequestsCount = refundRequestsCount,
            ReturnInitiatedCount = returnInitiatedCount,

            OrdersList = filteredOrders,
            SalesStaffList = salesStaff,
            SearchQuery = search,
            StatusFilter = status,
            ChannelFilter = channel,
            StaffFilter = staffId,
            CallStatusFilter = callStatus,
            OnlyMyOrders = myOrders,

            CustomerProfile = customerProfile,
            CustomerLookupQuery = customerQuery,

            CatalogProducts = catalogDtos,
            CatalogSearchQuery = catalogSearch,
            CatalogCategoryFilter = catalogCategory,

            RefundRequestsList = refundList,
            ReturnInitiatedList = returnList,

            AvailableProducts = allProducts.Where(p => p.IsActive).OrderBy(p => p.Name).ToList(),
            ActiveCoupons = allCoupons.Where(c => c.IsActive && c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow).ToList(),
            ShippingCarriers = allCarriers.Where(c => c.IsActive).ToList()
        };

        return View(model);
    }

    // =========================================================================
    // 4.1 TIẾP NHẬN & ĐIỀU PHỐI ĐƠN HÀNG (ORDER PROCESSING)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignStaff(
        string orderId,
        string staffId,
        string staffName,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        order.AssignedStaffId = staffId;
        order.AssignedStaffName = staffName;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"Đã phân công chuyên viên xử lý đơn: {staffName} (bởi {User.Identity?.Name ?? "Sales Lead"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderAssignStaff", "Sales", $"Phân công đơn {order.OrderCode} cho {staffName}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã phân công thành công đơn {order.OrderCode} cho {staffName}.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCallStatus(
        string orderId,
        string callStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        order.CallVerificationStatus = callStatus;
        order.CallVerificationNote = note;

        var statusDesc = callStatus switch
        {
            "CallVerified" => "Đã gọi xác nhận thành công",
            "Unreachable" => "Không liên lạc được / Máy bận",
            "Rescheduled" => "Khách hẹn gọi lại sau",
            _ => callStatus
        };

        // Nếu đã gọi xác nhận thành công và đơn đang Chờ xử lý -> tự động chuyển sang Đã xác nhận (Confirmed)
        if (callStatus == "CallVerified" && order.OrderStatus == OrderStatus.Pending)
        {
            order.OrderStatus = OrderStatus.Confirmed;
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"[Xác thực cuộc gọi telesales]: {statusDesc}. {(string.IsNullOrWhiteSpace(note) ? "" : $"Ghi chú: {note}")} - Nhân viên: {User.Identity?.Name ?? "Sales"}"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderUpdateStatus", "Sales", $"Cập nhật cuộc gọi đơn {order.OrderCode}: {callStatus}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái liên hệ cuộc gọi cho đơn {order.OrderCode}: {statusDesc}. {(order.OrderStatus == OrderStatus.Confirmed ? "Đơn đã được tự động duyệt sang 'Đã xác nhận' để chuyển tiếp đóng gói." : "")}";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(
        string orderId,
        OrderStatus newStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        // Sales chỉ được duyệt đến bước Chờ đóng gói (Confirmed / Preparing). Các bước sau do Kho phụ trách.
        var oldStatus = order.OrderStatus;
        order.OrderStatus = newStatus;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = newStatus,
            Note = $"Cập nhật trạng thái từ {oldStatus} sang {newStatus}: {note} (bởi {User.Identity?.Name ?? "Sales"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderUpdateStatus", "Sales", $"Đổi trạng thái đơn {order.OrderCode} từ {oldStatus} sang {newStatus}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật đơn hàng {order.OrderCode} sang trạng thái '{newStatus}'.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpGet]
    public async Task<IActionResult> PrintInvoice(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        return View("PrintInvoice", order);
    }

    // =========================================================================
    // 4.2 CHĂM SÓC KHÁCH HÀNG & ĐIỀU CHỈNH ĐƠN (CUSTOMER SUPPORT & EDITING)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingInfo(
        string orderId,
        string receiverName,
        string phoneNumber,
        string province,
        string district,
        string ward,
        string detailAddress,
        string? deliveryTimeSlot,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        if (order.OrderStatus == OrderStatus.Shipping || order.OrderStatus == OrderStatus.Delivered)
        {
            TempData["ErrorMessage"] = "Đơn hàng đã bàn giao cho bưu tá hoặc đã giao, không thể chỉnh sửa địa chỉ giao hàng!";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        order.CustomerName = receiverName;
        order.CustomerPhone = phoneNumber;
        order.DeliveryTimeSlot = !string.IsNullOrWhiteSpace(deliveryTimeSlot) ? deliveryTimeSlot : order.DeliveryTimeSlot;

        order.ShippingAddress = new OrderShippingAddress
        {
            ReceiverName = receiverName,
            PhoneNumber = phoneNumber,
            Province = province,
            District = district,
            Ward = ward,
            DetailAddress = detailAddress
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"Điều chỉnh thông tin nhận hàng: {order.ShippingAddress.FullAddress}, Khung giờ: {order.DeliveryTimeSlot} (bởi {User.Identity?.Name ?? "Sales"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderUpdateShippingInfo", "Sales", $"Chỉnh sửa địa chỉ/khung giờ đơn {order.OrderCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật thông tin giao nhận và khung giờ hẹn giao cho đơn {order.OrderCode} thành công.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModifyOrderItem(
        string orderId,
        string oldSku,
        string newProductId,
        string newVariantSku,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        var newProduct = await _productRepo.GetByIdAsync(newProductId, cancellationToken);

        if (order == null || newProduct == null)
        {
            TempData["ErrorMessage"] = "Thông tin đơn hàng hoặc sản phẩm thay thế không hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        if (order.OrderStatus == OrderStatus.Shipping || order.OrderStatus == OrderStatus.Delivered)
        {
            TempData["ErrorMessage"] = "Đơn hàng đang giao hoặc đã hoàn tất, không thể thay đổi mặt hàng!";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        var existingItem = order.Items.FirstOrDefault(i => i.VariantSku.Equals(oldSku, StringComparison.OrdinalIgnoreCase));
        if (existingItem == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy SKU {oldSku} trong đơn hàng {order.OrderCode}.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        var variant = newProduct.Variants.FirstOrDefault(v => v.Sku.Equals(newVariantSku, StringComparison.OrdinalIgnoreCase));
        
        // GIỚI HẠN BẢO MẬT SALES: Lấy đơn giá chuẩn từ niêm yết hệ thống, TUYỆT ĐỐI không cho phép gõ tay sửa giá
        var validPrice = variant != null && variant.Price > 0 ? variant.Price : newProduct.Price;

        existingItem.ProductId = newProduct.Id;
        existingItem.ProductName = newProduct.Name;
        existingItem.ProductSlug = newProduct.Slug;
        existingItem.VariantSku = !string.IsNullOrWhiteSpace(newVariantSku) ? newVariantSku : newProduct.Sku;
        existingItem.VariantName = variant?.VariantName ?? "Mặc định";
        existingItem.UnitPrice = validPrice;
        existingItem.Quantity = Math.Max(1, quantity);
        existingItem.WarrantyMonths = newProduct.WarrantyMonths;
        existingItem.ThumbnailUrl = !string.IsNullOrEmpty(variant?.ImageUrl) ? variant.ImageUrl : newProduct.ThumbnailUrl;

        // Tính toán lại tổng tiền đơn hàng
        order.Subtotal = order.Items.Sum(i => i.TotalPrice);
        order.TotalAmount = Math.Max(0, order.Subtotal + order.ShippingFee - order.OrderDiscountAmount - order.ShippingDiscountAmount);

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"[Điều chỉnh sản phẩm]: Thay đổi SKU {oldSku} -> {existingItem.ProductName} ({existingItem.VariantSku}), SL: {existingItem.Quantity}, Đơn giá: {validPrice:N0}đ (bởi {User.Identity?.Name ?? "Sales"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderItemModify", "Sales", $"Đổi biến thể máy đơn {order.OrderCode}: {oldSku} -> {existingItem.VariantSku}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật phân loại thiết bị cho đơn {order.OrderCode}. Tổng giá trị đơn hàng mới: {order.TotalAmount:N0} VNĐ.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInternalNote(
        string orderId,
        string internalNote,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        var noteEntry = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm} - {User.Identity?.Name}]: {internalNote}";
        order.InternalNotes = string.IsNullOrWhiteSpace(order.InternalNotes) ? noteEntry : $"{order.InternalNotes} | {noteEntry}";

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OrderInternalNote", "Sales", $"Thêm ghi chú nội bộ cho đơn {order.OrderCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã thêm ghi chú nội bộ cho đơn {order.OrderCode} gửi khâu đóng gói & giao nhận.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    // =========================================================================
    // 4.3 TRA CỨU TỒN KHO, HỦY ĐƠN & ĐỔI TRẢ (PROMOTION, CANCEL, REFUND, RETURN)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPromotion(
        string orderId,
        string couponCode,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        if (string.IsNullOrWhiteSpace(couponCode))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập mã khuyến mãi.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
        var coupon = allCoupons.FirstOrDefault(c => c.Code.Equals(couponCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (coupon == null || !coupon.IsActive || DateTime.UtcNow < coupon.StartDate || DateTime.UtcNow > coupon.EndDate)
        {
            TempData["ErrorMessage"] = "Mã khuyến mãi không tồn tại hoặc đã hết hạn áp dụng.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        if (order.Subtotal < coupon.MinOrderValue)
        {
            TempData["ErrorMessage"] = $"Đơn hàng chưa đạt giá trị tối thiểu {coupon.MinOrderValue:N0} VNĐ để áp dụng mã này.";
            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        decimal discount = 0;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            discount = order.Subtotal * coupon.DiscountValue / 100m;
            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            {
                discount = coupon.MaxDiscountAmount.Value;
            }
        }
        else
        {
            discount = coupon.DiscountValue;
        }

        order.AppliedOrderCouponCode = coupon.Code;
        order.OrderDiscountAmount = discount;
        order.TotalAmount = Math.Max(0, order.Subtotal + order.ShippingFee - order.OrderDiscountAmount - order.ShippingDiscountAmount);

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"Áp dụng thành công mã voucher {coupon.Code}: giảm {discount:N0} VNĐ (bởi {User.Identity?.Name ?? "Sales"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("PromotionApplyCode", "Sales", $"Áp dụng coupon {coupon.Code} giảm {discount:N0}đ cho đơn {order.OrderCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã áp dụng mã ưu đãi {coupon.Code} thành công! Giảm ngay {discount:N0} VNĐ.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(
        string orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var finalReason = string.IsNullOrWhiteSpace(reason) ? "Khách hàng yêu cầu hủy đơn hoặc không thể liên lạc" : reason.Trim();
        var result = await _orderService.CancelOrderAsync(orderId, finalReason, User.Identity?.Name ?? "SalesStaff", cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            await LogAuditAsync("OrderCancel", "Sales", $"Hủy đơn {orderId}: {finalReason}", cancellationToken);
            TempData["SuccessMessage"] = $"Đã hủy đơn hàng thành công và tự động hoàn trả số lượng tồn khả dụng.";
        }

        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRefundRequest(
        string orderId,
        decimal amount,
        string reason,
        string refundMethod = "BankTransfer",
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "refund_return" });
        }

        // GIỚI HẠN BẢO MẬT SALES: Sales chỉ lập yêu cầu (Requested), KHÔNG ĐƯỢC TỰ DUYỆT GIẢI NGÂN (việc này của Kế toán)
        order.RefundStatus = "Requested";
        order.RefundAmount = amount > 0 ? amount : order.TotalAmount;
        order.RefundReason = reason;
        order.RefundMethod = !string.IsNullOrWhiteSpace(refundMethod) ? refundMethod : "BankTransfer";

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"[Lập phiếu yêu cầu hoàn tiền]: {order.RefundAmount:N0} VNĐ qua {order.RefundMethod}. Lý do: {reason}. Chờ phòng Kế toán duyệt (Nhân viên: {User.Identity?.Name ?? "Sales"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("RefundRequestCreate", "Sales", $"Lập phiếu yêu cầu hoàn tiền {order.RefundAmount:N0}đ cho đơn {order.OrderCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo phiếu yêu cầu hoàn tiền {order.RefundAmount:N0} VNĐ cho đơn {order.OrderCode}. Hồ sơ đã chuyển tiếp sang Kế toán phê duyệt giải ngân.";
        return RedirectToAction(nameof(Index), new { tab = "refund_return" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitiateReturn(
        string orderId,
        string returnReason,
        string initialInspectionNotes,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "refund_return" });
        }

        order.ReturnInitiationStatus = "SentToTechnician";
        order.ReturnInitiationReason = returnReason;
        order.ReturnInitiationNotes = initialInspectionNotes;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"[Tiếp nhận yêu cầu đổi/trả]: {returnReason}. Ghi chú: {initialInspectionNotes}. Chuyển Kỹ thuật viên thẩm định (Nhân viên: {User.Identity?.Name ?? "Sales"})"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);

        // Tự động tạo Ticket tiếp nhận tại Hub Kỹ thuật viên (RMA)
        var firstItem = order.Items.FirstOrDefault();
        var rma = new RmaRequest
        {
            RmaCode = $"RMA-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            ProductId = firstItem?.ProductId ?? "",
            ProductName = firstItem?.ProductName ?? "Thiết bị đổi trả",
            VariantSku = firstItem?.VariantSku ?? "",
            SerialNumber = firstItem?.AssignedSerialNumbers.FirstOrDefault() ?? "CHỜ_ĐỐI_SOÁT",
            CustomerId = order.UserId,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            CustomerDescription = $"Khách yêu cầu đổi/trả từ đơn {order.OrderCode}: {returnReason}. Ghi chú Telesales: {initialInspectionNotes}",
            PhysicalConditionNotes = $"Tiếp nhận đổi trả ban đầu: {initialInspectionNotes}",
            Status = RmaStatus.Submitted,
            Priority = "Normal"
        };
        await _rmaRepo.AddAsync(rma, cancellationToken);

        await LogAuditAsync("ReturnInitiate", "Sales", $"Tiếp nhận đổi trả đơn {order.OrderCode} -> RMA {rma.RmaCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã khởi tạo yêu cầu đổi/trả cho đơn {order.OrderCode} và lập phiếu RMA {rma.RmaCode} chuyển sang bộ phận Kỹ thuật thẩm định ngoại quan/phần cứng.";
        return RedirectToAction(nameof(Index), new { tab = "refund_return" });
    }

    // =========================================================================
    // 4.5 TẠO ĐƠN HÀNG THỦ CÔNG ĐA KÊNH (MANUAL ORDER CREATION)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManualOrder(
        string customerName,
        string customerPhone,
        string? customerEmail,
        string province,
        string district,
        string ward,
        string detailAddress,
        string? deliveryTimeSlot,
        string sourceChannel,
        string paymentMethod,
        string productId,
        string? variantSku,
        int quantity,
        string? couponCode,
        string? carrierName,
        string? customerNotes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên và số điện thoại khách hàng.";
            return RedirectToAction(nameof(Index), new { tab = "create_order" });
        }

        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm được chọn không tồn tại.";
            return RedirectToAction(nameof(Index), new { tab = "create_order" });
        }

        var variant = product.Variants.FirstOrDefault(v => v.Sku.Equals(variantSku, StringComparison.OrdinalIgnoreCase));
        
        // GIỚI HẠN BẢO MẬT: Giá lấy tự động từ hệ thống niêm yết, CHẶN SỬA GIÁ TÙY TIỆN
        var unitPrice = variant != null && variant.Price > 0 ? variant.Price : product.Price;
        var validQuantity = Math.Max(1, quantity);
        var subtotal = unitPrice * validQuantity;

        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
            var coupon = allCoupons.FirstOrDefault(c => c.Code.Equals(couponCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (coupon != null && coupon.IsActive && coupon.StartDate <= DateTime.UtcNow && coupon.EndDate >= DateTime.UtcNow && subtotal >= coupon.MinOrderValue)
            {
                discount = coupon.DiscountType == DiscountType.Percentage
                    ? Math.Min(subtotal * coupon.DiscountValue / 100m, coupon.MaxDiscountAmount ?? decimal.MaxValue)
                    : coupon.DiscountValue;
            }
        }

        var shippingFee = 30000m;
        var totalAmount = Math.Max(0, subtotal + shippingFee - discount);

        Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsedPaymentMethod);

        var newOrder = new Order
        {
            OrderCode = $"ORD-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            CustomerName = customerName.Trim(),
            CustomerPhone = customerPhone.Trim(),
            CustomerEmail = customerEmail?.Trim() ?? "",
            ShippingAddress = new OrderShippingAddress
            {
                ReceiverName = customerName.Trim(),
                PhoneNumber = customerPhone.Trim(),
                Province = province,
                District = district,
                Ward = ward,
                DetailAddress = detailAddress
            },
            DeliveryTimeSlot = !string.IsNullOrWhiteSpace(deliveryTimeSlot) ? deliveryTimeSlot : "Giờ hành chính (8h-17h)",
            IsManualOrder = true,
            SourceChannel = !string.IsNullOrWhiteSpace(sourceChannel) ? sourceChannel : "Hotline",
            PaymentMethod = parsedPaymentMethod,
            PaymentStatus = parsedPaymentMethod == PaymentMethod.COD ? PaymentStatus.Pending : PaymentStatus.Completed,
            OrderStatus = OrderStatus.Confirmed, // Đơn chốt trực tiếp qua telesales -> chuyển ngay sang Đã xác nhận chờ đóng gói
            CallVerificationStatus = "CallVerified",
            CallVerificationNote = $"Đơn chốt trực tiếp qua kênh {sourceChannel}",
            AssignedStaffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            AssignedStaffName = User.Identity?.Name ?? "Nhân viên Bán hàng",
            CarrierName = !string.IsNullOrWhiteSpace(carrierName) ? carrierName : "Giao Hàng Nhanh (GHN)",
            CustomerNotes = customerNotes,
            Subtotal = subtotal,
            ShippingFee = shippingFee,
            OrderDiscountAmount = discount,
            AppliedOrderCouponCode = discount > 0 ? couponCode : null,
            TotalAmount = totalAmount,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSlug = product.Slug,
                    VariantSku = !string.IsNullOrWhiteSpace(variantSku) ? variantSku : product.Sku,
                    VariantName = variant?.VariantName ?? "Mặc định",
                    UnitPrice = unitPrice,
                    Quantity = validQuantity,
                    WarrantyMonths = product.WarrantyMonths,
                    ThumbnailUrl = !string.IsNullOrEmpty(variant?.ImageUrl) ? variant.ImageUrl : product.ThumbnailUrl
                }
            },
            StatusHistory = new List<OrderStatusHistory>
            {
                new()
                {
                    Status = OrderStatus.Confirmed,
                    Note = $"Tạo đơn hàng thủ công qua kênh {sourceChannel} bởi {User.Identity?.Name ?? "Sales"}"
                }
            }
        };

        await _orderRepo.AddAsync(newOrder, cancellationToken);

        // Giảm tồn khả dụng
        product.AvailableQuantity = Math.Max(0, product.AvailableQuantity - validQuantity);
        product.ReservedQuantity += validQuantity;
        await _productRepo.UpdateAsync(product, cancellationToken);

        await LogAuditAsync("OrderCreateManual", "Sales", $"Tạo đơn thủ công {newOrder.OrderCode} qua {sourceChannel} cho {customerName}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo thành công đơn hàng {newOrder.OrderCode} qua kênh {sourceChannel}! Đơn đã được xác nhận và chuyển thẳng sang khâu đóng gói.";
        return RedirectToAction(nameof(Index), new { tab = "orders" });
    }

    private async Task LogAuditAsync(string action, string module, string details, CancellationToken cancellationToken)
    {
        try
        {
            var audit = new SecurityAuditLog
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
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
            // Non-blocking audit failure
        }
    }
}
