using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;
using TechZone.Web.Models;

namespace TechZone.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;
    private readonly IRepository<Banner> _bannerRepo;
    private readonly IRepository<Post> _postRepo;

    public HomeController(
        IProductService productService,
        ICategoryService categoryService,
        IBrandService brandService,
        IRepository<Banner> bannerRepo,
        IRepository<Post> postRepo)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
        _bannerRepo = bannerRepo;
        _postRepo = postRepo;
    }

    public async Task<IActionResult> Index()
    {
        var banners = await _bannerRepo.FindAsync(b => b.IsActive);
        var categories = await _categoryService.GetAllCategoriesAsync();
        var brands = await _brandService.GetAllBrandsAsync();
        var featured = await _productService.GetFeaturedProductsAsync(8);
        var newArrivals = await _productService.GetNewArrivalsAsync(8);
        var flashSales = await _productService.GetFlashSaleProductsAsync(4);

        var viewModel = new HomeIndexViewModel
        {
            Banners = banners.OrderBy(b => b.DisplayOrder).ToList(),
            Categories = categories,
            Brands = brands,
            FlashSaleProducts = flashSales,
            FeaturedProducts = featured,
            NewArrivals = newArrivals
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return RedirectToAction(nameof(Policy), new { tab = "privacy" });
    }

    /// <summary>
    /// 6.1 catalog.view_public: Xem bài viết công nghệ, tin ra mắt, thủ thuật
    /// </summary>
    [HttpGet]
    [Route("News")]
    [Route("tin-tuc-cong-nghe")]
    public async Task<IActionResult> News(string? tag = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var posts = await _postRepo.GetAllAsync(cancellationToken);
        var published = posts.Where(p => p.IsPublished).OrderByDescending(p => p.PublishedAt ?? p.CreatedAt).ToList();

        if (!published.Any())
        {
            // Fallback rich sample tech articles
            published = new List<Post>
            {
                new()
                {
                    Id = "post-01",
                    Title = "Đánh giá chi tiết iPhone 16 Pro Max: Nút Camera Control và Chip A18 Pro có thực sự đáng nâng cấp?",
                    Slug = "danh-gia-iphone-16-pro-max",
                    Summary = "Trải nghiệm thực tế iPhone 16 Pro Max sau 2 tuần sử dụng: Pin trâu hơn, viền màn hình siêu mỏng và khả năng quay video 4K 120fps Dolby Vision.",
                    CategoryName = "Đánh giá thiết bị",
                    AuthorName = "BTV Công Nghệ TechZone",
                    PublishedAt = DateTime.UtcNow.AddDays(-2),
                    ViewCount = 1450,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=600",
                    Tags = new List<string> { "Apple", "iPhone 16", "Đánh giá" }
                },
                new()
                {
                    Id = "post-02",
                    Title = "Tại sao Bảo Hành Điện Tử E-Warranty theo IMEI lại vượt trội hơn hẳn tem giấy truyền thống?",
                    Slug = "uu-diem-bao-hanh-dien-tu-imei",
                    Summary = "Khám phá cách hệ thống TechZone quản lý vòng đời thiết bị qua mã IMEI/Serial: Không sợ mất hóa đơn, tra cứu hạn bảo hành 24/7 chỉ trong 3 giây.",
                    CategoryName = "Cẩm nang công nghệ",
                    AuthorName = "Kỹ sư Trưởng TechZone",
                    PublishedAt = DateTime.UtcNow.AddDays(-4),
                    ViewCount = 2890,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=600",
                    Tags = new List<string> { "Bảo hành điện tử", "E-Warranty", "IMEI" }
                },
                new()
                {
                    Id = "post-03",
                    Title = "Top 5 Laptop Gaming AI đáng mua nhất 2026: Đồ họa RTX 40 Series cùng NPU thông minh",
                    Slug = "top-5-laptop-gaming-ai-2026",
                    Summary = "Tổng hợp các mẫu laptop gaming hiệu năng đỉnh cao tích hợp vi xử lý AI mạnh mẽ giúp tăng tốc độ render, tối ưu khung hình và tản nhiệt êm ái.",
                    CategoryName = "Tư vấn chọn mua",
                    AuthorName = "TechZone Lab",
                    PublishedAt = DateTime.UtcNow.AddDays(-6),
                    ViewCount = 3120,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=600",
                    Tags = new List<string> { "Laptop Gaming", "AI", "RTX 40" }
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            published = published.Where(p => p.Tags.Any(t => t.Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            published = published.Where(p => p.Title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) || p.Summary.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.SelectedTag = tag;
        ViewBag.SearchKeyword = search;
        return View(published);
    }

    /// <summary>
    /// 6.1 catalog.view_public: Xem trang chính sách chính thức (Bảo hành, Đổi trả, Vận chuyển, Thanh toán, Bảo mật)
    /// </summary>
    [HttpGet]
    [Route("Policy")]
    [Route("chinh-sach")]
    public IActionResult Policy(string tab = "warranty")
    {
        ViewBag.ActiveTab = tab.ToLower();
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
