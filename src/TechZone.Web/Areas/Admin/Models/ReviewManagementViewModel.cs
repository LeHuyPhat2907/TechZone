using TechZone.Application.Common.Models;
using TechZone.Domain.Entities;

namespace TechZone.Web.Areas.Admin.Models;

public class ReviewManagementViewModel
{
    public PagedResult<Review> Reviews { get; set; } = new();
    public IReadOnlyList<Product> Products { get; set; } = Array.Empty<Product>();

    // Filters
    public string? SearchKeyword { get; set; }
    public string? SelectedProductId { get; set; }
    public string? StatusFilter { get; set; } // "all", "approved", "pending", "hidden"
    public int? RatingFilter { get; set; } // 1..5
    public string? RepliedFilter { get; set; } // "all", "replied", "unreplied"

    // Statistics & KPI
    public long TotalReviews { get; set; }
    public long ApprovedCount { get; set; }
    public long PendingApprovalCount { get; set; }
    public long RespondedCount { get; set; }
    public double AverageRating { get; set; }

    // Rating distribution
    public long Star5Count { get; set; }
    public long Star4Count { get; set; }
    public long Star3Count { get; set; }
    public long Star2Count { get; set; }
    public long Star1Count { get; set; }
}
