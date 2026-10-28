using TechZone.Application.Common.Models;
using TechZone.Domain.Entities;

namespace TechZone.Web.Areas.Admin.Models;

public class ProductManagementViewModel
{
    public PagedResult<Product> Products { get; set; } = new();
    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();
    public IReadOnlyList<Brand> Brands { get; set; } = Array.Empty<Brand>();

    // Filters
    public string? SearchKeyword { get; set; }
    public string? SelectedCategoryId { get; set; }
    public string? SelectedBrandId { get; set; }
    public string? StockFilter { get; set; } // "all", "in_stock", "low_stock", "out_of_stock"
    public bool? IsActive { get; set; }

    // Statistics KPI
    public long TotalProducts { get; set; }
    public long InStockCount { get; set; }
    public long LowStockCount { get; set; }
    public long OutOfStockOrHiddenCount { get; set; }
}
