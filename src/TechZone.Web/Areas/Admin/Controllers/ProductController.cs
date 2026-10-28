using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.Common.Models;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff,WarehouseStaff")]
public class ProductController : AdminBaseController
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Brand> _brandRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly ICacheService _cache;

    public ProductController(
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        IRepository<Brand> brandRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        ICacheService cache)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _brandRepo = brandRepo;
        _auditLogRepo = auditLogRepo;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? categoryId = null,
        string? brandId = null,
        string? stockFilter = null,
        bool? isActive = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 12;
        page = Math.Max(1, page);

        var allProducts = await _productRepo.GetAllAsync(cancellationToken);

        // Calculate KPI Metrics across all inventory
        var totalProducts = allProducts.Count;
        var inStockCount = allProducts.Count(p => p.StockQuantity > 5 && p.IsActive);
        var lowStockCount = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 5 && p.IsActive);
        var outOrHidden = allProducts.Count(p => p.StockQuantity == 0 || !p.IsActive);

        // Apply filters in-memory for resilience and case-insensitivity
        var query = allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Sku) && p.Sku.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.BrandName) && p.BrandName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.CategoryName) && p.CategoryName.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(brandId))
        {
            query = query.Where(p => p.BrandId == brandId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(stockFilter) && stockFilter != "all")
        {
            switch (stockFilter)
            {
                case "in_stock":
                    query = query.Where(p => p.StockQuantity > 5);
                    break;
                case "low_stock":
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
                    break;
                case "out_of_stock":
                    query = query.Where(p => p.StockQuantity == 0);
                    break;
            }
        }

        var filteredList = query.OrderByDescending(p => p.CreatedAt).ToList();
        var totalCount = filteredList.Count;

        var pagedItems = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var categories = await _categoryRepo.GetAllAsync(cancellationToken);
        var brands = await _brandRepo.GetAllAsync(cancellationToken);

        var viewModel = new ProductManagementViewModel
        {
            Products = new PagedResult<Product>(pagedItems, totalCount, page, pageSize),
            Categories = categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList(),
            Brands = brands.Where(b => b.IsActive).OrderBy(b => b.Name).ToList(),
            SearchKeyword = search,
            SelectedCategoryId = categoryId,
            SelectedBrandId = brandId,
            StockFilter = stockFilter,
            IsActive = isActive,
            TotalProducts = totalProducts,
            InStockCount = inStockCount,
            LowStockCount = lowStockCount,
            OutOfStockOrHiddenCount = outOrHidden
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        Category? category = null;
        if (!string.IsNullOrEmpty(product.CategoryId))
        {
            category = await _categoryRepo.GetByIdAsync(product.CategoryId, cancellationToken);
        }

        Brand? brand = null;
        if (!string.IsNullOrEmpty(product.BrandId))
        {
            brand = await _brandRepo.GetByIdAsync(product.BrandId, cancellationToken);
        }

        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var related = allProducts
            .Where(p => p.Id != product.Id && (p.CategoryId == product.CategoryId || p.BrandId == product.BrandId))
            .Take(4)
            .ToList();

        var viewModel = new ProductDetailsViewModel
        {
            Product = product,
            Category = category,
            Brand = brand,
            RelatedProducts = related
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var model = new ProductEditViewModel
        {
            IsActive = true,
            IsNewArrival = true,
            WarrantyMonths = 12,
            StockQuantity = 10
        };

        await PopulateLookupsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> Create(ProductEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }

        // Check if SKU already exists
        var existingSku = await _productRepo.FindAsync(p => p.Sku.ToLower() == model.Sku.Trim().ToLower(), cancellationToken);
        if (existingSku.Any())
        {
            ModelState.AddModelError(nameof(model.Sku), $"Mã SKU '{model.Sku}' đã tồn tại trong hệ thống. Vui lòng chọn mã SKU khác.");
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }

        // Auto-generate slug if empty
        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? GenerateSlug(model.Name)
            : GenerateSlug(model.Slug);

        // Resolve Category and Brand names
        var category = await _categoryRepo.GetByIdAsync(model.CategoryId, cancellationToken);
        var brand = await _brandRepo.GetByIdAsync(model.BrandId, cancellationToken);

        // Parse Highlight Specs & Tags
        var highlightSpecs = ParseDelimitedString(model.HighlightSpecsRaw);
        var tags = ParseDelimitedString(model.TagsRaw);

        // Parse Gallery Images
        var galleryImages = ParseGalleryImages(model.GalleryImagesJson, model.ThumbnailUrl);

        var product = new Product
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Sku = model.Sku.Trim().ToUpperInvariant(),
            CategoryId = model.CategoryId,
            CategoryName = category?.Name ?? "Thiết bị",
            BrandId = model.BrandId,
            BrandName = brand?.Name ?? "Hãng",
            OriginalPrice = model.OriginalPrice,
            Price = model.Price,
            WholesalePrice = model.WholesalePrice,
            WarrantyMonths = model.WarrantyMonths,
            StockQuantity = Math.Max(0, model.StockQuantity),
            AvailableQuantity = Math.Max(0, model.StockQuantity),
            ReservedQuantity = 0,
            ShortDescription = model.ShortDescription?.Trim(),
            Description = model.Description?.Trim(),
            ThumbnailUrl = model.ThumbnailUrl.Trim(),
            VideoUrl = string.IsNullOrWhiteSpace(model.VideoUrl) ? null : model.VideoUrl.Trim(),
            Images = galleryImages,
            HighlightSpecs = highlightSpecs,
            Tags = tags,
            IsActive = model.IsActive,
            IsFeatured = model.IsFeatured,
            IsNewArrival = model.IsNewArrival,
            IsBestSeller = model.IsBestSeller,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepo.AddAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "product.create",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Tạo mới sản phẩm thiết bị [{product.Name}] (SKU: {product.Sku}) - Giá niêm yết: {product.OriginalPrice:N0}đ, Giá bán: {product.Price:N0}đ"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo mới sản phẩm '{product.Name}' thành công!";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        var galleryDtos = product.Images.Select(img => new ProductImageItemDto
        {
            Url = img.Url,
            DisplayOrder = img.DisplayOrder,
            ColorTag = img.ColorTag
        }).ToList();

        var model = new ProductEditViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            OriginalPrice = product.OriginalPrice,
            Price = product.Price,
            WholesalePrice = product.WholesalePrice,
            WarrantyMonths = product.WarrantyMonths,
            StockQuantity = product.StockQuantity,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            ThumbnailUrl = product.ThumbnailUrl,
            VideoUrl = product.VideoUrl,
            HighlightSpecsRaw = string.Join(", ", product.HighlightSpecs),
            TagsRaw = string.Join(", ", product.Tags),
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsNewArrival = product.IsNewArrival,
            IsBestSeller = product.IsBestSeller,
            GalleryImages = galleryDtos,
            GalleryImagesJson = JsonSerializer.Serialize(galleryDtos)
        };

        await PopulateLookupsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> Edit(string id, ProductEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }

        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        // Check if SKU conflicts with another product
        var existingSku = await _productRepo.FindAsync(p => p.Sku.ToLower() == model.Sku.Trim().ToLower() && p.Id != id, cancellationToken);
        if (existingSku.Any())
        {
            ModelState.AddModelError(nameof(model.Sku), $"Mã SKU '{model.Sku}' đã thuộc về một sản phẩm khác.");
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }

        // Price override audit tracking (Mục 1.1 product.price_override)
        bool priceOverridden = product.Price != model.Price || product.OriginalPrice != model.OriginalPrice || product.WholesalePrice != model.WholesalePrice;

        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? GenerateSlug(model.Name)
            : GenerateSlug(model.Slug);

        var category = await _categoryRepo.GetByIdAsync(model.CategoryId, cancellationToken);
        var brand = await _brandRepo.GetByIdAsync(model.BrandId, cancellationToken);

        var highlightSpecs = ParseDelimitedString(model.HighlightSpecsRaw);
        var tags = ParseDelimitedString(model.TagsRaw);
        var galleryImages = ParseGalleryImages(model.GalleryImagesJson, model.ThumbnailUrl);

        product.Name = model.Name.Trim();
        product.Slug = slug;
        product.Sku = model.Sku.Trim().ToUpperInvariant();
        product.CategoryId = model.CategoryId;
        product.CategoryName = category?.Name ?? product.CategoryName;
        product.BrandId = model.BrandId;
        product.BrandName = brand?.Name ?? product.BrandName;
        product.OriginalPrice = model.OriginalPrice;
        product.Price = model.Price;
        product.WholesalePrice = model.WholesalePrice;
        product.WarrantyMonths = model.WarrantyMonths;
        product.StockQuantity = Math.Max(0, model.StockQuantity);
        product.AvailableQuantity = Math.Max(0, model.StockQuantity - product.ReservedQuantity);
        product.ShortDescription = model.ShortDescription?.Trim();
        product.Description = model.Description?.Trim();
        product.ThumbnailUrl = model.ThumbnailUrl.Trim();
        product.VideoUrl = string.IsNullOrWhiteSpace(model.VideoUrl) ? null : model.VideoUrl.Trim();
        product.Images = galleryImages;
        product.HighlightSpecs = highlightSpecs;
        product.Tags = tags;
        product.IsActive = model.IsActive;
        product.IsFeatured = model.IsFeatured;
        product.IsNewArrival = model.IsNewArrival;
        product.IsBestSeller = model.IsBestSeller;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        var auditDetails = priceOverridden
            ? $"Cập nhật sản phẩm & điều chỉnh giá (product.price_override) [{product.Name}]: Giá bán lẻ mới {product.Price:N0}đ, Giá sỉ {product.WholesalePrice?.ToString("N0") ?? "N/A"}đ"
            : $"Cập nhật thông tin thiết bị, bài viết & media [{product.Name}]";

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = priceOverridden ? "product.price_override" : "product.update",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = auditDetails
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật thông tin sản phẩm '{product.Name}' thành công!";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        // Perform soft delete
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, cancellationToken);
        await _productRepo.DeleteAsync(id, softDelete: true, cancellationToken: cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "product.delete",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xóa sản phẩm thiết bị [{product.Name}] (SKU: {product.Sku})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa sản phẩm '{product.Name}' khỏi hệ thống.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        // Enforce RBAC (Mục 1.1 / 3.4 / 4.3): Chỉ SuperAdmin mới có quyền bật/tắt trạng thái kinh doanh
        if (!User.IsInRole("SuperAdmin"))
        {
            TempData["ErrorMessage"] = "Bạn không có quyền thay đổi trạng thái mở bán sản phẩm. Thao tác này thuộc thẩm quyền Quản trị viên theo đặc tả RBAC.";
            return Redirect(returnUrl ?? "/Admin/Product");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "product.toggle_status",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"{(product.IsActive ? "Kích hoạt mở bán" : "Tạm ẩn")} sản phẩm [{product.Name}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(product.IsActive ? "kích hoạt mở bán" : "tạm ẩn")} sản phẩm '{product.Name}' thành công.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateStock(string id, int stockQuantity, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        // Enforce RBAC (Mục 1.2 & 3.4): Chỉ SuperAdmin và WarehouseStaff được điều chỉnh tồn kho vật lý
        if (!User.IsInRole("SuperAdmin") && !User.IsInRole("WarehouseStaff"))
        {
            TempData["ErrorMessage"] = "Bạn không có quyền điều chỉnh tồn kho vật lý. Quyền hạn này thuộc về Thủ kho và Quản trị viên theo đặc tả RBAC.";
            return Redirect(returnUrl ?? "/Admin/Product");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var product = await _productRepo.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        var newStock = Math.Max(0, stockQuantity);
        product.StockQuantity = newStock;
        product.AvailableQuantity = Math.Max(0, newStock - product.ReservedQuantity);
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "inventory.adjust",
            Module = "Inventory",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Điều chỉnh tồn kho nhanh cho [{product.Name}]: {newStock} chiếc"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật tồn kho cho '{product.Name}': {product.StockQuantity} chiếc.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(ProductEditViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepo.GetAllAsync(cancellationToken);
        var brands = await _brandRepo.GetAllAsync(cancellationToken);

        model.Categories = categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
        model.Brands = brands.Where(b => b.IsActive).OrderBy(b => b.Name).ToList();
    }

    private static List<string> ParseDelimitedString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

        return raw
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ProductImage> ParseGalleryImages(string? json, string thumbnailUrl)
    {
        var list = new List<ProductImage>();

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var dtos = JsonSerializer.Deserialize<List<ProductImageItemDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (dtos != null)
                {
                    int order = 1;
                    foreach (var d in dtos.Where(x => !string.IsNullOrWhiteSpace(x.Url)))
                    {
                        list.Add(new ProductImage
                        {
                            Url = d.Url.Trim(),
                            DisplayOrder = d.DisplayOrder > 0 ? d.DisplayOrder : order++,
                            ColorTag = string.IsNullOrWhiteSpace(d.ColorTag) ? null : d.ColorTag.Trim(),
                            IsThumbnail = false
                        });
                    }
                }
            }
            catch
            {
                // Ignore parse errors, will use fallback
            }
        }

        // Ensure thumbnail is tagged or added if not in gallery
        if (!string.IsNullOrWhiteSpace(thumbnailUrl) && !list.Any(i => i.Url.Equals(thumbnailUrl.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            list.Insert(0, new ProductImage
            {
                Url = thumbnailUrl.Trim(),
                DisplayOrder = 0,
                IsThumbnail = true
            });
        }

        return list;
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
