using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff,WarehouseStaff")]
public class AttributesController : AdminBaseController
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly ICacheService _cache;

    public AttributesController(
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        ICacheService cache)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _auditLogRepo = auditLogRepo;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? productId = null,
        string? search = null,
        string tab = "product_specs",
        CancellationToken cancellationToken = default)
    {
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var allCategories = await _categoryRepo.GetAllAsync(cancellationToken);

        var query = allProducts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var productList = query.OrderBy(p => p.Name).ToList();

        Product? selectedProduct = null;
        if (!string.IsNullOrWhiteSpace(productId))
        {
            selectedProduct = allProducts.FirstOrDefault(p => p.Id == productId);
        }

        if (selectedProduct == null && productList.Any())
        {
            selectedProduct = productList.FirstOrDefault(p => p.Specifications.Any()) ?? productList.First();
        }

        var templates = GetStandardElectronicsTemplates();

        var model = new AttributesViewModel
        {
            Products = productList,
            Categories = allCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList(),
            SelectedProduct = selectedProduct,
            SelectedProductId = selectedProduct?.Id,
            SearchKeyword = search,
            ActiveTab = tab.ToLowerInvariant() == "templates" ? "templates" : "product_specs",
            TotalProductsWithSpecs = allProducts.Count(p => p.Specifications != null && p.Specifications.Any()),
            TotalProductsWithoutSpecs = allProducts.Count(p => p.Specifications == null || !p.Specifications.Any()),
            TotalStandardTemplates = templates.Count,
            PredefinedTemplates = templates
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> SaveProductSpecs(
        [FromForm] SaveProductSpecsInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.ProductId))
        {
            TempData["ErrorMessage"] = "Không tìm thấy thiết bị cần lưu thông số kỹ thuật.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _productRepo.GetByIdAsync(input.ProductId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong hệ thống.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(input.SpecsJson))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsedGroups = JsonSerializer.Deserialize<List<SpecificationGroup>>(input.SpecsJson, options);
                if (parsedGroups != null)
                {
                    // Clean up empty items
                    product.Specifications = parsedGroups
                        .Where(g => !string.IsNullOrWhiteSpace(g.GroupName))
                        .Select(g => new SpecificationGroup
                        {
                            GroupName = g.GroupName.Trim(),
                            Items = g.Items
                                .Where(i => !string.IsNullOrWhiteSpace(i.Name) && !string.IsNullOrWhiteSpace(i.Value))
                                .Select(i => new SpecificationItem { Name = i.Name.Trim(), Value = i.Value.Trim() })
                                .ToList()
                        })
                        .Where(g => g.Items.Any())
                        .ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(input.HighlightSpecsText))
            {
                product.HighlightSpecs = input.HighlightSpecsText
                    .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct()
                    .Take(8)
                    .ToList();
            }
            else
            {
                product.HighlightSpecs.Clear();
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _productRepo.UpdateAsync(product, cancellationToken);
            await _cache.RemoveByPrefixAsync("products:", cancellationToken);

            await _auditLogRepo.AddAsync(new SecurityAuditLog
            {
                Action = "UpdateProductSpecifications",
                Module = "Catalog",
                UserEmail = User.Identity?.Name ?? "Staff",
                Details = $"Cập nhật bộ thông số kỹ thuật đặc thù ({product.Specifications.Count} nhóm) cho thiết bị [{product.Name}]"
            }, cancellationToken);

            TempData["SuccessMessage"] = $"Đã lưu thành công bộ thông số kỹ thuật cho '{product.Name}'!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi khi xử lý dữ liệu thông số kỹ thuật: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { productId = input.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> ApplyTemplate(
        string productId,
        string templateSlug,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        var templates = GetStandardElectronicsTemplates();
        var template = templates.FirstOrDefault(t => t.CategorySlug.Equals(templateSlug, StringComparison.OrdinalIgnoreCase));
        if (template == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy bộ thông số mẫu yêu cầu.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        // Apply template structure, keeping existing values if names match
        var existingSpecDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in product.Specifications)
        {
            foreach (var item in group.Items)
            {
                existingSpecDict[item.Name] = item.Value;
            }
        }

        var newGroups = new List<SpecificationGroup>();
        foreach (var group in template.Groups)
        {
            var newGroup = new SpecificationGroup
            {
                GroupName = group.GroupName,
                Items = new List<SpecificationItem>()
            };

            foreach (var item in group.Items)
            {
                var val = existingSpecDict.TryGetValue(item.Name, out var existingVal)
                    ? existingVal
                    : item.Value;

                newGroup.Items.Add(new SpecificationItem
                {
                    Name = item.Name,
                    Value = val
                });
            }

            newGroups.Add(newGroup);
        }

        product.Specifications = newGroups;

        if (!product.HighlightSpecs.Any() && template.SuggestedHighlightSpecs.Any())
        {
            product.HighlightSpecs = template.SuggestedHighlightSpecs.ToList();
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "ApplySpecificationTemplate",
            Module = "Catalog",
            UserEmail = User.Identity?.Name ?? "Staff",
            Details = $"Áp dụng bộ thông số mẫu [{template.CategoryName}] cho thiết bị [{product.Name}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã áp dụng khung thông số kỹ thuật chuẩn '{template.CategoryName}' cho '{product.Name}'!";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,SalesStaff")]
    public async Task<IActionResult> QuickAddSpecItem(
        [FromForm] QuickAddSpecItemInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.ProductId) || string.IsNullOrWhiteSpace(input.SpecName) || string.IsNullOrWhiteSpace(input.SpecValue))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên thông số và giá trị.";
            return RedirectToAction(nameof(Index), new { productId = input.ProductId });
        }

        var product = await _productRepo.GetByIdAsync(input.ProductId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        var targetGroupName = string.IsNullOrWhiteSpace(input.GroupName) ? "Thông số chung" : input.GroupName.Trim();
        var targetGroup = product.Specifications.FirstOrDefault(g => g.GroupName.Equals(targetGroupName, StringComparison.OrdinalIgnoreCase));

        if (targetGroup == null)
        {
            targetGroup = new SpecificationGroup { GroupName = targetGroupName };
            product.Specifications.Add(targetGroup);
        }

        var existingItem = targetGroup.Items.FirstOrDefault(i => i.Name.Equals(input.SpecName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
        {
            existingItem.Value = input.SpecValue.Trim();
        }
        else
        {
            targetGroup.Items.Add(new SpecificationItem
            {
                Name = input.SpecName.Trim(),
                Value = input.SpecValue.Trim()
            });
        }

        if (input.IsHighlight && !product.HighlightSpecs.Contains(input.SpecValue.Trim()))
        {
            product.HighlightSpecs.Add(input.SpecValue.Trim());
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        TempData["SuccessMessage"] = $"Đã thêm thông số '{input.SpecName}: {input.SpecValue}' vào nhóm '{targetGroupName}'.";
        return RedirectToAction(nameof(Index), new { productId = input.ProductId });
    }

    private static List<CategorySpecTemplate> GetStandardElectronicsTemplates()
    {
        return new List<CategorySpecTemplate>
        {
            new()
            {
                CategorySlug = "dien-thoai",
                CategoryName = "Điện thoại thông minh (Smartphones)",
                Icon = "bi-phone",
                Description = "Bộ thông số chuẩn công nghiệp cho flagship & smartphone (Apple iPhone, Samsung Galaxy, Xiaomi...)",
                SuggestedHighlightSpecs = new List<string> { "OLED 6.7 inch 120Hz", "Apple A18 Pro 6 nhân", "RAM 8GB", "Camera 48MP Zoom 5x", "Pin 4422 mAh" },
                Groups = new List<SpecificationGroup>
                {
                    new()
                    {
                        GroupName = "Màn hình & Hiển thị",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Kích thước màn hình", Value = "6.7 inch" },
                            new() { Name = "Công nghệ màn hình", Value = "Super Retina XDR OLED" },
                            new() { Name = "Độ phân giải màn hình", Value = "2796 x 1290 Pixels" },
                            new() { Name = "Tần số quét", Value = "120Hz ProMotion" },
                            new() { Name = "Độ sáng tối đa", Value = "2000 nits ngoài trời" }
                        }
                    },
                    new()
                    {
                        GroupName = "Hiệu năng & Phần cứng (CPU / RAM)",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Chip xử lý (CPU)", Value = "Apple A18 Pro 6 nhân" },
                            new() { Name = "Chip đồ họa (GPU)", Value = "Apple GPU 6 nhân" },
                            new() { Name = "Dung lượng RAM", Value = "8GB LPDDR5X" },
                            new() { Name = "Bộ nhớ trong (ROM)", Value = "256GB NVMe" },
                            new() { Name = "Hệ điều hành", Value = "iOS 18" }
                        }
                    },
                    new()
                    {
                        GroupName = "Camera & Quay phim",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Camera sau", Value = "Chính 48MP & Phụ 48MP, 12MP" },
                            new() { Name = "Camera trước", Value = "12MP TrueDepth" },
                            new() { Name = "Quay video", Value = "4K 120fps Dolby Vision, ProRes LOG" }
                        }
                    },
                    new()
                    {
                        GroupName = "Pin & Công nghệ sạc",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Dung lượng pin", Value = "4422 mAh" },
                            new() { Name = "Công nghệ sạc", Value = "Sạc nhanh 25W, Sạc không dây MagSafe 25W" },
                            new() { Name = "Cổng kết nối", Value = "Type-C (USB 3.0 tốc độ 10Gbps)" }
                        }
                    },
                    new()
                    {
                        GroupName = "Tiện ích & Thiết kế",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Kháng nước kháng bụi", Value = "IP68 (sâu 6m trong 30 phút)" },
                            new() { Name = "Bảo mật", Value = "Nhận diện khuôn mặt Face ID" },
                            new() { Name = "Chất liệu khung viền", Value = "Khung viền Titan chuẩn hàng không vũ trụ" },
                            new() { Name = "Trọng lượng", Value = "227 g" }
                        }
                    }
                }
            },
            new()
            {
                CategorySlug = "laptop",
                CategoryName = "Laptop & Máy tính xách tay",
                Icon = "bi-laptop",
                Description = "Bộ thông số đặc thù cho Laptop Gaming, Đồ họa, Ultrabook văn phòng (Asus ROG, MacBook, Dell XPS...)",
                SuggestedHighlightSpecs = new List<string> { "Core i9-14900HX / M3 Pro", "RAM 32GB DDR5", "SSD 1TB NVMe Gen4", "RTX 4070 8GB GDDR6", "Màn 2.5K 240Hz 100% sRGB" },
                Groups = new List<SpecificationGroup>
                {
                    new()
                    {
                        GroupName = "Bộ vi xử lý (CPU)",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Bộ vi xử lý (CPU)", Value = "Intel Core i9-14900HX" },
                            new() { Name = "Số nhân & Số luồng", Value = "24 nhân, 32 luồng" },
                            new() { Name = "Tốc độ tối đa", Value = "Turbo boost lên đến 5.8 GHz" },
                            new() { Name = "Bộ nhớ đệm (Cache)", Value = "36MB Intel Smart Cache" }
                        }
                    },
                    new()
                    {
                        GroupName = "Bộ nhớ RAM & Ổ cứng",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Dung lượng RAM", Value = "32GB (2 x 16GB)" },
                            new() { Name = "Chuẩn RAM & Tốc độ bus", Value = "DDR5 5600MHz" },
                            new() { Name = "Khả năng nâng cấp RAM", Value = "2 khe cắm, tối đa 64GB" },
                            new() { Name = "Ổ cứng lưu trữ", Value = "1TB SSD M.2 PCIe Gen 4.0 NVMe" }
                        }
                    },
                    new()
                    {
                        GroupName = "Màn hình & Card đồ họa (VGA)",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Kích thước màn hình", Value = "16.0 inch QHD+ (2560 x 1600)" },
                            new() { Name = "Tần số quét màn hình", Value = "240Hz, G-Sync, 3ms" },
                            new() { Name = "Độ phủ màu", Value = "100% DCI-P3, 500 nits" },
                            new() { Name = "Card đồ họa rời (VGA)", Value = "NVIDIA GeForce RTX 4070 8GB GDDR6 (TGP 140W)" }
                        }
                    },
                    new()
                    {
                        GroupName = "Pin, Cổng kết nối & Trọng lượng",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Dung lượng pin", Value = "90 Wh Li-ion" },
                            new() { Name = "Cổng giao tiếp", Value = "Thunderbolt 4, HDMI 2.1, 2.5G LAN, Audio Jack" },
                            new() { Name = "Bàn phím", Value = "RGB Per-Key Aura Sync, N-key Rollover" },
                            new() { Name = "Trọng lượng", Value = "2.38 kg" }
                        }
                    }
                }
            },
            new()
            {
                CategorySlug = "may-tinh-bang",
                CategoryName = "Máy tính bảng (Tablets)",
                Icon = "bi-tablet",
                Description = "Thông số Apple iPad Pro, iPad Air, Samsung Galaxy Tab cho sáng tạo & văn phòng",
                SuggestedHighlightSpecs = new List<string> { "Apple M4 10-core", "Ultra Retina XDR Tandem OLED", "RAM 16GB", "Apple Pencil Pro", "Độ mỏng 5.1mm" },
                Groups = new List<SpecificationGroup>
                {
                    new()
                    {
                        GroupName = "Màn hình & Bút cảm ứng",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Kích thước màn hình", Value = "13.0 inch Tandem OLED" },
                            new() { Name = "Độ phân giải màn hình", Value = "2752 x 2064 Pixels" },
                            new() { Name = "Hỗ trợ bút cảm ứng", Value = "Apple Pencil Pro / S-Pen với cảm biến haptic" }
                        }
                    },
                    new()
                    {
                        GroupName = "Vi xử lý & Hiệu năng",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Chip xử lý (CPU)", Value = "Apple M4 (9 nhân CPU, 10 nhân GPU)" },
                            new() { Name = "Dung lượng RAM", Value = "16GB Unified Memory" },
                            new() { Name = "Bộ nhớ trong", Value = "512GB" }
                        }
                    },
                    new()
                    {
                        GroupName = "Âm thanh & Pin",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Hệ thống âm thanh", Value = "4 loa stereo chuẩn trường âm thanh không gian" },
                            new() { Name = "Thời lượng pin", Value = "Lướt web liên tục 10 giờ" },
                            new() { Name = "Cổng sạc", Value = "Thunderbolt / USB 4" }
                        }
                    }
                }
            },
            new()
            {
                CategorySlug = "tai-nghe-am-thanh",
                CategoryName = "Tai nghe & Âm thanh",
                Icon = "bi-headphones",
                Description = "Tai nghe True Wireless, Over-ear chống ồn, loa Bluetooth cao cấp",
                SuggestedHighlightSpecs = new List<string> { "Chống ồn ANC thông minh", "Driver Dynamic 30mm", "Pin 30 giờ", "Hi-Res Audio LDAC", "Bluetooth 5.3" },
                Groups = new List<SpecificationGroup>
                {
                    new()
                    {
                        GroupName = "Công nghệ Âm thanh & Khử ồn",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Màng loa (Driver)", Value = "Dynamic 30mm tinh thể lỏng Polymer" },
                            new() { Name = "Chống ồn chủ động (ANC)", Value = "Bộ xử lý tích hợp V1 & HD QN1" },
                            new() { Name = "Chuẩn âm thanh", Value = "Hi-Res Audio Wireless, LDAC, DSEE Extreme" }
                        }
                    },
                    new()
                    {
                        GroupName = "Pin & Kết nối",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Thời lượng pin", Value = "30 giờ (bật ANC), 40 giờ (tắt ANC)" },
                            new() { Name = "Thời gian sạc", Value = "Sạc nhanh 3 phút nghe được 3 giờ" },
                            new() { Name = "Phiên bản Bluetooth", Value = "Bluetooth 5.3 (Kết nối 2 thiết bị cùng lúc)" },
                            new() { Name = "Cổng sạc", Value = "USB Type-C" }
                        }
                    }
                }
            },
            new()
            {
                CategorySlug = "dong-ho-thong-minh",
                CategoryName = "Đồng hồ thông minh (Smartwatches)",
                Icon = "bi-smartwatch",
                Description = "Apple Watch, Galaxy Watch đo nhịp tim, SpO2, ECG, GPS tần số kép",
                SuggestedHighlightSpecs = new List<string> { "Đo điện tâm đồ ECG", "SpO2 & Nhịp tim", "Độ sáng 3000 nits", "Khung Titan 49mm", "Chống nước 100m" },
                Groups = new List<SpecificationGroup>
                {
                    new()
                    {
                        GroupName = "Màn hình & Thiết kế",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Kích thước mặt", Value = "49 mm" },
                            new() { Name = "Công nghệ màn hình", Value = "OLED Always-On 3000 nits, kính Sapphire" },
                            new() { Name = "Chất liệu vỏ", Value = "Khung viền Titan cấp 4" }
                        }
                    },
                    new()
                    {
                        GroupName = "Cảm biến sức khỏe & Tiện ích",
                        Items = new List<SpecificationItem>
                        {
                            new() { Name = "Cảm biến nhịp tim & ECG", Value = "Cảm biến quang học thế hệ 3, Điện tâm đồ" },
                            new() { Name = "Đo oxy máu (SpO2)", Value = "Theo dõi liên tục ngày đêm" },
                            new() { Name = "Định vị GPS", Value = "GPS băng tần kép L1 và L5 chính xác cao" },
                            new() { Name = "Kháng nước", Value = "Độ sâu 100 mét (Chuẩn lặn biển EN13319)" },
                            new() { Name = "Thời lượng pin", Value = "36 giờ ở chế độ thường, 72 giờ tiết kiệm pin" }
                        }
                    }
                }
            }
        };
    }
}
