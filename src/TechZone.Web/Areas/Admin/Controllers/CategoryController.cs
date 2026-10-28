using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff")]
public class CategoryController : AdminBaseController
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Brand> _brandRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly ICacheService _cache;

    public CategoryController(
        IRepository<Category> categoryRepo,
        IRepository<Product> productRepo,
        IRepository<Brand> brandRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        ICacheService cache)
    {
        _categoryRepo = categoryRepo;
        _productRepo = productRepo;
        _brandRepo = brandRepo;
        _auditLogRepo = auditLogRepo;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string tab = "categories",
        CancellationToken cancellationToken = default)
    {
        var allCategories = await _categoryRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var allBrands = await _brandRepo.GetAllAsync(cancellationToken);

        // Group products by category ID for fast count lookups
        var productCountByCat = allProducts
            .Where(p => !string.IsNullOrEmpty(p.CategoryId))
            .GroupBy(p => p.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        var categoryMap = allCategories.ToDictionary(c => c.Id, c => c.Name);

        var categoryDtos = allCategories.Select(c => new CategoryItemDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ParentId = c.ParentId,
            ParentName = !string.IsNullOrEmpty(c.ParentId) && categoryMap.TryGetValue(c.ParentId, out var parentName) ? parentName : null,
            Level = c.Level,
            IconUrl = c.IconUrl,
            ImageUrl = c.ImageUrl,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive,
            ProductCount = productCountByCat.TryGetValue(c.Id, out var cnt) ? cnt : c.ProductCount,
            CreatedAt = c.CreatedAt
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            categoryDtos = categoryDtos.Where(c =>
                c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.Slug.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        categoryDtos = categoryDtos
            .OrderBy(c => c.ParentId != null) // Root first
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToList();

        var model = new CategoryManagementViewModel
        {
            Categories = categoryDtos,
            ParentCategories = allCategories.Where(c => c.Level == 1 || string.IsNullOrEmpty(c.ParentId)).OrderBy(c => c.DisplayOrder).ToList(),
            Brands = allBrands.OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name).ToList(),

            TotalCategories = allCategories.Count,
            RootCategoriesCount = allCategories.Count(c => c.Level == 1 || string.IsNullOrEmpty(c.ParentId)),
            SubCategoriesCount = allCategories.Count(c => c.Level > 1 || !string.IsNullOrEmpty(c.ParentId)),
            ActiveCategoriesCount = allCategories.Count(c => c.IsActive),
            TotalBrands = allBrands.Count,
            TotalProductsAcrossCategories = allProducts.Count,

            SearchQuery = search,
            ActiveTab = tab.ToLowerInvariant() == "brands" ? "brands" : "categories"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(
        [FromForm] CreateCategoryInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập tên danh mục sản phẩm.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? GenerateSlug(input.Name) : GenerateSlug(input.Slug);

        // Check unique slug
        var allCategories = await _categoryRepo.GetAllAsync(cancellationToken);
        if (allCategories.Any(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        int level = 1;
        var ancestors = new List<CategoryAncestor>();
        if (!string.IsNullOrWhiteSpace(input.ParentId))
        {
            var parent = allCategories.FirstOrDefault(c => c.Id == input.ParentId);
            if (parent != null)
            {
                level = parent.Level + 1;
                ancestors.Add(new CategoryAncestor
                {
                    Id = parent.Id,
                    Name = parent.Name,
                    Slug = parent.Slug
                });
            }
        }

        var category = new Category
        {
            Name = input.Name.Trim(),
            Slug = slug,
            Description = input.Description?.Trim(),
            ParentId = string.IsNullOrWhiteSpace(input.ParentId) ? null : input.ParentId,
            Level = level,
            Ancestors = ancestors,
            IconUrl = string.IsNullOrWhiteSpace(input.IconUrl) ? "bi-folder" : input.IconUrl.Trim(),
            ImageUrl = input.ImageUrl?.Trim(),
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
            ProductCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepo.AddAsync(category, cancellationToken);
        await _cache.RemoveByPrefixAsync("categories:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateCategory",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Tạo mới danh mục [{category.Name}] (Slug: {category.Slug}, Cấp: {category.Level})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo thành công danh mục [{category.Name}]!";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(
        [FromForm] UpdateCategoryInputModel input,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(input.Id, cancellationToken);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy danh mục cần chỉnh sửa.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["ErrorMessage"] = "Tên danh mục không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? GenerateSlug(input.Name) : GenerateSlug(input.Slug);

        // Prevent setting itself as parent
        if (input.ParentId == input.Id)
        {
            input.ParentId = null;
        }

        int level = 1;
        var ancestors = new List<CategoryAncestor>();
        if (!string.IsNullOrWhiteSpace(input.ParentId))
        {
            var parent = await _categoryRepo.GetByIdAsync(input.ParentId, cancellationToken);
            if (parent != null)
            {
                level = parent.Level + 1;
                ancestors.Add(new CategoryAncestor
                {
                    Id = parent.Id,
                    Name = parent.Name,
                    Slug = parent.Slug
                });
            }
        }

        category.Name = input.Name.Trim();
        category.Slug = slug;
        category.Description = input.Description?.Trim();
        category.ParentId = string.IsNullOrWhiteSpace(input.ParentId) ? null : input.ParentId;
        category.Level = level;
        category.Ancestors = ancestors;
        if (!string.IsNullOrWhiteSpace(input.IconUrl)) category.IconUrl = input.IconUrl.Trim();
        category.ImageUrl = input.ImageUrl?.Trim();
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepo.UpdateAsync(category, cancellationToken);
        await _cache.RemoveByPrefixAsync("categories:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UpdateCategory",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Cập nhật thông tin danh mục [{category.Name}] (ID: {category.Id})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật danh mục [{category.Name}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy danh mục cần xóa.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        // Safety check 1: Check if any products exist in this category
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var productCount = allProducts.Count(p => p.CategoryId == id);
        if (productCount > 0)
        {
            TempData["ErrorMessage"] = $"Không thể xóa danh mục [{category.Name}] vì đang có {productCount} sản phẩm trực thuộc. Vui lòng chuyển sản phẩm sang danh mục khác trước.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        // Safety check 2: Check if any subcategories exist
        var allCategories = await _categoryRepo.GetAllAsync(cancellationToken);
        var childCount = allCategories.Count(c => c.ParentId == id);
        if (childCount > 0)
        {
            TempData["ErrorMessage"] = $"Không thể xóa danh mục [{category.Name}] vì đang có {childCount} danh mục con cấp dưới. Vui lòng xóa hoặc di chuyển các danh mục con trước.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        await _categoryRepo.DeleteAsync(category.Id, softDelete: true, cancellationToken);
        await _cache.RemoveByPrefixAsync("categories:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DeleteCategory",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xóa hoàn toàn danh mục [{category.Name}] khỏi hệ thống"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa danh mục [{category.Name}] thành công.";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleActive(
        string id,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await _categoryRepo.UpdateAsync(category, cancellationToken);
        await _cache.RemoveByPrefixAsync("categories:", cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(category.IsActive ? "kích hoạt" : "ẩn")} danh mục [{category.Name}].";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateBrand(
        [FromForm] CreateBrandInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập tên thương hiệu / Hãng sản xuất.";
            return RedirectToAction(nameof(Index), new { tab = "brands" });
        }

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? GenerateSlug(input.Name) : GenerateSlug(input.Slug);

        var brand = new Brand
        {
            Name = input.Name.Trim(),
            Slug = slug,
            LogoUrl = input.LogoUrl?.Trim(),
            Description = input.Description?.Trim(),
            CountryOfOrigin = input.CountryOfOrigin?.Trim(),
            WebsiteUrl = input.WebsiteUrl?.Trim(),
            DisplayOrder = input.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _brandRepo.AddAsync(brand, cancellationToken);
        await _cache.RemoveByPrefixAsync("brands:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateBrand",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Thêm hãng sản xuất đối tác [{brand.Name}] ({brand.CountryOfOrigin})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã thêm hãng sản xuất [{brand.Name}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "brands" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleBrandActive(
        string id,
        CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepo.GetByIdAsync(id, cancellationToken);
        if (brand == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
            return RedirectToAction(nameof(Index), new { tab = "brands" });
        }

        brand.IsActive = !brand.IsActive;
        brand.UpdatedAt = DateTime.UtcNow;
        await _brandRepo.UpdateAsync(brand, cancellationToken);
        await _cache.RemoveByPrefixAsync("brands:", cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(brand.IsActive ? "kích hoạt" : "vô hiệu hóa")} đối tác [{brand.Name}].";
        return RedirectToAction(nameof(Index), new { tab = "brands" });
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalizedString = text.Trim().Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var clean = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        clean = clean.Replace("đ", "d").Replace("Đ", "d");
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", "-").Trim('-');

        return clean;
    }
}
