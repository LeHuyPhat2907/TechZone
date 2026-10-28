using TechZone.Application.Common.Models;
using TechZone.Application.DTOs.Catalog;

namespace TechZone.Web.Models;

public class CatalogViewModel
{
    public PagedResult<ProductCardDto> PagedProducts { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IReadOnlyList<BrandDto> Brands { get; set; } = new List<BrandDto>();
    public ProductFilterDto CurrentFilter { get; set; } = new();
    public string? SelectedCategoryName { get; set; }
    public string? SelectedBrandName { get; set; }
}
