using Microsoft.AspNetCore.Mvc;
using TechZone.Application.DTOs.Catalog;
using TechZone.Application.Services;
using TechZone.Web.Models;

namespace TechZone.Web.Controllers;

public class CatalogController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;

    public CatalogController(
        IProductService productService,
        ICategoryService categoryService,
        IBrandService brandService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
    }

    public async Task<IActionResult> Index(
        string? category,
        string? brand,
        string? keyword,
        decimal? minPrice,
        decimal? maxPrice,
        double? minRating,
        bool inStockOnly = false,
        string? sort = null,
        int page = 1)
    {
        var filter = new ProductFilterDto
        {
            CategorySlug = category,
            BrandSlug = brand,
            SearchKeyword = keyword,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinRating = minRating,
            InStockOnly = inStockOnly,
            SortBy = sort ?? "featured",
            Page = Math.Max(1, page),
            PageSize = 12
        };

        var pagedResult = await _productService.GetCatalogProductsAsync(filter);
        var categories = await _categoryService.GetAllCategoriesAsync();
        var brands = await _brandService.GetAllBrandsAsync();

        string? categoryName = null;
        if (!string.IsNullOrEmpty(category))
        {
            var cat = categories.FirstOrDefault(c => c.Slug == category);
            categoryName = cat?.Name;
        }

        string? brandName = null;
        if (!string.IsNullOrEmpty(brand))
        {
            var b = brands.FirstOrDefault(br => br.Slug == brand);
            brandName = b?.Name;
        }

        var viewModel = new CatalogViewModel
        {
            PagedProducts = pagedResult,
            Categories = categories,
            Brands = brands,
            CurrentFilter = filter,
            SelectedCategoryName = categoryName,
            SelectedBrandName = brandName
        };

        return View(viewModel);
    }

    [Route("product/{slug}")]
    [Route("Product/Detail/{slug}")]
    [Route("Catalog/Detail/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productService.GetProductBySlugAsync(slug);
        if (product == null)
        {
            product = await _productService.GetProductByIdAsync(slug);
        }
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpGet]
    [Route("Catalog/QuickSearch")]
    public async Task<IActionResult> QuickSearch([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(new { suggestions = Array.Empty<string>(), products = Array.Empty<object>() });
        }

        var result = await _productService.QuickSearchAsync(q);
        return Json(new
        {
            suggestions = result.Suggestions,
            products = result.Products.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                slug = p.Slug,
                thumbnailUrl = p.ThumbnailUrl,
                price = p.Price,
                priceText = $"{p.Price:N0}₫",
                originalPrice = p.OriginalPrice,
                discountRate = p.DiscountRate,
                brandName = p.BrandName
            })
        });
    }

    [HttpGet]
    [Route("Catalog/Compare")]
    public async Task<IActionResult> Compare([FromQuery] string? p1 = "iphone-15-pro-max", [FromQuery] string? p2 = "samsung-galaxy-s24-ultra")
    {
        var slug1 = string.IsNullOrWhiteSpace(p1) ? "iphone-15-pro-max" : p1.Trim();
        var slug2 = string.IsNullOrWhiteSpace(p2) ? "samsung-galaxy-s24-ultra" : p2.Trim();

        var comparison = await _productService.CompareProductsAsync(slug1, slug2);
        if (comparison == null)
        {
            var featured = await _productService.GetFeaturedProductsAsync(2);
            if (featured.Count >= 2)
            {
                comparison = await _productService.CompareProductsAsync(featured[0].Slug, featured[1].Slug);
            }
        }

        if (comparison == null)
        {
            return RedirectToAction("Index");
        }

        return View(comparison);
    }

    [HttpPost]
    [Route("Catalog/SubmitReview")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ProductId) || string.IsNullOrWhiteSpace(dto.Content) || string.IsNullOrWhiteSpace(dto.UserName))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ họ tên, số sao và nội dung đánh giá." });
        }

        if (User.Identity?.IsAuthenticated == true && string.IsNullOrWhiteSpace(dto.UserName))
        {
            dto.UserName = User.Identity.Name ?? "Khách hàng TechZone";
        }

        var result = await _productService.SubmitReviewAsync(dto);
        if (!result)
        {
            return BadRequest(new { success = false, message = "Không thể gửi đánh giá. Vui lòng thử lại sau." });
        }

        return Json(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá! Đánh giá của bạn đã được ghi nhận thành công với huy hiệu Đã mua tại TechZone." });
    }
}
