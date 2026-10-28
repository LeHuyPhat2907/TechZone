using TechZone.Application.DTOs.Catalog;
using TechZone.Domain.Entities;

namespace TechZone.Web.Models;

public class HomeIndexViewModel
{
    public IReadOnlyList<Banner> Banners { get; set; } = new List<Banner>();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IReadOnlyList<BrandDto> Brands { get; set; } = new List<BrandDto>();
    public IReadOnlyList<ProductCardDto> FlashSaleProducts { get; set; } = new List<ProductCardDto>();
    public IReadOnlyList<ProductCardDto> FeaturedProducts { get; set; } = new List<ProductCardDto>();
    public IReadOnlyList<ProductCardDto> NewArrivals { get; set; } = new List<ProductCardDto>();
}
