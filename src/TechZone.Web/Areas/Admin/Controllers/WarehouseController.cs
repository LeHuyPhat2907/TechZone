using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,WarehouseStaff")]
public class WarehouseController : AdminBaseController
{
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<StockSlip> _stockSlipRepo;
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<StocktakeSession> _stocktakeRepo;
    private readonly IRepository<StockTransfer> _transferRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<Warranty> _warrantyRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;

    public WarehouseController(
        IRepository<PurchaseOrder> poRepo,
        IRepository<StockSlip> stockSlipRepo,
        IRepository<InventoryItem> inventoryRepo,
        IRepository<Product> productRepo,
        IRepository<Order> orderRepo,
        IRepository<StocktakeSession> stocktakeRepo,
        IRepository<StockTransfer> transferRepo,
        IRepository<Supplier> supplierRepo,
        IRepository<Warranty> warrantyRepo,
        IRepository<SecurityAuditLog> auditLogRepo)
    {
        _poRepo = poRepo;
        _stockSlipRepo = stockSlipRepo;
        _inventoryRepo = inventoryRepo;
        _productRepo = productRepo;
        _orderRepo = orderRepo;
        _stocktakeRepo = stocktakeRepo;
        _transferRepo = transferRepo;
        _supplierRepo = supplierRepo;
        _warrantyRepo = warrantyRepo;
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "inbound",
        string? poStatus = null,
        string? poSearch = null,
        string? imeiSearch = null,
        string? imeiStatus = null,
        string? imeiSku = null,
        string? outboundCarrier = null,
        string? outboundSearch = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var currentUserName = User.Identity?.Name ?? "Thủ Kho";

        var allPos = await _poRepo.GetAllAsync(cancellationToken);
        var allSlips = await _stockSlipRepo.GetAllAsync(cancellationToken);
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var allStocktake = await _stocktakeRepo.GetAllAsync(cancellationToken);
        var allTransfers = await _transferRepo.GetAllAsync(cancellationToken);
        var allSuppliers = await _supplierRepo.GetAllAsync(cancellationToken);

        // 1. KPI Operational Metrics
        var pendingPoCount = allPos.Count(p => p.Status == "Approved" || p.Status == "Inspecting");
        var inStockCount = allItems.Count(i => i.Status == InventoryStatus.InStock);
        var lowSafetyStockCount = allProducts.Count(p => p.IsActive && p.AvailableQuantity <= (p.MinStockThreshold > 0 ? p.MinStockThreshold : 5));
        var outboundPendingCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Confirmed || o.OrderStatus == OrderStatus.Preparing);
        var activeTransfersCount = allTransfers.Count(t => t.Status == "InTransit" || t.Status == "Pending");
        var damagedDefectiveCount = allItems.Count(i => i.Status == InventoryStatus.Defective || i.Status == InventoryStatus.InRepair);

        // 2. Filter Purchase Orders (Mục 3.1)
        var posQuery = allPos.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(poStatus))
        {
            posQuery = posQuery.Where(p => p.Status.Equals(poStatus, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(poSearch))
        {
            posQuery = posQuery.Where(p => 
                p.PoCode.Contains(poSearch, StringComparison.OrdinalIgnoreCase) ||
                p.SupplierName.Contains(poSearch, StringComparison.OrdinalIgnoreCase) ||
                p.Items.Any(item => item.ProductName.Contains(poSearch, StringComparison.OrdinalIgnoreCase) || item.Sku.Contains(poSearch, StringComparison.OrdinalIgnoreCase)));
        }
        var filteredPos = posQuery.OrderByDescending(p => p.CreatedAt).ToList();
        var inboundReceipts = allSlips.Where(s => s.Type == StockSlipType.Import).OrderByDescending(s => s.CreatedAt).Take(20).ToList();

        // 3. Filter IMEI / Serial items (Mục 3.2)
        var imeiQuery = allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(imeiSearch))
        {
            imeiQuery = imeiQuery.Where(i => 
                i.SerialNumber.Contains(imeiSearch, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(i.Imei2) && i.Imei2.Contains(imeiSearch, StringComparison.OrdinalIgnoreCase)) ||
                i.ProductName.Contains(imeiSearch, StringComparison.OrdinalIgnoreCase) ||
                i.VariantSku.Contains(imeiSearch, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(i.ImportBatchCode) && i.ImportBatchCode.Contains(imeiSearch, StringComparison.OrdinalIgnoreCase)));
        }
        if (!string.IsNullOrWhiteSpace(imeiStatus) && Enum.TryParse<InventoryStatus>(imeiStatus, true, out var parsedStatus))
        {
            imeiQuery = imeiQuery.Where(i => i.Status == parsedStatus);
        }
        if (!string.IsNullOrWhiteSpace(imeiSku))
        {
            imeiQuery = imeiQuery.Where(i => i.VariantSku.Equals(imeiSku, StringComparison.OrdinalIgnoreCase));
        }
        var filteredItems = imeiQuery.OrderByDescending(i => i.ReceivedDate).Take(100).ToList();

        // 4. Outbound & Fulfillment (Mục 3.3)
        var outboundOrdersQuery = allOrders.Where(o => 
            o.OrderStatus == OrderStatus.Confirmed || 
            o.OrderStatus == OrderStatus.Preparing).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(outboundCarrier))
        {
            outboundOrdersQuery = outboundOrdersQuery.Where(o => o.CarrierName.Contains(outboundCarrier, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(outboundSearch))
        {
            outboundOrdersQuery = outboundOrdersQuery.Where(o => 
                o.OrderCode.Contains(outboundSearch, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerName.Contains(outboundSearch, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerPhone.Contains(outboundSearch, StringComparison.OrdinalIgnoreCase));
        }
        var pendingOrders = outboundOrdersQuery.OrderBy(o => o.CreatedAt).ToList();

        // Compute Pick List grouped by Shelf/Aisle
        var pickList = new List<PickListItemDto>();
        foreach (var order in pendingOrders)
        {
            foreach (var item in order.Items)
            {
                var neededQty = item.Quantity - item.AssignedSerialNumbers.Count;
                if (neededQty <= 0) continue;

                var existingPick = pickList.FirstOrDefault(p => p.Sku == item.VariantSku);
                if (existingPick != null)
                {
                    existingPick.RequiredQuantity += neededQty;
                    if (!existingPick.OrderCodes.Contains(order.OrderCode))
                    {
                        existingPick.OrderCodes.Add(order.OrderCode);
                    }
                }
                else
                {
                    var sampleStockItem = allItems.FirstOrDefault(i => i.VariantSku == item.VariantSku && i.Status == InventoryStatus.InStock);
                    var inStockQty = allItems.Count(i => i.VariantSku == item.VariantSku && i.Status == InventoryStatus.InStock);

                    pickList.Add(new PickListItemDto
                    {
                        WarehouseLocation = sampleStockItem?.WarehouseLocation ?? "Kệ A1-Kho Tổng",
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Sku = item.VariantSku,
                        VariantName = item.VariantName,
                        RequiredQuantity = neededQty,
                        InStockQuantity = inStockQty,
                        OrderCodes = new List<string> { order.OrderCode }
                    });
                }
            }
        }
        pickList = pickList.OrderBy(p => p.WarehouseLocation).ThenBy(p => p.ProductName).ToList();

        // 5. Stocktake, Transfers & Reverse Logistics (Mục 3.4)
        var stocktakeSessions = allStocktake.OrderByDescending(s => s.CreatedAt).ToList();
        var stockTransfers = allTransfers.OrderByDescending(t => t.SentDate).ToList();
        var safetyAlertProducts = allProducts.Where(p => p.IsActive && p.AvailableQuantity <= (p.MinStockThreshold > 0 ? p.MinStockThreshold : 5)).OrderBy(p => p.AvailableQuantity).ToList();
        var reverseItems = allItems.Where(i => i.Status == InventoryStatus.Defective || i.Status == InventoryStatus.Returned || i.Status == InventoryStatus.InRepair).OrderByDescending(i => i.UpdatedAt ?? i.ReceivedDate).Take(50).ToList();

        var model = new WarehousePortalViewModel
        {
            ActiveTab = tab,
            CurrentStaffId = currentUserId,
            CurrentStaffName = currentUserName,
            CurrentWarehouseName = "Tổng Kho Tân Bình - TP.HCM",

            PendingPoCount = pendingPoCount,
            InStockCount = inStockCount,
            LowStockAlertCount = lowSafetyStockCount,
            OutboundPendingOrdersCount = outboundPendingCount,
            ActiveTransfersCount = activeTransfersCount,
            DamagedDefectiveCount = damagedDefectiveCount,

            PurchaseOrders = filteredPos,
            InboundReceipts = inboundReceipts,
            Suppliers = allSuppliers.Where(s => s.IsActive).ToList(),
            PoFilterStatus = poStatus,
            PoSearchQuery = poSearch,

            InventoryItems = filteredItems,
            AvailableProducts = allProducts.Where(p => p.IsActive).OrderBy(p => p.Name).ToList(),
            ImeiSearchQuery = imeiSearch,
            ImeiFilterStatus = imeiStatus,
            ImeiFilterSku = imeiSku,

            OutboundPendingOrders = pendingOrders,
            PickList = pickList,
            OutboundFilterCarrier = outboundCarrier,
            OutboundSearchQuery = outboundSearch,

            StocktakeSessions = stocktakeSessions,
            StockTransfers = stockTransfers,
            SafetyStockAlertProducts = safetyAlertProducts,
            ReverseLogisticsItems = reverseItems
        };

        return View(model);
    }

    // =========================================================================
    // 3.1 QUẢN LÝ NHẬP KHO & NHÀ CUNG CẤP (INBOUND LOGISTICS)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReceiptFromPo(string poId, CancellationToken cancellationToken = default)
    {
        var po = await _poRepo.GetByIdAsync(poId, cancellationToken);
        if (po == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn đặt hàng nhập (PO).";
            return RedirectToAction(nameof(Index), new { tab = "inbound" });
        }

        po.Status = "Inspecting";
        await _poRepo.UpdateAsync(po, cancellationToken);

        var slip = new StockSlip
        {
            SlipCode = $"GRN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            Type = StockSlipType.Import,
            SupplierId = po.SupplierId,
            SupplierName = po.SupplierName,
            WarehouseLocation = po.TargetWarehouse,
            CreatedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            CreatedByName = User.Identity?.Name ?? "Thủ Kho",
            Status = StockSlipStatus.Draft,
            TotalQuantity = po.TotalQuantity,
            TotalAmount = po.TotalAmount,
            Note = $"Tiếp nhận thực tế từ đơn đặt hàng PO: {po.PoCode}",
            Items = po.Items.Select(i => new StockSlipItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                Quantity = i.OrderedQuantity,
                UnitPrice = i.UnitCost,
                SubTotal = i.SubTotal
            }).ToList()
        };

        await _stockSlipRepo.AddAsync(slip, cancellationToken);

        await LogAuditAsync("InboundCreateReceipt", "Warehouse", $"Tạo phiếu nhập kho {slip.SlipCode} từ {po.PoCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu tiếp nhận nhập kho thực tế (GRN) {slip.SlipCode} cho đơn PO {po.PoCode}. Trạng thái chuyển sang 'Kiểm định'.";
        return RedirectToAction(nameof(Index), new { tab = "inbound" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InspectInboundBatch(
        string poId,
        string inspectionResult,
        string? defectNotes,
        CancellationToken cancellationToken = default)
    {
        var po = await _poRepo.GetByIdAsync(poId, cancellationToken);
        if (po == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn đặt hàng PO.";
            return RedirectToAction(nameof(Index), new { tab = "inbound" });
        }

        var logInspection = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm} - {User.Identity?.Name}]: Kiểm định ngoại quan: {inspectionResult}. {(string.IsNullOrWhiteSpace(defectNotes) ? "" : $"Chi tiết: {defectNotes}")}";
        po.Notes = string.IsNullOrWhiteSpace(po.Notes) ? logInspection : $"{po.Notes} | {logInspection}";

        await _poRepo.UpdateAsync(po, cancellationToken);
        await LogAuditAsync("InboundInspect", "Warehouse", $"Kiểm định kiện hàng {po.PoCode}: {inspectionResult}", cancellationToken);

        TempData["SuccessMessage"] = $"Ghi nhận kết quả kiểm định số lượng & ngoại quan kiện hàng {po.PoCode}: {inspectionResult}.";
        return RedirectToAction(nameof(Index), new { tab = "inbound" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanImeiBatch(
        string poId,
        string productId,
        string variantSku,
        string rawSerials,
        string warehouseLocation = "Kệ A1-Kho Tổng",
        CancellationToken cancellationToken = default)
    {
        var po = await _poRepo.GetByIdAsync(poId, cancellationToken);
        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);

        if (po == null || product == null)
        {
            TempData["ErrorMessage"] = "Thông tin PO hoặc Sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab = "inbound" });
        }

        if (string.IsNullOrWhiteSpace(rawSerials))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập hoặc quét ít nhất 1 mã IMEI/Serial.";
            return RedirectToAction(nameof(Index), new { tab = "inbound" });
        }

        var lines = rawSerials
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpper())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        var poItem = po.Items.FirstOrDefault(i => i.Sku.Equals(variantSku, StringComparison.OrdinalIgnoreCase) || i.ProductId == productId);
        var unitCost = poItem?.UnitCost ?? (product.OriginalPrice > 0 ? product.OriginalPrice * 0.7m : product.Price * 0.7m);

        var allExisting = await _inventoryRepo.GetAllAsync(cancellationToken);
        var existingSerialsSet = allExisting.Select(i => i.SerialNumber.ToUpper()).ToHashSet();

        var addedCount = 0;
        var duplicateCount = 0;

        foreach (var serial in lines)
        {
            if (existingSerialsSet.Contains(serial))
            {
                duplicateCount++;
                continue;
            }

            var newItem = new InventoryItem
            {
                SerialNumber = serial,
                ProductId = product.Id,
                ProductName = product.Name,
                VariantSku = !string.IsNullOrWhiteSpace(variantSku) ? variantSku : product.Sku,
                VariantName = product.Variants.FirstOrDefault(v => v.Sku.Equals(variantSku, StringComparison.OrdinalIgnoreCase))?.VariantName ?? "Mặc định",
                Status = InventoryStatus.InStock,
                WarehouseLocation = string.IsNullOrWhiteSpace(warehouseLocation) ? "Kệ A1-Kho Tổng" : warehouseLocation,
                CostPrice = unitCost,
                ImportBatchCode = po.PoCode,
                SupplierId = po.SupplierId,
                SupplierName = po.SupplierName,
                ReceivedDate = DateTime.UtcNow,
                Notes = $"Nhập khui kiện từ PO {po.PoCode}"
            };

            await _inventoryRepo.AddAsync(newItem, cancellationToken);
            existingSerialsSet.Add(serial);
            addedCount++;
        }

        if (addedCount > 0)
        {
            // Update PO received count
            if (poItem != null)
            {
                poItem.ReceivedQuantity += addedCount;
            }
            po.ReceivedQuantity += addedCount;
            if (po.ReceivedQuantity >= po.TotalQuantity)
            {
                po.Status = "FullyReceived";
            }
            await _poRepo.UpdateAsync(po, cancellationToken);

            // Update Product stock quantities
            product.StockQuantity += addedCount;
            product.AvailableQuantity += addedCount;
            await _productRepo.UpdateAsync(product, cancellationToken);

            await LogAuditAsync("InboundScanImeiBatch", "Warehouse", $"Quét import {addedCount} IMEI cho PO {po.PoCode}, SKU: {variantSku}", cancellationToken);

            TempData["SuccessMessage"] = $"Đã nhập kho thành công {addedCount} mã IMEI/Serial! Tự động cộng dồn số lượng tồn khả dụng cho {product.Name}. {(duplicateCount > 0 ? $"Bỏ qua {duplicateCount} mã đã tồn tại." : "")}";
        }
        else
        {
            TempData["WarningMessage"] = $"Không có mã IMEI nào được thêm. Tất cả {duplicateCount} mã quét đều đã tồn tại trong hệ thống.";
        }

        return RedirectToAction(nameof(Index), new { tab = "inbound" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmInboundReceipt(string slipId, CancellationToken cancellationToken = default)
    {
        var slip = await _stockSlipRepo.GetByIdAsync(slipId, cancellationToken);
        if (slip == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập kho.";
            return RedirectToAction(nameof(Index), new { tab = "inbound" });
        }

        slip.Status = StockSlipStatus.Approved;
        slip.ApprovedAt = DateTime.UtcNow;
        slip.ApprovedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        slip.ApprovedByName = User.Identity?.Name ?? "Thủ Kho";

        await _stockSlipRepo.UpdateAsync(slip, cancellationToken);
        await LogAuditAsync("InboundConfirm", "Warehouse", $"Xác nhận hoàn tất phiếu nhập kho {slip.SlipCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã phê duyệt và xác nhận hoàn tất phiếu nhập kho (GRN) {slip.SlipCode}. Số lượng tồn khả dụng đã được xác nhận chính thức.";
        return RedirectToAction(nameof(Index), new { tab = "inbound" });
    }

    // =========================================================================
    // 3.2 QUẢN LÝ ĐỊNH DANH THIẾT BỊ (IMEI / SERIAL NUMBER TRACKING)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSerialStatus(
        string id,
        InventoryStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var item = await _inventoryRepo.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thiết bị với mã định danh đã chọn.";
            return RedirectToAction(nameof(Index), new { tab = "imei" });
        }

        var oldStatus = item.Status;
        if (oldStatus == newStatus)
        {
            return RedirectToAction(nameof(Index), new { tab = "imei" });
        }

        item.Status = newStatus;
        var noteEntry = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm} - {User.Identity?.Name}]: Chuyển trạng thái từ {oldStatus} sang {newStatus}. Lý do: {reason}";
        item.Notes = string.IsNullOrWhiteSpace(item.Notes) ? noteEntry : $"{item.Notes} | {noteEntry}";
        await _inventoryRepo.UpdateAsync(item, cancellationToken);

        // Adjust product available stock if moving between InStock and Damaged/Reserved/Defective
        var product = await _productRepo.GetByIdAsync(item.ProductId, cancellationToken);
        if (product != null)
        {
            if (oldStatus == InventoryStatus.InStock && (newStatus == InventoryStatus.Defective || newStatus == InventoryStatus.InRepair))
            {
                product.AvailableQuantity = Math.Max(0, product.AvailableQuantity - 1);
                product.StockQuantity = Math.Max(0, product.StockQuantity - 1);
                await _productRepo.UpdateAsync(product, cancellationToken);
            }
            else if ((oldStatus == InventoryStatus.Defective || oldStatus == InventoryStatus.InRepair) && newStatus == InventoryStatus.InStock)
            {
                product.AvailableQuantity += 1;
                product.StockQuantity += 1;
                await _productRepo.UpdateAsync(product, cancellationToken);
            }
        }

        await LogAuditAsync("SerialImeiUpdateStatus", "Warehouse", $"Chuyển trạng thái IMEI {item.SerialNumber} từ {oldStatus} sang {newStatus}. Lý do: {reason}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái IMEI/Serial {item.SerialNumber} sang '{newStatus}'.";
        return RedirectToAction(nameof(Index), new { tab = "imei" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BindSerialSku(
        string id,
        string newProductId,
        string newVariantSku,
        CancellationToken cancellationToken = default)
    {
        var item = await _inventoryRepo.GetByIdAsync(id, cancellationToken);
        var product = await _productRepo.GetByIdAsync(newProductId, cancellationToken);

        if (item == null || product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc sản phẩm mục tiêu.";
            return RedirectToAction(nameof(Index), new { tab = "imei" });
        }

        var variant = product.Variants.FirstOrDefault(v => v.Sku.Equals(newVariantSku, StringComparison.OrdinalIgnoreCase));
        var oldSku = item.VariantSku;

        item.ProductId = product.Id;
        item.ProductName = product.Name;
        item.VariantSku = !string.IsNullOrWhiteSpace(newVariantSku) ? newVariantSku : product.Sku;
        item.VariantName = variant?.VariantName ?? "Mặc định";

        await _inventoryRepo.UpdateAsync(item, cancellationToken);
        await LogAuditAsync("SerialImeiBindSku", "Warehouse", $"Gán lại SKU cho IMEI {item.SerialNumber} từ {oldSku} sang {item.VariantSku}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã gán thành công định danh Serial/IMEI {item.SerialNumber} với SKU: {item.VariantSku} ({product.Name}).";
        return RedirectToAction(nameof(Index), new { tab = "imei" });
    }

    [HttpGet]
    public async Task<IActionResult> PrintBarcode(string serialNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            TempData["ErrorMessage"] = "Mã Serial không hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab = "imei" });
        }

        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        var item = allItems.FirstOrDefault(i => i.SerialNumber.Equals(serialNumber.Trim(), StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy thiết bị với mã {serialNumber}.";
            return RedirectToAction(nameof(Index), new { tab = "imei" });
        }

        var dto = new BarcodePrintItemDto
        {
            SerialNumber = item.SerialNumber,
            Imei2 = item.Imei2,
            ProductName = item.ProductName,
            Sku = item.VariantSku,
            WarehouseLocation = item.WarehouseLocation,
            BatchCode = item.ImportBatchCode
        };

        return View("PrintBarcode", dto);
    }

    // =========================================================================
    // 3.3 QUẢN LÝ XUẤT KHO & ĐÓNG GÓI (OUTBOUND & FULFILLMENT)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> CreatePickList(CancellationToken cancellationToken = default)
    {
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);

        var pendingOrders = allOrders.Where(o => 
            o.OrderStatus == OrderStatus.Confirmed || 
            o.OrderStatus == OrderStatus.Preparing).OrderBy(o => o.CreatedAt).ToList();

        var pickList = new List<PickListItemDto>();
        foreach (var order in pendingOrders)
        {
            foreach (var item in order.Items)
            {
                var neededQty = item.Quantity - item.AssignedSerialNumbers.Count;
                if (neededQty <= 0) continue;

                var existingPick = pickList.FirstOrDefault(p => p.Sku == item.VariantSku);
                if (existingPick != null)
                {
                    existingPick.RequiredQuantity += neededQty;
                    if (!existingPick.OrderCodes.Contains(order.OrderCode))
                    {
                        existingPick.OrderCodes.Add(order.OrderCode);
                    }
                }
                else
                {
                    var sampleStockItem = allItems.FirstOrDefault(i => i.VariantSku == item.VariantSku && i.Status == InventoryStatus.InStock);
                    var inStockQty = allItems.Count(i => i.VariantSku == item.VariantSku && i.Status == InventoryStatus.InStock);

                    pickList.Add(new PickListItemDto
                    {
                        WarehouseLocation = sampleStockItem?.WarehouseLocation ?? "Kệ A1-Kho Tổng",
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Sku = item.VariantSku,
                        VariantName = item.VariantName,
                        RequiredQuantity = neededQty,
                        InStockQuantity = inStockQty,
                        OrderCodes = new List<string> { order.OrderCode }
                    });
                }
            }
        }

        pickList = pickList.OrderBy(p => p.WarehouseLocation).ThenBy(p => p.ProductName).ToList();
        return View("PrintPickList", pickList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanDispatchSerial(
        string orderId,
        string sku,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần xuất kho.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            TempData["ErrorMessage"] = "Vui lòng quét hoặc nhập mã IMEI/Serial.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        var normalizedSerial = serialNumber.Trim().ToUpper();
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        var invItem = allItems.FirstOrDefault(i => i.SerialNumber.Equals(normalizedSerial, StringComparison.OrdinalIgnoreCase));

        if (invItem == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy IMEI {normalizedSerial} trong hệ thống kho.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        if (invItem.Status != InventoryStatus.InStock)
        {
            TempData["ErrorMessage"] = $"IMEI {normalizedSerial} hiện có trạng thái '{invItem.Status}', không sẵn sàng để xuất bán!";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        var orderItem = order.Items.FirstOrDefault(i => i.VariantSku.Equals(sku, StringComparison.OrdinalIgnoreCase) || i.ProductId == invItem.ProductId);
        if (orderItem == null)
        {
            TempData["ErrorMessage"] = $"Đơn hàng {order.OrderCode} không chứa sản phẩm SKU: {sku}.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        if (orderItem.AssignedSerialNumbers.Count >= orderItem.Quantity)
        {
            TempData["WarningMessage"] = $"SKU {sku} trong đơn {order.OrderCode} đã được quét đủ {orderItem.Quantity} máy.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        if (orderItem.AssignedSerialNumbers.Contains(normalizedSerial))
        {
            TempData["WarningMessage"] = $"Mã IMEI {normalizedSerial} đã được gán cho sản phẩm này trong đơn.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        // 1. Assign Serial to Order
        orderItem.AssignedSerialNumbers.Add(normalizedSerial);

        // 2. Update InventoryItem
        invItem.Status = InventoryStatus.AssignedToOrder;
        invItem.AssignedOrderId = order.Id;
        invItem.AssignedOrderCode = order.OrderCode;
        await _inventoryRepo.UpdateAsync(invItem, cancellationToken);

        // 3. Automatically Create / Activate Warranty Record (Ràng buộc Serial với đơn để kích hoạt bảo hành)
        var allWarranties = await _warrantyRepo.GetAllAsync(cancellationToken);
        var existingWarranty = allWarranties.FirstOrDefault(w => w.SerialNumber.Equals(normalizedSerial, StringComparison.OrdinalIgnoreCase));
        if (existingWarranty == null)
        {
            var warrantyMonths = orderItem.WarrantyMonths > 0 ? orderItem.WarrantyMonths : 12;
            var newWarranty = new Warranty
            {
                SerialNumber = normalizedSerial,
                Imei = invItem.Imei2,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.ProductName,
                VariantSku = orderItem.VariantSku,
                VariantName = orderItem.VariantName,
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                CustomerId = order.UserId,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(warrantyMonths),
                WarrantyMonths = warrantyMonths,
                Status = WarrantyStatus.Active
            };
            await _warrantyRepo.AddAsync(newWarranty, cancellationToken);
            invItem.WarrantyId = newWarranty.Id;
            await _inventoryRepo.UpdateAsync(invItem, cancellationToken);
        }

        // 4. Update Order Status to Preparing if all items scanned
        var allScanned = order.Items.All(i => i.AssignedSerialNumbers.Count >= i.Quantity);
        if (allScanned)
        {
            order.OrderStatus = OrderStatus.Preparing;
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.OrderStatus,
            Note = $"Đã quét gán IMEI {normalizedSerial} cho SKU {sku} bởi {User.Identity?.Name ?? "Thủ Kho"}"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);
        await LogAuditAsync("OutboundScanDispatch", "Warehouse", $"Quét xuất IMEI {normalizedSerial} cho đơn {order.OrderCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã quét xuất và ràng buộc IMEI {normalizedSerial} cho đơn {order.OrderCode}. Kích hoạt bảo hành điện tử thành công! {(allScanned ? "Đơn hàng đã được gán đủ 100% IMEI, sẵn sàng bàn giao vận chuyển." : "")}";
        return RedirectToAction(nameof(Index), new { tab = "outbound" });
    }

    [HttpGet]
    public async Task<IActionResult> PrintShippingLabel(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        return View("PrintShippingLabel", order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandoverCarrier(
        string orderId,
        string carrierName,
        string? trackingNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        var assignedSerials = order.Items.SelectMany(i => i.AssignedSerialNumbers).Distinct().ToList();
        if (!assignedSerials.Any())
        {
            TempData["ErrorMessage"] = "Đơn hàng chưa được quét gán bất kỳ mã IMEI/Serial nào. Vui lòng quét IMEI trước khi xuất kho giao bưu tá.";
            return RedirectToAction(nameof(Index), new { tab = "outbound" });
        }

        order.CarrierName = !string.IsNullOrWhiteSpace(carrierName) ? carrierName : order.CarrierName;
        order.TrackingNumber = !string.IsNullOrWhiteSpace(trackingNumber) ? trackingNumber : $"VN-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        order.OrderStatus = OrderStatus.Shipping;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Shipping,
            Note = $"Đã bàn giao kiện hàng cho đối tác vận chuyển {order.CarrierName}, Vận đơn: {order.TrackingNumber}. Nhân sự: {User.Identity?.Name ?? "Thủ Kho"}"
        });

        await _orderRepo.UpdateAsync(order, cancellationToken);

        // Update InventoryItems to Shipped
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        foreach (var serial in assignedSerials)
        {
            var item = allItems.FirstOrDefault(i => i.SerialNumber.Equals(serial, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Status = InventoryStatus.Shipped;
                item.ShippedDate = DateTime.UtcNow;
                await _inventoryRepo.UpdateAsync(item, cancellationToken);
            }
        }

        await LogAuditAsync("OutboundHandoverCarrier", "Warehouse", $"Bàn giao đơn {order.OrderCode} cho {order.CarrierName}, tracking {order.TrackingNumber}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã xác nhận bàn giao thành công kiện hàng {order.OrderCode} cho bưu tá {order.CarrierName}. Mã vận đơn: {order.TrackingNumber}. Đơn đã chuyển sang trạng thái 'Đang giao hàng'.";
        return RedirectToAction(nameof(Index), new { tab = "outbound" });
    }

    // =========================================================================
    // 3.4 KIỂM KÊ, ĐIỀU CHUYỂN & HÀNG ĐỔI TRẢ (STOCKTAKE & REVERSE LOGISTICS)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStocktakeSession(
        string title,
        string warehouseLocation,
        CancellationToken cancellationToken = default)
    {
        var session = new StocktakeSession
        {
            Title = !string.IsNullOrWhiteSpace(title) ? title : "Kiểm kê định kỳ Kho TechZone",
            WarehouseLocation = !string.IsNullOrWhiteSpace(warehouseLocation) ? warehouseLocation : "Tổng Kho Tân Bình - TP.HCM",
            ConductedByStaff = User.Identity?.Name ?? "Thủ Kho",
            ConductedDate = DateTime.UtcNow,
            Status = "InProgress"
        };

        await _stocktakeRepo.AddAsync(session, cancellationToken);
        await LogAuditAsync("StocktakePerform", "Warehouse", $"Khởi tạo phiên kiểm kê kho {session.SessionCode}: {session.Title}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã khởi tạo phiên kiểm kê kho {session.SessionCode}. Hãy tiến hành quét đếm thực tế.";
        return RedirectToAction(nameof(Index), new { tab = "stocktake" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitStocktakeCount(
        string sessionId,
        string productId,
        string sku,
        int countedQty,
        string? scannedSerialsRaw,
        string? discrepancyReason,
        CancellationToken cancellationToken = default)
    {
        var session = await _stocktakeRepo.GetByIdAsync(sessionId, cancellationToken);
        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);

        if (session == null || product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiên kiểm kê hoặc sản phẩm.";
            return RedirectToAction(nameof(Index), new { tab = "stocktake" });
        }

        var systemQty = product.StockQuantity;
        var diff = countedQty - systemQty;

        var scannedList = (scannedSerialsRaw ?? "")
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpper())
            .Distinct()
            .ToList();

        var discrepancyItem = new StocktakeDiscrepancyItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Sku = !string.IsNullOrWhiteSpace(sku) ? sku : product.Sku,
            SystemQty = systemQty,
            CountedQty = countedQty,
            ScannedSerials = scannedList,
            DiscrepancyReason = discrepancyReason
        };

        session.Items.Add(discrepancyItem);
        session.TotalSystemItems += systemQty;
        session.TotalCountedItems += countedQty;
        session.DiscrepancyCount += diff;

        await _stocktakeRepo.UpdateAsync(session, cancellationToken);

        // Adjust product stock if diff != 0
        if (diff != 0)
        {
            product.StockQuantity = countedQty;
            product.AvailableQuantity = Math.Max(0, countedQty - product.ReservedQuantity);
            await _productRepo.UpdateAsync(product, cancellationToken);
        }

        await LogAuditAsync("StocktakeLogDiscrepancy", "Warehouse", $"Biên bản chênh lệch phiên {session.SessionCode} - SKU {sku}: Hệ thống {systemQty}, Thực tế {countedQty}, Chênh lệch {diff}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã ghi nhận số liệu kiểm kê SKU {sku}. Chênh lệch: {(diff > 0 ? $"+{diff} (Thừa)" : diff < 0 ? $"{diff} (Thiếu)" : "Khớp 100%")}.";
        return RedirectToAction(nameof(Index), new { tab = "stocktake" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteStocktakeSession(
        string sessionId,
        string? auditNotes,
        CancellationToken cancellationToken = default)
    {
        var session = await _stocktakeRepo.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiên kiểm kê.";
            return RedirectToAction(nameof(Index), new { tab = "stocktake" });
        }

        session.Status = "Completed";
        session.AuditNotes = auditNotes;

        await _stocktakeRepo.UpdateAsync(session, cancellationToken);
        await LogAuditAsync("StocktakePerform", "Warehouse", $"Hoàn tất và chốt sổ biên bản phiên kiểm kê {session.SessionCode}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã chốt sổ và lưu trữ biên bản kiểm kê {session.SessionCode}.";
        return RedirectToAction(nameof(Index), new { tab = "stocktake" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustDamagedStock(
        string serialNumber,
        string damageReason,
        string? shelfLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập số IMEI/Serial của máy bị lỗi/hư hại.";
            return RedirectToAction(nameof(Index), new { tab = "stocktake" });
        }

        var normalized = serialNumber.Trim().ToUpper();
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        var item = allItems.FirstOrDefault(i => i.SerialNumber.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy thiết bị IMEI {normalized}.";
            return RedirectToAction(nameof(Index), new { tab = "stocktake" });
        }

        var wasInStock = item.Status == InventoryStatus.InStock;
        item.Status = InventoryStatus.Defective;
        item.WarehouseLocation = !string.IsNullOrWhiteSpace(shelfLocation) ? shelfLocation : "Khu Cách Ly Hàng Lỗi - Kệ X";
        item.Notes = $"{item.Notes} | [Hàng hư hại {DateTime.UtcNow:dd/MM/yyyy HH:mm} bởi {User.Identity?.Name}]: {damageReason}";

        await _inventoryRepo.UpdateAsync(item, cancellationToken);

        if (wasInStock)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null)
            {
                product.AvailableQuantity = Math.Max(0, product.AvailableQuantity - 1);
                product.StockQuantity = Math.Max(0, product.StockQuantity - 1);
                await _productRepo.UpdateAsync(product, cancellationToken);
            }
        }

        await LogAuditAsync("InventoryAdjustDamaged", "Warehouse", $"Giảm tồn hàng hư hại IMEI {normalized}: {damageReason}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cách ly thiết bị IMEI {normalized} sang khu vực hàng lỗi và tự động giảm tồn khả dụng.";
        return RedirectToAction(nameof(Index), new { tab = "stocktake" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureSafetyStockThreshold(
        string productId,
        int minStockThreshold,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Index), new { tab = "stocktake" });
        }

        var oldThreshold = product.MinStockThreshold;
        product.MinStockThreshold = Math.Max(0, minStockThreshold);
        await _productRepo.UpdateAsync(product, cancellationToken);

        await LogAuditAsync("InventoryThresholdAlert", "Warehouse", $"Cấu hình ngưỡng an toàn Safety Stock cho {product.Name} từ {oldThreshold} sang {product.MinStockThreshold}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật mức tồn an toàn (Safety Stock) cho {product.Name} là {product.MinStockThreshold} cái.";
        return RedirectToAction(nameof(Index), new { tab = "stocktake" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransfer(
        string fromWarehouse,
        string toWarehouse,
        string productId,
        string sku,
        int quantity,
        string? serialsRaw,
        string? carrierName,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần điều chuyển.";
            return RedirectToAction(nameof(Index), new { tab = "transfer" });
        }

        var serialsList = (serialsRaw ?? "")
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpper())
            .Distinct()
            .ToList();

        var transfer = new StockTransfer
        {
            FromWarehouse = !string.IsNullOrWhiteSpace(fromWarehouse) ? fromWarehouse : "Tổng Kho Tân Bình - TP.HCM",
            ToWarehouse = !string.IsNullOrWhiteSpace(toWarehouse) ? toWarehouse : "Chi Nhánh Showroom Quận 1",
            CreatedByStaff = User.Identity?.Name ?? "Thủ Kho",
            CarrierName = !string.IsNullOrWhiteSpace(carrierName) ? carrierName : "Đội Giao Hàng Nội Bộ TechZone",
            TrackingNumber = $"TRF-TRK-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            TotalItems = serialsList.Any() ? serialsList.Count : quantity,
            Status = "InTransit",
            SentDate = DateTime.UtcNow,
            Notes = notes,
            Items = new List<StockTransferItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = !string.IsNullOrWhiteSpace(sku) ? sku : product.Sku,
                    Quantity = serialsList.Any() ? serialsList.Count : quantity,
                    SerialNumbers = serialsList
                }
            }
        };

        await _transferRepo.AddAsync(transfer, cancellationToken);

        // Update items status to InTransit
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        foreach (var sn in serialsList)
        {
            var item = allItems.FirstOrDefault(i => i.SerialNumber.Equals(sn, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Status = InventoryStatus.InTransit;
                item.WarehouseLocation = $"Đang điều chuyển -> {transfer.ToWarehouse}";
                await _inventoryRepo.UpdateAsync(item, cancellationToken);
            }
        }

        await LogAuditAsync("TransferCreate", "Warehouse", $"Lập phiếu điều chuyển {transfer.TransferCode} từ {transfer.FromWarehouse} sang {transfer.ToWarehouse} ({transfer.TotalItems} thiết bị)", cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu điều chuyển {transfer.TransferCode} gồm {transfer.TotalItems} thiết bị từ {transfer.FromWarehouse} sang {transfer.ToWarehouse}. Trạng thái: 'Đang vận chuyển'.";
        return RedirectToAction(nameof(Index), new { tab = "transfer" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveTransfer(string transferId, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepo.GetByIdAsync(transferId, cancellationToken);
        if (transfer == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu điều chuyển.";
            return RedirectToAction(nameof(Index), new { tab = "transfer" });
        }

        transfer.Status = "Received";
        transfer.ReceivedDate = DateTime.UtcNow;
        transfer.ReceivedByStaff = User.Identity?.Name ?? "Thủ Kho";

        await _transferRepo.UpdateAsync(transfer, cancellationToken);

        // Update items to InStock at destination
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        foreach (var itemRow in transfer.Items)
        {
            foreach (var sn in itemRow.SerialNumbers)
            {
                var item = allItems.FirstOrDefault(i => i.SerialNumber.Equals(sn, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.Status = InventoryStatus.InStock;
                    item.WarehouseLocation = transfer.ToWarehouse;
                    await _inventoryRepo.UpdateAsync(item, cancellationToken);
                }
            }
        }

        await LogAuditAsync("TransferReceive", "Warehouse", $"Tiếp nhận thành công phiếu điều chuyển {transfer.TransferCode} tại {transfer.ToWarehouse}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã tiếp nhận thành công phiếu điều chuyển {transfer.TransferCode}. Các thiết bị đã được nhập kho sẵn sàng bán tại {transfer.ToWarehouse}.";
        return RedirectToAction(nameof(Index), new { tab = "transfer" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCustomerReturn(
        string orderCode,
        string serialNumber,
        string returnType,
        string inspectionResult,
        string? actionNotes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập mã IMEI/Serial thiết bị hoàn về.";
            return RedirectToAction(nameof(Index), new { tab = "returns" });
        }

        var normalized = serialNumber.Trim().ToUpper();
        var allItems = await _inventoryRepo.GetAllAsync(cancellationToken);
        var item = allItems.FirstOrDefault(i => i.SerialNumber.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy IMEI {normalized} trong cơ sở dữ liệu.";
            return RedirectToAction(nameof(Index), new { tab = "returns" });
        }

        var product = await _productRepo.GetByIdAsync(item.ProductId, cancellationToken);
        var isGood = returnType.Equals("RestockGood", StringComparison.OrdinalIgnoreCase);

        if (isGood)
        {
            item.Status = InventoryStatus.InStock;
            item.WarehouseLocation = "Kệ A1-Kho Hàng Trả Hoàn Khả Dụng";
            if (product != null)
            {
                product.StockQuantity += 1;
                product.AvailableQuantity += 1;
                await _productRepo.UpdateAsync(product, cancellationToken);
            }
        }
        else
        {
            item.Status = InventoryStatus.Defective;
            item.WarehouseLocation = "Khu Cách Ly Hàng Đổi Trả Chờ Hãng - Kệ R";
        }

        item.Notes = $"{item.Notes} | [Tiếp nhận hàng hoàn {DateTime.UtcNow:dd/MM/yyyy HH:mm} từ đơn {orderCode}]: Phân loại: {(isGood ? "Nhập lại kho bán" : "Cách ly hàng lỗi")}, Kiểm tra: {inspectionResult}. {actionNotes}";
        await _inventoryRepo.UpdateAsync(item, cancellationToken);

        await LogAuditAsync("ReturnsRestock", "Warehouse", $"Xử lý hàng hoàn IMEI {normalized} từ đơn {orderCode} -> {(isGood ? "Nhập lại tồn khả dụng" : "Cách ly lỗi")}", cancellationToken);

        TempData["SuccessMessage"] = $"Đã tiếp nhận và đối chiếu IMEI {normalized} thành công. Xử lý: {(isGood ? "Đã nhập lại kho khả dụng để bán tiếp" : "Đã cách ly kho hàng lỗi để đổi trả hãng / RMA")}.";
        return RedirectToAction(nameof(Index), new { tab = "returns" });
    }

    private async Task LogAuditAsync(string action, string module, string details, CancellationToken cancellationToken)
    {
        try
        {
            var audit = new SecurityAuditLog
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserEmail = User.Identity?.Name,
                Action = action,
                Module = module,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                Details = details
            };
            await _auditLogRepo.AddAsync(audit, cancellationToken);
        }
        catch
        {
            // Logging failure should not disrupt main transaction
        }
    }
}
