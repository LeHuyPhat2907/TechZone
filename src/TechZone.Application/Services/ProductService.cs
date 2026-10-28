using System.Linq.Expressions;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.Common.Models;
using TechZone.Application.DTOs.Catalog;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;

namespace TechZone.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductCardDto>> GetFeaturedProductsAsync(int count = 8, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCardDto>> GetNewArrivalsAsync(int count = 8, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCardDto>> GetFlashSaleProductsAsync(int count = 4, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductCardDto>> GetCatalogProductsAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCardDto>> SearchProductsAsync(string keyword, int count = 10, CancellationToken cancellationToken = default);
    Task<QuickSearchResultDto> QuickSearchAsync(string query, CancellationToken cancellationToken = default);
    Task<ProductComparisonDto?> CompareProductsAsync(string slug1, string slug2, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReviewItemDto>> GetProductReviewsAsync(string productId, CancellationToken cancellationToken = default);
    Task<bool> SubmitReviewAsync(SubmitReviewDto dto, CancellationToken cancellationToken = default);
}

public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Brand> _brandRepo;
    private readonly IRepository<Review> _reviewRepo;
    private readonly ICacheService _cache;

    public ProductService(
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        IRepository<Brand> brandRepo,
        IRepository<Review> reviewRepo,
        ICacheService cache)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _brandRepo = brandRepo;
        _reviewRepo = reviewRepo;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ProductCardDto>> GetFeaturedProductsAsync(int count = 8, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products:featured:{count}";
        var cached = await _cache.GetAsync<List<ProductCardDto>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _productRepo.FindAsync(p => p.IsActive && p.IsFeatured, cancellationToken);
        var dtos = products
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .Select(MapToCardDto)
            .ToList();

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(15), cancellationToken);
        return dtos;
    }

    public async Task<IReadOnlyList<ProductCardDto>> GetNewArrivalsAsync(int count = 8, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products:new_arrivals:{count}";
        var cached = await _cache.GetAsync<List<ProductCardDto>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _productRepo.FindAsync(p => p.IsActive && p.IsNewArrival, cancellationToken);
        var dtos = products
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .Select(MapToCardDto)
            .ToList();

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(15), cancellationToken);
        return dtos;
    }

    public async Task<IReadOnlyList<ProductCardDto>> GetFlashSaleProductsAsync(int count = 4, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products:flash_sale:{count}";
        var cached = await _cache.GetAsync<List<ProductCardDto>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _productRepo.FindAsync(p => p.IsActive && p.OriginalPrice > p.Price, cancellationToken);
        var dtos = products
            .OrderByDescending(p => p.OriginalPrice - p.Price)
            .Take(count)
            .Select(MapToCardDto)
            .ToList();

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);
        return dtos;
    }

    public async Task<PagedResult<ProductCardDto>> GetCatalogProductsAsync(ProductFilterDto filter, CancellationToken cancellationToken = default)
    {
        string? targetCategoryId = null;
        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            var cat = await _categoryRepo.FindOneAsync(c => c.Slug == filter.CategorySlug && c.IsActive, cancellationToken);
            if (cat != null) targetCategoryId = cat.Id;
        }

        string? targetBrandId = null;
        if (!string.IsNullOrWhiteSpace(filter.BrandSlug))
        {
            var brand = await _brandRepo.FindOneAsync(b => b.Slug == filter.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null) targetBrandId = brand.Id;
        }

        // Build dynamic expression with strict AND logic across all filter criteria
        Expression<Func<Product, bool>> predicate = p =>
            p.IsActive &&
            (targetCategoryId == null || p.CategoryId == targetCategoryId) &&
            (targetBrandId == null || p.BrandId == targetBrandId) &&
            (!filter.MinPrice.HasValue || p.Price >= filter.MinPrice.Value) &&
            (!filter.MaxPrice.HasValue || p.Price <= filter.MaxPrice.Value) &&
            (!filter.MinRating.HasValue || p.RatingAverage >= filter.MinRating.Value) &&
            (!filter.InStockOnly || p.AvailableQuantity > 0) &&
            (string.IsNullOrEmpty(filter.SearchKeyword) ||
             p.Name.Contains(filter.SearchKeyword) ||
             p.BrandName.Contains(filter.SearchKeyword) ||
             p.CategoryName.Contains(filter.SearchKeyword));

        Expression<Func<Product, object>>? orderBy = filter.SortBy switch
        {
            "price_asc" => p => p.Price,
            "price_desc" => p => p.Price,
            "new" => p => p.CreatedAt,
            "best_seller" => p => p.SoldCount,
            "rating_desc" => p => p.RatingAverage,
            _ => p => p.RatingAverage
        };

        bool isAscending = filter.SortBy == "price_asc";

        var (items, totalCount) = await _productRepo.GetPagedAsync(
            predicate,
            filter.Page,
            filter.PageSize,
            orderBy,
            isAscending,
            cancellationToken);

        var dtos = items.Select(MapToCardDto).ToList();
        return new PagedResult<ProductCardDto>(dtos, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.FindOneAsync(p => p.Slug == slug && p.IsActive, cancellationToken);
        if (product == null) return null;

        var dto = MapToDetailDto(product);

        // Fetch related products in the same category
        var related = await _productRepo.FindAsync(
            p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive,
            cancellationToken);

        dto.RelatedProducts = related.Take(4).Select(MapToCardDto).ToList();

        // Fetch approved reviews for this product
        var reviews = await _reviewRepo.FindAsync(r => r.ProductId == product.Id && r.IsApproved, cancellationToken);
        dto.Reviews = reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductReviewItemDto
            {
                Id = r.Id,
                UserName = r.UserName,
                Rating = r.Rating,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                IsVerifiedBuyer = r.IsVerifiedBuyer,
                StaffReply = r.StaffReply,
                StaffReplyAt = r.StaffReplyAt
            })
            .ToList();

        return dto;
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null || !product.IsActive) return null;

        return MapToDetailDto(product);
    }

    public async Task<IReadOnlyList<ProductCardDto>> SearchProductsAsync(string keyword, int count = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<ProductCardDto>();

        var kw = keyword.Trim();
        var products = await _productRepo.FindAsync(
            p => p.IsActive && (p.Name.Contains(kw) || p.BrandName.Contains(kw) || p.CategoryName.Contains(kw)),
            cancellationToken);

        return products.Take(count).Select(MapToCardDto).ToList();
    }

    public async Task<QuickSearchResultDto> QuickSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new QuickSearchResultDto();
        }

        var kw = query.Trim();
        var cacheKey = $"quick_search:{kw.ToLowerInvariant()}";
        var cached = await _cache.GetAsync<QuickSearchResultDto>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var allProducts = await _productRepo.FindAsync(p => p.IsActive, cancellationToken);
        var products = allProducts
            .Where(p => p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        p.BrandName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        p.CategoryName.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var topProducts = products.Take(5).Select(MapToCardDto).ToList();

        var suggestions = new List<string> { kw };
        if (topProducts.Count > 0)
        {
            suggestions.AddRange(topProducts.Select(p => p.Name).Take(3));
        }

        var result = new QuickSearchResultDto
        {
            Suggestions = suggestions.Distinct().ToList(),
            Products = topProducts
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), cancellationToken);
        return result;
    }

    public async Task<ProductComparisonDto?> CompareProductsAsync(string slug1, string slug2, CancellationToken cancellationToken = default)
    {
        var p1 = await GetProductBySlugAsync(slug1, cancellationToken);
        var p2 = await GetProductBySlugAsync(slug2, cancellationToken);
        if (p1 == null || p2 == null) return null;

        var dto = new ProductComparisonDto
        {
            Product1 = p1,
            Product2 = p2
        };

        // 1. General comparison rows
        dto.Rows.Add(new ComparisonRowDto { GroupName = "Tổng quan", SpecName = "Giá niêm yết", Value1 = $"{p1.Price:N0}₫", Value2 = $"{p2.Price:N0}₫" });
        dto.Rows.Add(new ComparisonRowDto { GroupName = "Tổng quan", SpecName = "Thương hiệu", Value1 = p1.BrandName, Value2 = p2.BrandName });
        dto.Rows.Add(new ComparisonRowDto { GroupName = "Tổng quan", SpecName = "Bảo hành chính hãng", Value1 = $"{p1.WarrantyMonths} tháng", Value2 = $"{p2.WarrantyMonths} tháng" });
        dto.Rows.Add(new ComparisonRowDto { GroupName = "Tổng quan", SpecName = "Chính sách trả góp", Value1 = p1.InstallmentText, Value2 = p2.InstallmentText });
        dto.Rows.Add(new ComparisonRowDto { GroupName = "Tổng quan", SpecName = "Tình trạng kho hàng", Value1 = p1.InStock ? "Còn hàng" : "Tạm hết hàng", Value2 = p2.InStock ? "Còn hàng" : "Tạm hết hàng" });

        // 2. Map through all technical specifications
        var specDict1 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in p1.Specifications)
        {
            foreach (var item in group.Items)
            {
                specDict1[item.Name] = item.Value;
            }
        }

        var specDict2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in p2.Specifications)
        {
            foreach (var item in group.Items)
            {
                specDict2[item.Name] = item.Value;
            }
        }

        var allSpecNames = specDict1.Keys.Union(specDict2.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var name in allSpecNames)
        {
            specDict1.TryGetValue(name, out var val1);
            specDict2.TryGetValue(name, out var val2);

            dto.Rows.Add(new ComparisonRowDto
            {
                GroupName = "Thông số kỹ thuật chi tiết",
                SpecName = name,
                Value1 = val1 ?? "Chưa có thông số",
                Value2 = val2 ?? "Chưa có thông số"
            });
        }

        return dto;
    }

    public async Task<IReadOnlyList<ProductReviewItemDto>> GetProductReviewsAsync(string productId, CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewRepo.FindAsync(r => r.ProductId == productId && r.IsApproved, cancellationToken);
        return reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductReviewItemDto
            {
                Id = r.Id,
                UserName = r.UserName,
                Rating = r.Rating,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                IsVerifiedBuyer = r.IsVerifiedBuyer,
                StaffReply = r.StaffReply,
                StaffReplyAt = r.StaffReplyAt
            })
            .ToList();
    }

    public async Task<bool> SubmitReviewAsync(SubmitReviewDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductId) || string.IsNullOrWhiteSpace(dto.Content) || string.IsNullOrWhiteSpace(dto.UserName))
        {
            return false;
        }

        var product = await _productRepo.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null) return false;

        var review = new Review
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UserId = "guest",
            UserName = dto.UserName.Trim(),
            UserPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Content = dto.Content.Trim(),
            IsVerifiedBuyer = true,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepo.AddAsync(review, cancellationToken);

        // Recalculate average rating & rating count
        var allReviews = await _reviewRepo.FindAsync(r => r.ProductId == product.Id && r.IsApproved, cancellationToken);
        if (allReviews.Count > 0)
        {
            product.RatingAverage = Math.Round(allReviews.Average(r => r.Rating), 1);
            product.RatingCount = allReviews.Count;
            await _productRepo.UpdateAsync(product, cancellationToken);
        }

        // Invalidate cache
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        return true;
    }

    private static ProductCardDto MapToCardDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Sku = p.Sku,
        CategoryName = p.CategoryName,
        BrandName = p.BrandName,
        ThumbnailUrl = p.ThumbnailUrl,
        OriginalPrice = p.OriginalPrice,
        Price = p.Price,
        RatingAverage = p.RatingAverage,
        RatingCount = p.RatingCount,
        AvailableQuantity = p.AvailableQuantity,
        SoldCount = p.SoldCount,
        HighlightSpecs = p.HighlightSpecs,
        IsFeatured = p.IsFeatured,
        IsNewArrival = p.IsNewArrival
    };

    private static ProductDetailDto MapToDetailDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Sku = p.Sku,
        CategoryId = p.CategoryId,
        CategoryName = p.CategoryName,
        BrandId = p.BrandId,
        BrandName = p.BrandName,
        ThumbnailUrl = p.ThumbnailUrl,
        ImageUrls = p.Images.Select(img => img.Url).DefaultIfEmpty(p.ThumbnailUrl).ToList(),
        OriginalPrice = p.OriginalPrice,
        Price = p.Price,
        WarrantyMonths = p.WarrantyMonths,
        AvailableQuantity = p.AvailableQuantity,
        RatingAverage = p.RatingAverage,
        RatingCount = p.RatingCount,
        ShortDescription = p.ShortDescription,
        Description = p.Description,
        HighlightSpecs = p.HighlightSpecs,
        Variants = p.Variants.Select(v => new ProductVariantDto
        {
            Sku = v.Sku,
            VariantName = v.VariantName,
            Color = v.Color,
            ColorHex = v.ColorHex,
            Storage = v.Storage,
            Ram = v.Ram,
            OriginalPrice = v.OriginalPrice,
            Price = v.Price,
            AvailableQuantity = v.AvailableQuantity,
            ImageUrl = v.ImageUrl
        }).ToList(),
        Specifications = p.Specifications.Select(g => new SpecificationGroupDto
        {
            GroupName = g.GroupName,
            Items = g.Items.Select(i => new SpecificationItemDto
            {
                Name = i.Name,
                Value = i.Value
            }).ToList()
        }).ToList()
    };
}
