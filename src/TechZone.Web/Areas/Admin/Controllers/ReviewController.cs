using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.Common.Models;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff")]
public class ReviewController : AdminBaseController
{
    private readonly IRepository<Review> _reviewRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly ICacheService _cache;

    public ReviewController(
        IRepository<Review> reviewRepo,
        IRepository<Product> productRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        ICacheService cache)
    {
        _reviewRepo = reviewRepo;
        _productRepo = productRepo;
        _auditLogRepo = auditLogRepo;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? productId = null,
        string? status = null,
        int? rating = null,
        string? replied = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 12;
        page = Math.Max(1, page);

        var allReviews = await _reviewRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);

        // Overall KPI Metrics
        var totalReviews = allReviews.Count;
        var approvedCount = allReviews.Count(r => r.IsApproved);
        var pendingApprovalCount = allReviews.Count(r => !r.IsApproved);
        var respondedCount = allReviews.Count(r => !string.IsNullOrEmpty(r.StaffReply));
        var avgRating = allReviews.Count > 0 ? Math.Round(allReviews.Average(r => r.Rating), 1) : 5.0;

        var star5Count = allReviews.Count(r => r.Rating == 5);
        var star4Count = allReviews.Count(r => r.Rating == 4);
        var star3Count = allReviews.Count(r => r.Rating == 3);
        var star2Count = allReviews.Count(r => r.Rating == 2);
        var star1Count = allReviews.Count(r => r.Rating == 1);

        // In-memory filter pipeline
        var query = allReviews.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r =>
                (!string.IsNullOrEmpty(r.UserName) && r.UserName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.ProductName) && r.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.Content) && r.Content.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.UserPhone) && r.UserPhone.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            query = query.Where(r => r.ProductId == productId);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            switch (status.ToLowerInvariant())
            {
                case "approved":
                    query = query.Where(r => r.IsApproved);
                    break;
                case "pending":
                case "hidden":
                    query = query.Where(r => !r.IsApproved);
                    break;
            }
        }

        if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(replied) && replied != "all")
        {
            switch (replied.ToLowerInvariant())
            {
                case "replied":
                    query = query.Where(r => !string.IsNullOrEmpty(r.StaffReply));
                    break;
                case "unreplied":
                    query = query.Where(r => string.IsNullOrEmpty(r.StaffReply));
                    break;
            }
        }

        var filteredList = query.OrderByDescending(r => r.CreatedAt).ToList();
        var totalCount = filteredList.Count;

        var pagedItems = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var viewModel = new ReviewManagementViewModel
        {
            Reviews = new PagedResult<Review>(pagedItems, totalCount, page, pageSize),
            Products = allProducts.OrderBy(p => p.Name).ToList(),
            SearchKeyword = search,
            SelectedProductId = productId,
            StatusFilter = status,
            RatingFilter = rating,
            RepliedFilter = replied,
            TotalReviews = totalReviews,
            ApprovedCount = approvedCount,
            PendingApprovalCount = pendingApprovalCount,
            RespondedCount = respondedCount,
            AverageRating = avgRating,
            Star5Count = star5Count,
            Star4Count = star4Count,
            Star3Count = star3Count,
            Star2Count = star2Count,
            Star1Count = star1Count
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApprove(string id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var review = await _reviewRepo.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần kiểm duyệt.";
            return Redirect(returnUrl ?? "/Admin/Review");
        }

        review.IsApproved = !review.IsApproved;
        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(review, cancellationToken);

        // Recalculate product rating
        await RecalculateProductRatingAsync(review.ProductId, cancellationToken);

        var actionText = review.IsApproved ? "Duyệt hiển thị" : "Ẩn khỏi website";
        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = review.IsApproved ? "review.approve" : "review.hide",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"{actionText} đánh giá của khách hàng [{review.UserName}] cho sản phẩm [{review.ProductName}] ({review.Rating} sao)"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {actionText.ToLower()} đánh giá của '{review.UserName}' thành công!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string id, string staffReply, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(staffReply))
        {
            TempData["ErrorMessage"] = "Nội dung phản hồi không được để trống.";
            return Redirect(returnUrl ?? "/Admin/Review");
        }

        var review = await _reviewRepo.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá để phản hồi.";
            return Redirect(returnUrl ?? "/Admin/Review");
        }

        var staffName = User.Identity?.Name ?? "Quản trị viên TechZone";

        review.StaffReply = staffReply.Trim();
        review.StaffReplyAt = DateTime.UtcNow;
        review.StaffReplyByName = staffName;
        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(review, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "review.reply",
            Module = "Catalog",
            UserEmail = staffName,
            Details = $"Phản hồi đánh giá của [{review.UserName}] trên sản phẩm [{review.ProductName}]: \"{staffReply.Trim()}\""
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã gửi phản hồi chính thức tới khách hàng '{review.UserName}' thành công!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReply(string id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var review = await _reviewRepo.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
            return Redirect(returnUrl ?? "/Admin/Review");
        }

        review.StaffReply = null;
        review.StaffReplyAt = null;
        review.StaffReplyByName = null;
        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(review, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        TempData["SuccessMessage"] = "Đã thu hồi phản hồi thành công!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var review = await _reviewRepo.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần xóa.";
            return Redirect(returnUrl ?? "/Admin/Review");
        }

        var productId = review.ProductId;
        var reviewerName = review.UserName;
        var prodName = review.ProductName;

        // Hard delete review from MongoDB
        await _reviewRepo.DeleteAsync(id, softDelete: false, cancellationToken: cancellationToken);

        // Recalculate product rating
        await RecalculateProductRatingAsync(productId, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "review.delete",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Xóa vĩnh viễn đánh giá/bình luận của [{reviewerName}] trên sản phẩm [{prodName}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa đánh giá của khách hàng '{reviewerName}' khỏi hệ thống.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task RecalculateProductRatingAsync(string productId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId)) return;

        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);
        if (product == null) return;

        var approvedReviews = await _reviewRepo.FindAsync(r => r.ProductId == productId && r.IsApproved, cancellationToken);
        if (approvedReviews.Count > 0)
        {
            product.RatingAverage = Math.Round(approvedReviews.Average(r => r.Rating), 1);
            product.RatingCount = approvedReviews.Count;
        }
        else
        {
            product.RatingAverage = 5.0;
            product.RatingCount = 0;
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);
    }
}
