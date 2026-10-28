using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff")]
public class MarketingController : AdminBaseController
{
    private readonly IRepository<Coupon> _couponRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Banner> _bannerRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;

    public MarketingController(
        IRepository<Coupon> couponRepo,
        IRepository<User> userRepo,
        IRepository<Banner> bannerRepo,
        IRepository<SecurityAuditLog> auditLogRepo)
    {
        _couponRepo = couponRepo;
        _userRepo = userRepo;
        _bannerRepo = bannerRepo;
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        bool? isActive = null,
        string tab = "coupons",
        string? couponType = "All",
        BannerPosition? position = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 12;
        page = Math.Max(1, page);

        var allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
        var allUsers = await _userRepo.GetAllAsync(cancellationToken);
        var allBanners = await _bannerRepo.GetAllAsync(cancellationToken);

        // Seed default promotional coupons and banners if empty
        if (!allCoupons.Any() || !allBanners.Any())
        {
            await SeedInitialMarketingDataAsync(cancellationToken);
            allCoupons = await _couponRepo.GetAllAsync(cancellationToken);
            allBanners = await _bannerRepo.GetAllAsync(cancellationToken);
        }

        var customerUsers = allUsers.Where(u => u.Role == UserRole.Customer).ToList();
        var seoCampaignList = allBanners.Where(b => !string.IsNullOrEmpty(b.EventSlug) || b.Position == BannerPosition.PopupModal).ToList();

        var model = new MarketingViewModel
        {
            TotalCoupons = allCoupons.Count,
            ActiveCoupons = allCoupons.Count(c => c.IsActive && c.EndDate >= DateTime.UtcNow),
            TotalVoucherUses = allCoupons.Sum(c => c.UsedCount),
            TotalFlashSaleCoupons = allCoupons.Count(c => c.IsFlashSale),

            TotalCustomers = customerUsers.Count,
            ActiveCustomers = customerUsers.Count(c => c.IsActive),
            VipCustomers = customerUsers.Count(c => c.RewardPoints >= 500),

            TotalBanners = allBanners.Count,
            ActiveBanners = allBanners.Count(b => b.IsActive),
            TotalSeoCampaigns = seoCampaignList.Count,

            ActiveTab = tab.ToLowerInvariant() switch
            {
                "customers" => "customers",
                "banners" => "banners",
                "seo" => "seo",
                _ => "coupons"
            },
            SearchQuery = search,
            IsActiveFilter = isActive,
            CouponTypeFilter = string.IsNullOrWhiteSpace(couponType) ? "All" : couponType,
            BannerPositionFilter = position,
            Page = page,
            PageSize = pageSize
        };

        if (model.ActiveTab == "coupons")
        {
            var q = allCoupons.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(c =>
                    c.Code.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.Title.Contains(s, StringComparison.OrdinalIgnoreCase));
            }
            if (isActive.HasValue)
            {
                q = q.Where(c => c.IsActive == isActive.Value);
            }
            if (!string.IsNullOrWhiteSpace(couponType) && !couponType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                switch (couponType.ToLowerInvariant())
                {
                    case "order":
                        q = q.Where(c => c.Scope == CouponScope.Order && !c.IsFlashSale && !c.IsComboPromotion);
                        break;
                    case "shipping":
                        q = q.Where(c => c.Scope == CouponScope.Shipping);
                        break;
                    case "flashsale":
                        q = q.Where(c => c.IsFlashSale);
                        break;
                    case "combo":
                        q = q.Where(c => c.IsComboPromotion);
                        break;
                }
            }
            var filtered = q.OrderByDescending(c => c.CreatedAt).ToList();
            model.TotalFilteredCount = filtered.Count;
            model.Coupons = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else if (model.ActiveTab == "customers")
        {
            var q = customerUsers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(c =>
                    (!string.IsNullOrEmpty(c.FullName) && c.FullName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.PhoneNumber) && c.PhoneNumber.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }
            if (isActive.HasValue)
            {
                q = q.Where(c => c.IsActive == isActive.Value);
            }
            var filtered = q.OrderByDescending(c => c.CreatedAt).ToList();
            model.TotalFilteredCount = filtered.Count;
            model.Customers = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else if (model.ActiveTab == "seo")
        {
            var q = seoCampaignList.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(b =>
                    b.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(b.EventSlug) && b.EventSlug.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(b.MetaTitle) && b.MetaTitle.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }
            if (isActive.HasValue)
            {
                q = q.Where(b => b.IsActive == isActive.Value);
            }
            var filtered = q.OrderByDescending(b => b.CreatedAt).ToList();
            model.TotalFilteredCount = filtered.Count;
            model.SeoCampaigns = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else // banners
        {
            var q = allBanners.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(b => b.Title.Contains(s, StringComparison.OrdinalIgnoreCase));
            }
            if (isActive.HasValue)
            {
                q = q.Where(b => b.IsActive == isActive.Value);
            }
            if (position.HasValue)
            {
                q = q.Where(b => b.Position == position.Value);
            }
            var filtered = q.OrderBy(b => b.DisplayOrder).ThenByDescending(b => b.CreatedAt).ToList();
            model.TotalFilteredCount = filtered.Count;
            model.Banners = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateCoupon(
        [FromForm] CreateCouponInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Title) || input.DiscountValue <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Mã coupon, Tiêu đề và Giá trị giảm lớn hơn 0.";
            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        var codeNormalized = input.Code.Trim().ToUpper();
        var existing = (await _couponRepo.GetAllAsync(cancellationToken))
            .FirstOrDefault(c => c.Code.Equals(codeNormalized, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            TempData["ErrorMessage"] = $"Mã coupon [{codeNormalized}] đã tồn tại trong hệ thống.";
            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        var coupon = new Coupon
        {
            Code = codeNormalized,
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            Scope = input.Scope,
            DiscountType = input.DiscountType,
            DiscountValue = input.DiscountValue,
            MaxDiscountAmount = input.DiscountType == DiscountType.Percentage ? input.MaxDiscountAmount : null,
            MinOrderValue = input.MinOrderValue,
            UsageLimit = input.UsageLimit > 0 ? input.UsageLimit : 100,
            UsageLimitPerUser = input.UsageLimitPerUser > 0 ? input.UsageLimitPerUser : 1,
            StartDate = input.StartDate,
            EndDate = input.EndDate > input.StartDate ? input.EndDate : input.StartDate.AddMonths(1),
            IsActive = true,
            IsFlashSale = input.IsFlashSale,
            FlashSaleSlot = input.IsFlashSale ? input.FlashSaleSlot : null,
            IsComboPromotion = input.IsComboPromotion,
            ApplicableCategory = input.ApplicableCategory ?? "Tất cả",
            MinItemQuantity = input.MinItemQuantity > 0 ? input.MinItemQuantity : 1,
            CreatedAt = DateTime.UtcNow
        };

        await _couponRepo.AddAsync(coupon, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateCoupon",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Tạo mã ưu đãi [{coupon.Code}] - Loại: {coupon.DiscountType} - Trị giá: {coupon.DiscountValue:N0} (Flash Sale: {coupon.IsFlashSale}, Combo: {coupon.IsComboPromotion})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo thành công mã khuyến mãi [{coupon.Code}]!";
        return RedirectToAction(nameof(Index), new { tab = "coupons" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> EditCoupon(
        [FromForm] EditCouponInputModel input,
        CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepo.GetByIdAsync(input.Id, cancellationToken);
        if (coupon == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy mã ưu đãi cần chỉnh sửa.";
            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        coupon.Title = input.Title.Trim();
        coupon.Description = input.Description?.Trim();
        coupon.Scope = input.Scope;
        coupon.DiscountType = input.DiscountType;
        coupon.DiscountValue = input.DiscountValue;
        coupon.MaxDiscountAmount = input.DiscountType == DiscountType.Percentage ? input.MaxDiscountAmount : null;
        coupon.MinOrderValue = input.MinOrderValue;
        coupon.UsageLimit = input.UsageLimit;
        coupon.UsageLimitPerUser = input.UsageLimitPerUser;
        coupon.StartDate = input.StartDate;
        coupon.EndDate = input.EndDate;
        coupon.IsActive = input.IsActive;
        coupon.IsFlashSale = input.IsFlashSale;
        coupon.FlashSaleSlot = input.IsFlashSale ? input.FlashSaleSlot : null;
        coupon.IsComboPromotion = input.IsComboPromotion;
        coupon.ApplicableCategory = input.ApplicableCategory ?? "Tất cả";
        coupon.MinItemQuantity = input.MinItemQuantity;
        coupon.UpdatedAt = DateTime.UtcNow;

        await _couponRepo.UpdateAsync(coupon, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "EditCoupon",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Cập nhật quy tắc voucher [{coupon.Code}] - Hạn dùng: {coupon.EndDate:dd/MM/yyyy} - FlashSale: {coupon.IsFlashSale}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật cấu hình mã ưu đãi [{coupon.Code}]!";
        return RedirectToAction(nameof(Index), new { tab = "coupons" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleCouponStatus(
        string id,
        CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepo.GetByIdAsync(id, cancellationToken);
        if (coupon == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy mã ưu đãi.";
            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;
        await _couponRepo.UpdateAsync(coupon, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "ToggleCouponStatus",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Chuyển trạng thái voucher [{coupon.Code}] thành {(coupon.IsActive ? "Kích hoạt" : "Vô hiệu hóa")}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(coupon.IsActive ? "kích hoạt" : "vô hiệu hóa")} mã khuyến mãi [{coupon.Code}].";
        return RedirectToAction(nameof(Index), new { tab = "coupons" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteCoupon(
        string id,
        CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepo.GetByIdAsync(id, cancellationToken);
        if (coupon == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy mã ưu đãi.";
            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        await _couponRepo.DeleteAsync(coupon.Id, softDelete: true, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DeleteCoupon",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xóa bỏ hoàn toàn mã voucher [{coupon.Code}] khỏi hệ thống"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa mã ưu đãi [{coupon.Code}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "coupons" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateBanner(
        [FromForm] CreateBannerInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.ImageUrl))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập tiêu đề banner và URL hình ảnh.";
            return RedirectToAction(nameof(Index), new { tab = "banners" });
        }

        var banner = new Banner
        {
            Title = input.Title.Trim(),
            ImageUrl = input.ImageUrl.Trim(),
            MobileImageUrl = input.MobileImageUrl?.Trim(),
            LinkUrl = input.LinkUrl?.Trim(),
            Position = input.Position,
            DisplayOrder = input.DisplayOrder,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _bannerRepo.AddAsync(banner, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateBanner",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Thêm banner khuyến mãi [{banner.Title}] tại vị trí {banner.Position}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã thêm banner tiếp thị [{banner.Title}]!";
        return RedirectToAction(nameof(Index), new { tab = "banners" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> EditBanner(
        [FromForm] EditBannerInputModel input,
        CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepo.GetByIdAsync(input.Id, cancellationToken);
        if (banner == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy banner cần chỉnh sửa.";
            return RedirectToAction(nameof(Index), new { tab = "banners" });
        }

        banner.Title = input.Title.Trim();
        banner.ImageUrl = input.ImageUrl.Trim();
        banner.MobileImageUrl = input.MobileImageUrl?.Trim();
        banner.LinkUrl = input.LinkUrl?.Trim();
        banner.Position = input.Position;
        banner.DisplayOrder = input.DisplayOrder;
        banner.StartDate = input.StartDate;
        banner.EndDate = input.EndDate;
        banner.IsActive = input.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;

        await _bannerRepo.UpdateAsync(banner, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "EditBanner",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Cập nhật banner [{banner.Title}] tại vị trí {banner.Position}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật banner [{banner.Title}]!";
        return RedirectToAction(nameof(Index), new { tab = "banners" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleBannerStatus(
        string bannerId,
        CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepo.GetByIdAsync(bannerId, cancellationToken);
        if (banner == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy banner.";
            return RedirectToAction(nameof(Index), new { tab = "banners" });
        }

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;
        await _bannerRepo.UpdateAsync(banner, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(banner.IsActive ? "kích hoạt" : "tắt hiển thị")} banner [{banner.Title}].";
        return RedirectToAction(nameof(Index), new { tab = "banners" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteBanner(
        string bannerId,
        CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepo.GetByIdAsync(bannerId, cancellationToken);
        if (banner == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy banner.";
            return RedirectToAction(nameof(Index), new { tab = "banners" });
        }

        await _bannerRepo.DeleteAsync(banner.Id, softDelete: true, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DeleteBanner",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xóa bỏ banner tiếp thị [{banner.Title}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa banner [{banner.Title}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "banners" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SaveSeoCampaign(
        [FromForm] SeoCampaignInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.EventSlug))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập Tiêu đề chiến dịch và Đường dẫn thân thiện (Slug).";
            return RedirectToAction(nameof(Index), new { tab = "seo" });
        }

        Banner banner;
        bool isNew = string.IsNullOrWhiteSpace(input.Id);

        if (isNew)
        {
            banner = new Banner
            {
                Position = BannerPosition.PopupModal,
                DisplayOrder = 99,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            var found = await _bannerRepo.GetByIdAsync(input.Id!, cancellationToken);
            if (found == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chiến dịch cần cập nhật.";
                return RedirectToAction(nameof(Index), new { tab = "seo" });
            }
            banner = found;
        }

        banner.Title = input.Title.Trim();
        banner.EventSlug = input.EventSlug.Trim().ToLowerInvariant().Replace(" ", "-");
        banner.ImageUrl = input.ImageUrl?.Trim() ?? "/images/banners/default-campaign.jpg";
        banner.LinkUrl = string.IsNullOrWhiteSpace(input.LinkUrl) ? $"/khuyen-mai/{banner.EventSlug}" : input.LinkUrl.Trim();
        banner.StartDate = input.StartDate;
        banner.EndDate = input.EndDate;
        banner.MetaTitle = input.MetaTitle?.Trim() ?? input.Title.Trim();
        banner.MetaDescription = input.MetaDescription?.Trim();
        banner.MetaKeywords = input.MetaKeywords?.Trim();
        banner.CanonicalUrl = input.CanonicalUrl?.Trim();
        banner.OgImageUrl = input.OgImageUrl?.Trim() ?? banner.ImageUrl;
        banner.HtmlContent = input.HtmlContent;
        banner.IsActive = input.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            await _bannerRepo.AddAsync(banner, cancellationToken);
        }
        else
        {
            await _bannerRepo.UpdateAsync(banner, cancellationToken);
        }

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = isNew ? "CreateSeoCampaign" : "UpdateSeoCampaign",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"{(isNew ? "Tạo mới" : "Cập nhật")} chiến dịch SEO/Landing Page [{banner.Title}] (Slug: {banner.EventSlug})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lưu chiến dịch SEO & Landing Page [{banner.Title}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "seo" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleSeoCampaignStatus(
        string id,
        CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepo.GetByIdAsync(id, cancellationToken);
        if (banner == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chiến dịch SEO.";
            return RedirectToAction(nameof(Index), new { tab = "seo" });
        }

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;
        await _bannerRepo.UpdateAsync(banner, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(banner.IsActive ? "kích hoạt" : "vô hiệu hóa")} chiến dịch [{banner.Title}].";
        return RedirectToAction(nameof(Index), new { tab = "seo" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteSeoCampaign(
        string id,
        CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepo.GetByIdAsync(id, cancellationToken);
        if (banner == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chiến dịch.";
            return RedirectToAction(nameof(Index), new { tab = "seo" });
        }

        await _bannerRepo.DeleteAsync(banner.Id, softDelete: true, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DeleteSeoCampaign",
            Module = "Marketing",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xóa bỏ chiến dịch SEO/Landing page [{banner.Title}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa chiến dịch SEO [{banner.Title}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "seo" });
    }

    // CRM Fallbacks for compatibility
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult ToggleCustomerStatus(string customerId)
    {
        return RedirectToAction("Index", "Customer");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult AdjustPoints([FromForm] AdjustCustomerPointsInputModel input)
    {
        return RedirectToAction("Index", "Customer");
    }

    private async Task SeedInitialMarketingDataAsync(CancellationToken cancellationToken)
    {
        var sampleCoupons = new List<Coupon>
        {
            new()
            {
                Code = "TECHZONE50K",
                Title = "Giảm ngay 50.000đ cho đơn hàng điện tử từ 500K",
                Description = "Áp dụng toàn sàn cho mọi đơn hàng điện tử",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 50000,
                MinOrderValue = 500000,
                UsageLimit = 500,
                UsedCount = 42,
                UsageLimitPerUser = 2,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(50),
                IsActive = true
            },
            new()
            {
                Code = "APPLEVIP10",
                Title = "Giảm 10% (tối đa 1.000.000đ) khi mua hệ sinh thái Apple",
                Description = "Đặc quyền thành viên TechZone VIP khi mua iPhone, MacBook, iPad",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                MaxDiscountAmount = 1000000,
                MinOrderValue = 10000000,
                UsageLimit = 200,
                UsedCount = 28,
                UsageLimitPerUser = 1,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(25),
                IsActive = true
            },
            new()
            {
                Code = "FLASHSALE99K",
                Title = "Flash Sale Giờ Vàng - Giảm 99.000đ cho phụ kiện",
                Description = "Áp dụng trong khung giờ vàng 12:00 - 15:00 cho phụ kiện điện thoại & máy tính",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 99000,
                MinOrderValue = 300000,
                UsageLimit = 100,
                UsedCount = 64,
                UsageLimitPerUser = 1,
                StartDate = DateTime.UtcNow.AddDays(-2),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                IsFlashSale = true,
                FlashSaleSlot = "12:00 - 15:00",
                ApplicableCategory = "Phụ kiện"
            },
            new()
            {
                Code = "COMBOGAMING",
                Title = "Combo Khuyến Mãi Mua Kèm Phụ Kiện - Giảm 200.000đ",
                Description = "Áp dụng khi mua từ 2 sản phẩm Laptop Gaming + Phụ kiện tai nghe/chuột",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 200000,
                MinOrderValue = 2000000,
                UsageLimit = 150,
                UsedCount = 19,
                UsageLimitPerUser = 1,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(40),
                IsActive = true,
                IsComboPromotion = true,
                ApplicableCategory = "Laptop",
                MinItemQuantity = 2
            },
            new()
            {
                Code = "FREESHIPNOW",
                Title = "Miễn phí vận chuyển hỏa tốc toàn quốc",
                Description = "Trợ giá cước vận chuyển đối tác GHN / ViettelPost",
                Scope = CouponScope.Shipping,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 40000,
                MinOrderValue = 300000,
                UsageLimit = 1000,
                UsedCount = 185,
                UsageLimitPerUser = 5,
                StartDate = DateTime.UtcNow.AddDays(-15),
                EndDate = DateTime.UtcNow.AddDays(45),
                IsActive = true
            }
        };

        foreach (var c in sampleCoupons)
        {
            var exists = (await _couponRepo.GetAllAsync(cancellationToken)).Any(x => x.Code == c.Code);
            if (!exists)
            {
                await _couponRepo.AddAsync(c, cancellationToken);
            }
        }

        var sampleBanners = new List<Banner>
        {
            new()
            {
                Title = "Lễ Hội Flagship - Mở bán iPhone 16 Pro Max Titan",
                ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=1200&q=80",
                MobileImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80",
                LinkUrl = "/Product?search=iPhone",
                Position = BannerPosition.HomeMainSlider,
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Title = "Tuần Lễ Laptop Gaming & Đồ Họa ASUS ROG - Giảm tới 4 triệu",
                ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?auto=format&fit=crop&w=1200&q=80",
                MobileImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?auto=format&fit=crop&w=600&q=80",
                LinkUrl = "/Product?search=ROG",
                Position = BannerPosition.HomeMainSlider,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Title = "Đại Tiệc Công Nghệ Apple Black Friday 2026",
                EventSlug = "dai-tiec-apple-2026",
                ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02560?auto=format&fit=crop&w=1200&q=80",
                LinkUrl = "/khuyen-mai/dai-tiec-apple-2026",
                Position = BannerPosition.PopupModal,
                DisplayOrder = 1,
                IsActive = true,
                MetaTitle = "Đại Tiệc Apple 2026 - Giảm sốc iPhone, iPad, MacBook tại TechZone",
                MetaDescription = "Sự kiện siêu ưu đãi hệ sinh thái Apple chính hãng VN/A lớn nhất năm tại TechZone. Giảm trực tiếp tới 4 triệu đồng, trả góp 0%, quà tặng bảo hành kim cương.",
                MetaKeywords = "apple, iphone 16, macbook, techzone, black friday, khuyen mai",
                CanonicalUrl = "https://techzone.vn/khuyen-mai/dai-tiec-apple-2026",
                OgImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02560?auto=format&fit=crop&w=1200&q=80",
                HtmlContent = "### THỂ LỆ CHƯƠNG TRÌNH ĐẠI TIỆC APPLE 2026\n- Giảm ngay 10% tối đa 1.000.000đ khi nhập mã `APPLEVIP10`\n- Tặng gói bảo hành toàn diện 2 năm 1 đổi 1\n- Miễn phí vận chuyển hỏa tốc trong 2 giờ nội thành"
            }
        };

        foreach (var b in sampleBanners)
        {
            var exists = (await _bannerRepo.GetAllAsync(cancellationToken)).Any(x => x.Title == b.Title);
            if (!exists)
            {
                await _bannerRepo.AddAsync(b, cancellationToken);
            }
        }
    }
}
