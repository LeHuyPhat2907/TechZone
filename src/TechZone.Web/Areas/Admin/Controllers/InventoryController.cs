using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.Common.Models;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,WarehouseStaff")]
public class InventoryController : AdminBaseController
{
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<StockSlip> _stockSlipRepo;
    private readonly IRepository<InventoryTransaction> _transactionRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly ICacheService _cache;

    public InventoryController(
        IRepository<InventoryItem> inventoryRepo,
        IRepository<Product> productRepo,
        IRepository<Supplier> supplierRepo,
        IRepository<StockSlip> stockSlipRepo,
        IRepository<InventoryTransaction> transactionRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        ICacheService cache)
    {
        _inventoryRepo = inventoryRepo;
        _productRepo = productRepo;
        _supplierRepo = supplierRepo;
        _stockSlipRepo = stockSlipRepo;
        _transactionRepo = transactionRepo;
        _auditLogRepo = auditLogRepo;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "imei",
        string? search = null,
        InventoryStatus? status = null,
        string? productId = null,
        string? supplierId = null,
        StockSlipType? slipType = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 15;
        page = Math.Max(1, page);

        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var allSuppliers = await _supplierRepo.GetAllAsync(cancellationToken);
        var allSlips = await _stockSlipRepo.GetAllAsync(cancellationToken);

        // Calculate KPI Counts
        var totalItems = allItems.Count;
        var inStockCount = allItems.Count(i => i.Status == InventoryStatus.InStock);
        var soldCount = allItems.Count(i => i.Status == InventoryStatus.Delivered || i.Status == InventoryStatus.Shipped);
        var damagedCount = allItems.Count(i => i.Status == InventoryStatus.Defective);
        var reservedCount = allItems.Count(i => i.Status == InventoryStatus.Reserved);

        var totalInventoryValue = allItems.Where(i => i.Status == InventoryStatus.InStock).Sum(i => i.CostPrice);
        var totalSupplierDebt = allSuppliers.Where(s => s.IsActive).Sum(s => s.TotalDebt);
        var lowStockCount = allProducts.Count(p => p.StockQuantity <= 5 && p.IsActive);
        var totalSlipsCount = allSlips.Count;

        // Build Stock Overview (Mục 1.2: inventory.view)
        var defectiveMap = allItems
            .Where(i => i.Status == InventoryStatus.Defective)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Count());

        var stockOverview = allProducts
            .OrderBy(p => p.StockQuantity)
            .Select(p => new ProductStockOverviewDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                CategoryName = p.CategoryName,
                BrandName = p.BrandName,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                AvailableQuantity = p.AvailableQuantity,
                ReservedQuantity = p.ReservedQuantity,
                DefectiveCount = defectiveMap.GetValueOrDefault(p.Id, 0)
            })
            .ToList();

        // Filter IMEI Items
        var imeiQuery = allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            imeiQuery = imeiQuery.Where(i =>
                (!string.IsNullOrEmpty(i.SerialNumber) && i.SerialNumber.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(i.Imei2) && i.Imei2.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(i.ProductName) && i.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(i.VariantSku) && i.VariantSku.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(i.SupplierName) && i.SupplierName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(i.ImportBatchCode) && i.ImportBatchCode.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        if (status.HasValue)
        {
            imeiQuery = imeiQuery.Where(i => i.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            imeiQuery = imeiQuery.Where(i => i.ProductId == productId);
        }

        var filteredImeiList = imeiQuery.OrderByDescending(i => i.CreatedAt).ToList();
        var totalImeiCount = filteredImeiList.Count;
        var pagedImeiItems = filteredImeiList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Filter Stock Slips
        var slipQuery = allSlips.AsEnumerable();
        if (slipType.HasValue)
        {
            slipQuery = slipQuery.Where(s => s.Type == slipType.Value);
        }
        if (!string.IsNullOrWhiteSpace(supplierId))
        {
            slipQuery = slipQuery.Where(s => s.SupplierId == supplierId);
        }
        var filteredSlips = slipQuery.OrderByDescending(s => s.CreatedAt).ToList();

        // Filter Suppliers
        var supplierQuery = allSuppliers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search) && tab == "suppliers")
        {
            var s = search.Trim();
            supplierQuery = supplierQuery.Where(sup =>
                (!string.IsNullOrEmpty(sup.Name) && sup.Name.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(sup.Code) && sup.Code.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(sup.TaxCode) && sup.TaxCode.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(sup.ContactPerson) && sup.ContactPerson.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }
        var filteredSuppliers = supplierQuery.OrderByDescending(s => s.TotalDebt).ToList();

        var viewModel = new InventoryManagementViewModel
        {
            ActiveTab = tab,
            Items = new PagedResult<InventoryItem>(pagedImeiItems, totalImeiCount, page, pageSize),
            Products = allProducts.OrderBy(p => p.Name).ToList(),
            StockSlips = filteredSlips,
            Suppliers = filteredSuppliers,
            StockOverview = stockOverview,
            SearchKeyword = search,
            StatusFilter = status,
            SelectedProductId = productId,
            SelectedSupplierId = supplierId,
            SlipTypeFilter = slipType,
            TotalItems = totalItems,
            InStockCount = inStockCount,
            SoldCount = soldCount,
            DamagedCount = damagedCount,
            ReservedCount = reservedCount,
            TotalInventoryValue = totalInventoryValue,
            TotalSupplierDebt = totalSupplierDebt,
            LowStockProductsCount = lowStockCount,
            TotalSlipsCount = totalSlipsCount
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(StockAdjustmentDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductId))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm cần điều chỉnh kiểm kê.";
            return RedirectToAction(nameof(Index), new { tab = "stock" });
        }

        var product = await _productRepo.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
            return RedirectToAction(nameof(Index), new { tab = "stock" });
        }

        var oldStock = product.StockQuantity;
        var newStock = Math.Max(0, dto.ActualPhysicalStock);
        var diff = newStock - oldStock;

        product.StockQuantity = newStock;
        product.AvailableQuantity = Math.Max(0, newStock - product.ReservedQuantity);
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        // Record Inventory Transaction
        var tx = new InventoryTransaction
        {
            Type = InventoryTransactionType.StockAdjustment,
            ProductId = product.Id,
            VariantSku = product.Sku,
            Quantity = Math.Abs(diff),
            PerformedByName = User.Identity?.Name ?? "Thủ kho TechZone",
            Note = $"Điều chỉnh kiểm kê kho: {(diff >= 0 ? "+" : "")}{diff} chiếc. Lý do: {dto.Reason}. Ghi chú: {dto.Note}"
        };
        await _transactionRepo.AddAsync(tx, cancellationToken);

        // Security Audit Log
        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "inventory.adjust",
            Module = "Inventory",
            UserEmail = User.Identity?.Name ?? "WarehouseStaff",
            Details = $"Kiểm kê điều chỉnh kho cho [{product.Name}] (SKU: {product.Sku}): Tồn cũ {oldStock} -> Tồn mới {newStock} (Chênh lệch: {(diff >= 0 ? "+" : "")}{diff} máy). Lý do: {dto.Reason}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật kiểm kê cho '{product.Name}': Tồn kho thực tế hiện tại là {newStock} chiếc.";
        return RedirectToAction(nameof(Index), new { tab = "stock" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportBatch(ImportStockDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductId) || string.IsNullOrWhiteSpace(dto.SerialNumbersText))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm và nhập danh sách số Serial/IMEI.";
            return RedirectToAction(nameof(Index), new { tab = "slips" });
        }

        var product = await _productRepo.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại trong hệ thống.";
            return RedirectToAction(nameof(Index), new { tab = "slips" });
        }

        Supplier? supplier = null;
        if (!string.IsNullOrWhiteSpace(dto.SupplierId))
        {
            supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, cancellationToken);
        }

        // Split Serial/IMEI by newline, comma, semicolon
        var rawSerials = dto.SerialNumbersText
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (rawSerials.Count == 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy số Serial/IMEI hợp lệ nào trong danh sách.";
            return RedirectToAction(nameof(Index), new { tab = "slips" });
        }

        var batchCode = string.IsNullOrWhiteSpace(dto.BatchCode)
            ? $"GRN-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}"
            : dto.BatchCode.Trim().ToUpperInvariant();

        var unitCost = dto.CostPrice > 0 ? dto.CostPrice : (product.OriginalPrice > 0 ? product.OriginalPrice * 0.8m : product.Price * 0.8m);
        var totalAmount = unitCost * rawSerials.Count;
        var userName = User.Identity?.Name ?? "Thủ kho TechZone";

        var newItems = rawSerials.Select(sn => new InventoryItem
        {
            SerialNumber = sn,
            ProductId = product.Id,
            ProductName = product.Name,
            VariantSku = product.Sku,
            VariantName = "Tiêu chuẩn",
            Status = InventoryStatus.InStock,
            WarehouseLocation = string.IsNullOrWhiteSpace(dto.WarehouseLocation) ? "Kệ A1-Kho Tổng" : dto.WarehouseLocation.Trim(),
            CostPrice = unitCost,
            ImportBatchCode = batchCode,
            SupplierId = supplier?.Id,
            SupplierName = supplier?.Name ?? "Nhà cung cấp đối tác",
            ReceivedDate = DateTime.UtcNow,
            Notes = dto.Note
        }).ToList();

        await _inventoryRepo.AddRangeAsync(newItems, cancellationToken);

        // Synchronize Product inventory on hand & available
        product.StockQuantity += newItems.Count;
        product.AvailableQuantity += newItems.Count;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        // Update Supplier Debt & Cumulative Import
        if (supplier != null)
        {
            supplier.TotalDebt += totalAmount;
            supplier.TotalImportAmount += totalAmount;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _supplierRepo.UpdateAsync(supplier, cancellationToken);
        }

        // Create Stock Slip (GRN)
        var slip = new StockSlip
        {
            SlipCode = batchCode,
            Type = StockSlipType.Import,
            SupplierId = supplier?.Id,
            SupplierName = supplier?.Name ?? "Đối tác phân phối",
            WarehouseLocation = string.IsNullOrWhiteSpace(dto.WarehouseLocation) ? "Kệ A1-Kho Tổng" : dto.WarehouseLocation.Trim(),
            CreatedByName = userName,
            ApprovedByName = userName,
            ApprovedAt = DateTime.UtcNow,
            Status = StockSlipStatus.Approved,
            TotalQuantity = newItems.Count,
            TotalAmount = totalAmount,
            Note = dto.Note ?? $"Nhập kho {newItems.Count} thiết bị [{product.Name}]",
            Items = new List<StockSlipItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Quantity = newItems.Count,
                    UnitPrice = unitCost,
                    SubTotal = totalAmount,
                    SerialNumbers = rawSerials
                }
            }
        };
        await _stockSlipRepo.AddAsync(slip, cancellationToken);

        // Record Transaction
        await _transactionRepo.AddAsync(new InventoryTransaction
        {
            Type = InventoryTransactionType.InboundImport,
            ProductId = product.Id,
            VariantSku = product.Sku,
            Quantity = newItems.Count,
            PerformedByName = userName,
            Note = $"Lập phiếu nhập kho GRN [{batchCode}] từ nhà cung cấp {supplier?.Name ?? "Đối tác"}."
        }, cancellationToken);

        // Security Audit Log
        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "stock.import",
            Module = "Inventory",
            UserEmail = userName,
            Details = $"Nhập kho thành công phiếu [{batchCode}]: {newItems.Count} máy [{product.Name}] - Tổng tiền: {totalAmount:N0}đ (NCC: {supplier?.Name ?? "Chưa chỉ định"})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu nhập kho GRN [{batchCode}] thành công ({newItems.Count} thiết bị). Tồn kho sản phẩm '{product.Name}' đã được cộng thêm.";
        return RedirectToAction(nameof(Index), new { tab = "slips" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExportSlip(ExportStockDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductId) || string.IsNullOrWhiteSpace(dto.SerialNumbersText))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm và nhập danh sách số Serial/IMEI cần xuất.";
            return RedirectToAction(nameof(Index), new { tab = "slips" });
        }

        var product = await _productRepo.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại trong hệ thống.";
            return RedirectToAction(nameof(Index), new { tab = "slips" });
        }

        Supplier? supplier = null;
        if (!string.IsNullOrWhiteSpace(dto.SupplierId))
        {
            supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, cancellationToken);
        }

        var rawSerials = dto.SerialNumbersText
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (rawSerials.Count == 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy mã Serial/IMEI hợp lệ để xuất kho.";
            return RedirectToAction(nameof(Index), new { tab = "slips" });
        }

        var slipCode = string.IsNullOrWhiteSpace(dto.SlipCode)
            ? $"GDN-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}"
            : dto.SlipCode.Trim().ToUpperInvariant();

        var unitPrice = dto.UnitPrice > 0 ? dto.UnitPrice : product.Price * 0.8m;
        var totalAmount = unitPrice * rawSerials.Count;
        var userName = User.Identity?.Name ?? "Thủ kho TechZone";

        // Mark items as exported / defective returned
        var exportedCount = 0;
        foreach (var sn in rawSerials)
        {
            var item = await _inventoryRepo.FindOneAsync(i => i.SerialNumber == sn, cancellationToken);
            if (item != null)
            {
                item.Status = dto.ExportReason == StockExportReason.ReturnToSupplier ? InventoryStatus.Defective : InventoryStatus.Shipped;
                item.ExportSlipCode = slipCode;
                item.ShippedDate = DateTime.UtcNow;
                item.Notes = $"Xuất kho theo phiếu {slipCode} ({dto.ExportReason}). {dto.Note}";
                item.UpdatedAt = DateTime.UtcNow;
                await _inventoryRepo.UpdateAsync(item, cancellationToken);
                exportedCount++;
            }
        }

        // Adjust Product Stock
        var qtyToDeduct = exportedCount > 0 ? exportedCount : rawSerials.Count;
        product.StockQuantity = Math.Max(0, product.StockQuantity - qtyToDeduct);
        product.AvailableQuantity = Math.Max(0, product.AvailableQuantity - qtyToDeduct);
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        // Deduct Supplier Debt if returning defective goods to supplier
        if (supplier != null && dto.ExportReason == StockExportReason.ReturnToSupplier)
        {
            supplier.TotalDebt = Math.Max(0, supplier.TotalDebt - totalAmount);
            supplier.UpdatedAt = DateTime.UtcNow;
            await _supplierRepo.UpdateAsync(supplier, cancellationToken);
        }

        // Create Stock Slip (GDN)
        var slip = new StockSlip
        {
            SlipCode = slipCode,
            Type = StockSlipType.Export,
            ExportReason = dto.ExportReason,
            SupplierId = supplier?.Id,
            SupplierName = supplier?.Name ?? "Xuất kho nội bộ",
            WarehouseLocation = string.IsNullOrWhiteSpace(dto.WarehouseLocation) ? "Kho Tổng" : dto.WarehouseLocation.Trim(),
            CreatedByName = userName,
            ApprovedByName = userName,
            ApprovedAt = DateTime.UtcNow,
            Status = StockSlipStatus.Approved,
            TotalQuantity = qtyToDeduct,
            TotalAmount = totalAmount,
            Note = dto.Note ?? $"Xuất kho {qtyToDeduct} thiết bị [{product.Name}] ({dto.ExportReason})",
            Items = new List<StockSlipItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Quantity = qtyToDeduct,
                    UnitPrice = unitPrice,
                    SubTotal = totalAmount,
                    SerialNumbers = rawSerials
                }
            }
        };
        await _stockSlipRepo.AddAsync(slip, cancellationToken);

        // Transaction
        await _transactionRepo.AddAsync(new InventoryTransaction
        {
            Type = dto.ExportReason == StockExportReason.ReturnToSupplier ? InventoryTransactionType.DefectiveReturn : InventoryTransactionType.OutboundSale,
            ProductId = product.Id,
            VariantSku = product.Sku,
            Quantity = qtyToDeduct,
            PerformedByName = userName,
            Note = $"Lập phiếu xuất kho GDN [{slipCode}] lý do: {dto.ExportReason}."
        }, cancellationToken);

        // Security Audit Log
        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "stock.export",
            Module = "Inventory",
            UserEmail = userName,
            Details = $"Xuất kho phiếu [{slipCode}]: {qtyToDeduct} máy [{product.Name}] ({dto.ExportReason}) - Tổng giá trị: {totalAmount:N0}đ"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu xuất kho GDN [{slipCode}] thành công ({qtyToDeduct} thiết bị). Tồn kho sản phẩm '{product.Name}' đã được cập nhật.";
        return RedirectToAction(nameof(Index), new { tab = "slips" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItemStatus(string itemId, InventoryStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return BadRequest();

        var item = await _inventoryRepo.GetByIdAsync(itemId, cancellationToken);
        if (item == null) return NotFound();

        var oldStatus = item.Status;
        if (oldStatus == newStatus)
        {
            return RedirectToAction(nameof(Index), new { tab = "imei" });
        }

        item.Status = newStatus;
        item.UpdatedAt = DateTime.UtcNow;
        await _inventoryRepo.UpdateAsync(item, cancellationToken);

        // Adjust product stock if changing to/from Damaged
        var product = await _productRepo.GetByIdAsync(item.ProductId, cancellationToken);
        if (product != null)
        {
            if (newStatus == InventoryStatus.Defective && oldStatus == InventoryStatus.InStock)
            {
                product.AvailableQuantity = Math.Max(0, product.AvailableQuantity - 1);
                product.StockQuantity = Math.Max(0, product.StockQuantity - 1);
                await _productRepo.UpdateAsync(product, cancellationToken);
                await _cache.RemoveByPrefixAsync("products:", cancellationToken);
            }
            else if (oldStatus == InventoryStatus.Defective && newStatus == InventoryStatus.InStock)
            {
                product.AvailableQuantity += 1;
                product.StockQuantity += 1;
                await _productRepo.UpdateAsync(product, cancellationToken);
                await _cache.RemoveByPrefixAsync("products:", cancellationToken);
            }
        }

        TempData["SuccessMessage"] = $"Đã chuyển trạng thái Serial '{item.SerialNumber}' sang '{newStatus}'.";
        return RedirectToAction(nameof(Index), new { tab = "imei" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplier(SupplierInputDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập tên nhà cung cấp và mã viết tắt.";
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        var supplier = new Supplier
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpperInvariant(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            TaxCode = dto.TaxCode?.Trim(),
            PaymentTerms = string.IsNullOrWhiteSpace(dto.PaymentTerms) ? "Net 30 ngày" : dto.PaymentTerms.Trim(),
            TotalDebt = Math.Max(0, dto.TotalDebt),
            TotalImportAmount = Math.Max(0, dto.TotalDebt),
            IsActive = true,
            Note = dto.Note?.Trim()
        };

        await _supplierRepo.AddAsync(supplier, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "supplier.manage",
            Module = "Inventory",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Thêm mới nhà cung cấp đối tác [{supplier.Name}] (Mã: {supplier.Code}, MST: {supplier.TaxCode ?? "N/A"})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã thêm nhà cung cấp đối tác '{supplier.Name}' thành công!";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSupplier(SupplierInputDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            return BadRequest();
        }

        var supplier = await _supplierRepo.GetByIdAsync(dto.Id, cancellationToken);
        if (supplier == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp.";
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        supplier.Name = dto.Name.Trim();
        supplier.Code = dto.Code.Trim().ToUpperInvariant();
        supplier.ContactPerson = dto.ContactPerson?.Trim();
        supplier.Phone = dto.Phone?.Trim();
        supplier.Email = dto.Email?.Trim();
        supplier.Address = dto.Address?.Trim();
        supplier.TaxCode = dto.TaxCode?.Trim();
        supplier.PaymentTerms = dto.PaymentTerms.Trim();
        supplier.Note = dto.Note?.Trim();
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepo.UpdateAsync(supplier, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật thông tin nhà cung cấp '{supplier.Name}' thành công!";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordSupplierPayment(SupplierPaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierId) || dto.Amount <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn nhà cung cấp và số tiền thanh toán hợp lệ (> 0đ).";
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        var supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, cancellationToken);
        if (supplier == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp.";
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        var paidAmount = dto.Amount;
        var oldDebt = supplier.TotalDebt;
        supplier.TotalDebt = Math.Max(0, supplier.TotalDebt - paidAmount);
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepo.UpdateAsync(supplier, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "supplier.manage",
            Module = "Finance",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Thanh toán công nợ {paidAmount:N0}đ cho nhà cung cấp [{supplier.Name}] qua {dto.PaymentMethod}. Nợ cũ: {oldDebt:N0}đ -> Còn lại: {supplier.TotalDebt:N0}đ. Ghi chú: {dto.Note}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã ghi nhận thanh toán {paidAmount:N0}đ cho nhà cung cấp '{supplier.Name}'. Công nợ còn lại: {supplier.TotalDebt:N0}đ.";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSupplierActive(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var supplier = await _supplierRepo.GetByIdAsync(id, cancellationToken);
        if (supplier == null) return NotFound();

        supplier.IsActive = !supplier.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _supplierRepo.UpdateAsync(supplier, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(supplier.IsActive ? "kích hoạt lại" : "tạm ngưng hợp tác với")} nhà cung cấp '{supplier.Name}'.";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }
}
