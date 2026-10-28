using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,Accountant")]
public class ReportsController : AdminBaseController
{
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;

    public ReportsController(
        IRepository<Order> orderRepo,
        IRepository<InventoryItem> inventoryRepo,
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        IRepository<SecurityAuditLog> auditLogRepo)
    {
        _orderRepo = orderRepo;
        _inventoryRepo = inventoryRepo;
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "revenue",
        string range = "30d",
        string granularity = "day",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var allInventory = await _inventoryRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);
        var allCategories = await _categoryRepo.GetAllAsync(cancellationToken);

        // Date range filtering
        DateTime startDate;
        DateTime endDate = toDate.HasValue 
            ? toDate.Value.Date.AddDays(1).AddTicks(-1) 
            : DateTime.UtcNow;

        if (range == "custom" && fromDate.HasValue)
        {
            startDate = fromDate.Value.Date;
        }
        else
        {
            startDate = range switch
            {
                "7d" => DateTime.UtcNow.Date.AddDays(-7),
                "90d" => DateTime.UtcNow.Date.AddDays(-90),
                "year" => new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "all" => DateTime.MinValue,
                _ => DateTime.UtcNow.Date.AddDays(-30) // default 30d
            };
        }

        var rangeOrders = allOrders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var validOrders = rangeOrders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled)
            .ToList();

        // 1. REVENUE METRICS (report.revenue)
        var grossRevenue = validOrders.Sum(o => o.TotalAmount);
        var netRevenue = validOrders.Sum(o => o.Subtotal - o.OrderDiscountAmount);
        var totalDiscounts = validOrders.Sum(o => o.OrderDiscountAmount + o.ShippingDiscountAmount);
        var totalShipping = validOrders.Sum(o => o.ShippingFee);
        var aov = validOrders.Count > 0 ? Math.Round(grossRevenue / validOrders.Count, 0) : 0;

        var totalOrdersCount = rangeOrders.Count;
        var completedOrdersCount = rangeOrders.Count(o => o.OrderStatus == OrderStatus.Delivered);
        var processingOrdersCount = rangeOrders.Count(o => o.OrderStatus == OrderStatus.Pending || 
                                                          o.OrderStatus == OrderStatus.Confirmed || 
                                                          o.OrderStatus == OrderStatus.Preparing || 
                                                          o.OrderStatus == OrderStatus.Shipping);
        var cancelledOrdersCount = rangeOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled);
        var deliverySuccessRate = totalOrdersCount > 0 ? Math.Round((double)completedOrdersCount / totalOrdersCount * 100, 1) : 0;
        var cancellationRate = totalOrdersCount > 0 ? Math.Round((double)cancelledOrdersCount / totalOrdersCount * 100, 1) : 0;

        // Channel breakdown
        var channelGroups = rangeOrders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.SourceChannel) ? "Website" : o.SourceChannel)
            .Select(g => new ChannelRevenueMetric
            {
                ChannelName = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Where(o => o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                Percentage = grossRevenue > 0 
                    ? Math.Round((double)(g.Where(o => o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount) / grossRevenue) * 100, 1) 
                    : 0
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        // Payment breakdown
        var paymentGroups = rangeOrders
            .GroupBy(o => o.PaymentMethod.ToString())
            .Select(g => new PaymentMethodMetric
            {
                MethodName = g.Key switch
                {
                    "COD" => "COD (Thanh toán khi nhận)",
                    "VNPay" => "VNPay QR / Thẻ nội địa",
                    "BankTransfer" => "Chuyển khoản ngân hàng",
                    "MoMo" => "Ví điện tử MoMo",
                    _ => g.Key
                },
                OrderCount = g.Count(),
                TotalAmount = g.Where(o => o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                Percentage = grossRevenue > 0 
                    ? Math.Round((double)(g.Where(o => o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount) / grossRevenue) * 100, 1) 
                    : 0
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // 2. PROFIT MARGIN METRICS (report.profit_margin)
        // Estimated Cost of Goods Sold (COGS) based on physical inventory cost or product original price
        decimal totalCogs = 0;
        var productCostCache = new Dictionary<string, decimal>();

        foreach (var order in validOrders)
        {
            foreach (var item in order.Items)
            {
                if (!productCostCache.TryGetValue(item.ProductId, out var unitCost))
                {
                    var invMatch = allInventory.FirstOrDefault(i => i.ProductId == item.ProductId && i.CostPrice > 0);
                    if (invMatch != null && invMatch.CostPrice > 0)
                    {
                        unitCost = invMatch.CostPrice;
                    }
                    else
                    {
                        var prodMatch = allProducts.FirstOrDefault(p => p.Id == item.ProductId);
                        if (prodMatch != null && prodMatch.OriginalPrice > 0)
                        {
                            unitCost = prodMatch.OriginalPrice;
                        }
                        else
                        {
                            unitCost = item.UnitPrice * 0.82m; // Benchmark benchmark ngành điện tử ~82% giá bán
                        }
                    }
                    productCostCache[item.ProductId] = unitCost;
                }

                totalCogs += unitCost * item.Quantity;
            }
        }

        var grossProfit = Math.Max(0, netRevenue - totalCogs);
        var profitMarginPct = netRevenue > 0 ? Math.Round((double)(grossProfit / netRevenue) * 100, 1) : 0;
        var totalOrderVouchers = validOrders.Sum(o => o.OrderDiscountAmount);
        var totalShippingSubsidy = validOrders.Sum(o => o.ShippingDiscountAmount);
        var operatingProfit = Math.Max(0, grossProfit - totalOrderVouchers - totalShippingSubsidy);
        var operatingMarginPct = netRevenue > 0 ? Math.Round((double)(operatingProfit / netRevenue) * 100, 1) : 0;

        // Top Profit Products Breakdown
        var productSalesDict = new Dictionary<string, (string Name, string Sku, int Units, decimal Revenue, decimal Cost)>();
        foreach (var order in validOrders)
        {
            foreach (var item in order.Items)
            {
                var unitCost = productCostCache.GetValueOrDefault(item.ProductId, item.UnitPrice * 0.82m);
                if (!productSalesDict.ContainsKey(item.ProductId))
                {
                    productSalesDict[item.ProductId] = (item.ProductName, item.VariantSku, 0, 0, 0);
                }
                var current = productSalesDict[item.ProductId];
                productSalesDict[item.ProductId] = (
                    current.Name,
                    current.Sku,
                    current.Units + item.Quantity,
                    current.Revenue + item.TotalPrice,
                    current.Cost + (unitCost * item.Quantity)
                );
            }
        }

        var topProfitProducts = productSalesDict.Values
            .Select(p =>
            {
                var profit = Math.Max(0, p.Revenue - p.Cost);
                var margin = p.Revenue > 0 ? Math.Round((double)(profit / p.Revenue) * 100, 1) : 0;
                return new ProductProfitMetric
                {
                    ProductName = p.Name,
                    VariantSku = p.Sku,
                    UnitsSold = p.Units,
                    Revenue = p.Revenue,
                    CostOfGoodsSold = p.Cost,
                    GrossProfit = profit,
                    MarginPercent = margin
                };
            })
            .OrderByDescending(p => p.GrossProfit)
            .Take(10)
            .ToList();

        // Category Profitability Breakdown
        var prodCategoryMap = allProducts.ToDictionary(p => p.Id, p => p.CategoryName ?? "Khác");
        var categorySalesDict = new Dictionary<string, (int Units, decimal Revenue, decimal Cost)>();
        foreach (var order in validOrders)
        {
            foreach (var item in order.Items)
            {
                var catName = prodCategoryMap.GetValueOrDefault(item.ProductId, "Khác");
                if (string.IsNullOrWhiteSpace(catName)) catName = "Khác";

                var unitCost = productCostCache.GetValueOrDefault(item.ProductId, item.UnitPrice * 0.82m);
                if (!categorySalesDict.ContainsKey(catName))
                {
                    categorySalesDict[catName] = (0, 0, 0);
                }
                var cur = categorySalesDict[catName];
                categorySalesDict[catName] = (cur.Units + item.Quantity, cur.Revenue + item.TotalPrice, cur.Cost + (unitCost * item.Quantity));
            }
        }

        var categoryProfits = categorySalesDict
            .Select(c =>
            {
                var profit = Math.Max(0, c.Value.Revenue - c.Value.Cost);
                var margin = c.Value.Revenue > 0 ? Math.Round((double)(profit / c.Value.Revenue) * 100, 1) : 0;
                return new CategoryProfitMetric
                {
                    CategoryName = c.Key,
                    UnitsSold = c.Value.Units,
                    Revenue = c.Value.Revenue,
                    CostOfGoodsSold = c.Value.Cost,
                    GrossProfit = profit,
                    MarginPercent = margin
                };
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        // 3. INVENTORY TURNOVER & AGING (report.inventory_turnover)
        var totalInv = allInventory.Count;
        var inStockInv = allInventory.Count(i => i.Status == InventoryStatus.InStock);
        var soldInv = allInventory.Count(i => i.Status == InventoryStatus.Delivered || i.Status == InventoryStatus.Shipped);
        var defectiveInv = allInventory.Count(i => i.Status == InventoryStatus.Defective);

        var inStockItems = allInventory.Where(i => i.Status == InventoryStatus.InStock).ToList();
        var invAssetVal = inStockItems.Sum(i => i.CostPrice > 0 ? i.CostPrice : 15000000m);

        // Inventory turnover: COGS / Avg Inventory Asset or sold / total
        var turnoverRate = invAssetVal > 0 && totalCogs > 0 
            ? Math.Round((double)(totalCogs / invAssetVal) * (365.0 / Math.Max(1, (endDate - startDate).TotalDays)), 2)
            : (totalInv > 0 ? Math.Round((double)soldInv / totalInv * 100, 1) : 0);

        var dsi = turnoverRate > 0 ? Math.Round(365.0 / Math.Max(0.1, turnoverRate), 1) : 45.0;

        // Aging breakdown
        int agingUnder30 = 0;
        int aging30To60 = 0;
        int aging60To90 = 0;
        int agingOver90 = 0;
        decimal staleCapital = 0;
        var staleItemsList = new List<StaleInventoryAlertItem>();

        var nowUtc = DateTime.UtcNow;
        foreach (var item in inStockItems)
        {
            var days = (int)(nowUtc - item.ReceivedDate).TotalDays;
            if (days < 0) days = 0;

            if (days <= 30) agingUnder30++;
            else if (days <= 60) aging30To60++;
            else if (days <= 90)
            {
                aging60To90++;
                staleCapital += item.CostPrice > 0 ? item.CostPrice : 15000000m;
                staleItemsList.Add(new StaleInventoryAlertItem
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    VariantSku = item.VariantSku,
                    SerialNumber = item.SerialNumber,
                    WarehouseLocation = item.WarehouseLocation,
                    SupplierName = item.SupplierName ?? "Nhà phân phối",
                    CostPrice = item.CostPrice > 0 ? item.CostPrice : 15000000m,
                    ReceivedDate = item.ReceivedDate,
                    AgingDays = days,
                    AlertSeverity = "Warning",
                    Recommendation = "Tồn chậm 61-90 ngày. Đề xuất: Tặng kèm Voucher hoặc Bundle phụ kiện giá 0đ"
                });
            }
            else
            {
                agingOver90++;
                staleCapital += item.CostPrice > 0 ? item.CostPrice : 15000000m;
                staleItemsList.Add(new StaleInventoryAlertItem
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    VariantSku = item.VariantSku,
                    SerialNumber = item.SerialNumber,
                    WarehouseLocation = item.WarehouseLocation,
                    SupplierName = item.SupplierName ?? "Nhà phân phối",
                    CostPrice = item.CostPrice > 0 ? item.CostPrice : 15000000m,
                    ReceivedDate = item.ReceivedDate,
                    AgingDays = days,
                    AlertSeverity = "Critical",
                    Recommendation = "Hàng om tồn lâu >90 ngày. Đề xuất: Flash Sale giảm sâu -25% xả kho thu hồi vốn"
                });
            }
        }

        staleItemsList = staleItemsList.OrderByDescending(s => s.AgingDays).Take(25).ToList();

        // Category Turnover Breakdown
        var prodToCategoryMap = allProducts.ToDictionary(p => p.Id, p => p.CategoryName ?? "Khác");
        var catTurnoverDict = new Dictionary<string, (int Total, int InStock, int Sold, decimal Asset)>();
        foreach (var inv in allInventory)
        {
            var cat = prodToCategoryMap.GetValueOrDefault(inv.ProductId, "Khác");
            if (string.IsNullOrWhiteSpace(cat)) cat = "Khác";

            if (!catTurnoverDict.ContainsKey(cat))
            {
                catTurnoverDict[cat] = (0, 0, 0, 0);
            }
            var cur = catTurnoverDict[cat];
            var isStock = inv.Status == InventoryStatus.InStock;
            var isSold = inv.Status == InventoryStatus.Delivered || inv.Status == InventoryStatus.Shipped;
            var cost = inv.CostPrice > 0 ? inv.CostPrice : 15000000m;

            catTurnoverDict[cat] = (
                cur.Total + 1,
                cur.InStock + (isStock ? 1 : 0),
                cur.Sold + (isSold ? 1 : 0),
                cur.Asset + (isStock ? cost : 0)
            );
        }

        var categoryTurnovers = catTurnoverDict
            .Select(c => new CategoryTurnoverMetric
            {
                CategoryName = c.Key,
                TotalStock = c.Value.Total,
                InStock = c.Value.InStock,
                SoldUnits = c.Value.Sold,
                AssetValue = c.Value.Asset,
                TurnoverRate = c.Value.Total > 0 ? Math.Round((double)c.Value.Sold / c.Value.Total * 100, 1) : 0
            })
            .OrderByDescending(c => c.AssetValue)
            .ToList();

        // Time Series Points generation according to Granularity
        var chartPoints = GenerateTimeSeriesPoints(validOrders, startDate, endDate, granularity, productCostCache);

        // Recent Audit logs for financial export
        var auditLogs = await _auditLogRepo.GetAllAsync(cancellationToken);
        var exportLogs = auditLogs
            .Where(l => l.Module == "Finance" || l.Action.Contains("Export"))
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .ToList();

        var model = new ReportsViewModel
        {
            ActiveTab = tab,
            DateRange = range,
            Granularity = granularity,
            FromDate = fromDate,
            ToDate = toDate,

            GrossRevenue = grossRevenue,
            NetRevenue = netRevenue,
            TotalDiscounts = totalDiscounts,
            TotalShippingCollected = totalShipping,
            AverageOrderValue = aov,
            TotalOrders = totalOrdersCount,
            CompletedOrders = completedOrdersCount,
            ProcessingOrders = processingOrdersCount,
            CancelledOrders = cancelledOrdersCount,
            DeliverySuccessRate = deliverySuccessRate,
            CancellationRate = cancellationRate,

            ChartPoints = chartPoints,
            ChannelBreakdowns = channelGroups,
            PaymentBreakdowns = paymentGroups,

            TotalCostOfGoodsSold = totalCogs,
            GrossProfit = grossProfit,
            ProfitMarginPercent = profitMarginPct,
            TotalOrderVouchers = totalOrderVouchers,
            TotalShippingSubsidy = totalShippingSubsidy,
            EstimatedOperatingProfit = operatingProfit,
            OperatingProfitMarginPercent = operatingMarginPct,
            TopProfitProducts = topProfitProducts,
            CategoryProfits = categoryProfits,

            TotalInventoryItems = totalInv,
            InStockInventoryItems = inStockInv,
            SoldInventoryItems = soldInv,
            DefectiveInventoryItems = defectiveInv,
            TotalInventoryAssetValue = invAssetVal,
            InventoryTurnoverRate = turnoverRate,
            DaysSalesOfInventory = dsi,
            AgingUnder30Days = agingUnder30,
            Aging30To60Days = aging30To60,
            Aging60To90Days = aging60To90,
            AgingOver90Days = agingOver90,
            StaleCapitalLocked = staleCapital,
            StaleInventoryItems = staleItemsList,
            CategoryTurnovers = categoryTurnovers,

            RecentTransactions = rangeOrders.Take(15).ToList(),
            RecentExportAuditLogs = exportLogs
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueChartData(
        string range = "30d",
        string granularity = "day",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var allInventory = await _inventoryRepo.GetAllAsync(cancellationToken);

        DateTime startDate;
        DateTime endDate = toDate.HasValue 
            ? toDate.Value.Date.AddDays(1).AddTicks(-1) 
            : DateTime.UtcNow;

        if (range == "custom" && fromDate.HasValue)
        {
            startDate = fromDate.Value.Date;
        }
        else
        {
            startDate = range switch
            {
                "7d" => DateTime.UtcNow.Date.AddDays(-7),
                "90d" => DateTime.UtcNow.Date.AddDays(-90),
                "year" => new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "all" => DateTime.MinValue,
                _ => DateTime.UtcNow.Date.AddDays(-30)
            };
        }

        var validOrders = allOrders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatus != OrderStatus.Cancelled)
            .ToList();

        var costDict = new Dictionary<string, decimal>();
        foreach (var inv in allInventory.Where(i => i.CostPrice > 0))
        {
            if (!costDict.ContainsKey(inv.ProductId))
            {
                costDict[inv.ProductId] = inv.CostPrice;
            }
        }

        var points = GenerateTimeSeriesPoints(validOrders, startDate, endDate, granularity, costDict);
        return Json(new { success = true, points });
    }

    [HttpGet]
    public async Task<IActionResult> ExportFinancialData(
        string type = "orders",
        string format = "csv",
        string range = "30d",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
        var allInventory = await _inventoryRepo.GetAllAsync(cancellationToken);
        var allProducts = await _productRepo.GetAllAsync(cancellationToken);

        DateTime startDate;
        DateTime endDate = toDate.HasValue 
            ? toDate.Value.Date.AddDays(1).AddTicks(-1) 
            : DateTime.UtcNow;

        if (range == "custom" && fromDate.HasValue)
        {
            startDate = fromDate.Value.Date;
        }
        else
        {
            startDate = range switch
            {
                "today" => DateTime.UtcNow.Date,
                "7d" => DateTime.UtcNow.Date.AddDays(-7),
                "90d" => DateTime.UtcNow.Date.AddDays(-90),
                "year" => new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "all" => DateTime.MinValue,
                _ => DateTime.UtcNow.Date.AddDays(-30)
            };
        }

        var filteredOrders = allOrders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var csvBuilder = new StringBuilder();
        // UTF-8 BOM for Microsoft Excel compatibility
        csvBuilder.Append('\uFEFF');

        string fileName;

        switch (type.ToLowerInvariant())
        {
            case "profit":
                // 2. Profit Margin & COGS by Product
                fileName = $"BaoCao_LoiNhuan_GiaVon_TechZone_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                csvBuilder.AppendLine("Mã Sản Phẩm,Tên Thiết Bị,SKU/Phiên Bản,Danh Mục,Số Lượng Bán,Doanh Thu Thuần (VND),Giá Vốn Hàng Bán COGS (VND),Lợi Nhuận Gộp (VND),Biên Lợi Nhuận Gộp (%)");

                var validOrders = filteredOrders.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();
                var prodDict = new Dictionary<string, (string Name, string Sku, string Category, int Qty, decimal Revenue, decimal Cost)>();

                foreach (var o in validOrders)
                {
                    foreach (var it in o.Items)
                    {
                        var prod = allProducts.FirstOrDefault(p => p.Id == it.ProductId);
                        var inv = allInventory.FirstOrDefault(i => i.ProductId == it.ProductId && i.CostPrice > 0);
                        var cost = inv?.CostPrice ?? (prod?.OriginalPrice > 0 ? prod.OriginalPrice : it.UnitPrice * 0.82m);

                        if (!prodDict.ContainsKey(it.ProductId))
                        {
                            prodDict[it.ProductId] = (it.ProductName, it.VariantSku, prod?.CategoryName ?? "Khác", 0, 0, 0);
                        }
                        var cur = prodDict[it.ProductId];
                        prodDict[it.ProductId] = (
                            cur.Name,
                            cur.Sku,
                            cur.Category,
                            cur.Qty + it.Quantity,
                            cur.Revenue + it.TotalPrice,
                            cur.Cost + (cost * it.Quantity)
                        );
                    }
                }

                foreach (var kvp in prodDict.OrderByDescending(p => p.Value.Revenue))
                {
                    var gross = Math.Max(0, kvp.Value.Revenue - kvp.Value.Cost);
                    var margin = kvp.Value.Revenue > 0 ? Math.Round((double)(gross / kvp.Value.Revenue) * 100, 2) : 0;
                    csvBuilder.AppendLine(string.Format(
                        "\"{0}\",\"{1}\",\"{2}\",\"{3}\",{4},{5},{6},{7},{8}%",
                        EscapeCsv(kvp.Key),
                        EscapeCsv(kvp.Value.Name),
                        EscapeCsv(kvp.Value.Sku),
                        EscapeCsv(kvp.Value.Category),
                        kvp.Value.Qty,
                        kvp.Value.Revenue,
                        kvp.Value.Cost,
                        gross,
                        margin
                    ));
                }
                break;

            case "inventory_aging":
                // 3. Inventory Aging & Om Alert
                fileName = $"BaoCao_TonKho_HangOm_TechZone_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                csvBuilder.AppendLine("Mã Serial/IMEI,Mã SP,Tên Thiết Bị,Phiên Bản/SKU,Vị Trí Lưu Kho,Nhà Cung Cấp,Ngày Nhập Kho,Số Ngày Tồn Đọng,Giá Vốn Lưu Kho (VND),Phân Loại Rủi Ro,Đề Xuất Xử Lý");

                var inStock = allInventory.Where(i => i.Status == InventoryStatus.InStock).OrderByDescending(i => i.ReceivedDate).ToList();
                var now = DateTime.UtcNow;

                foreach (var inv in inStock)
                {
                    var days = Math.Max(0, (int)(now - inv.ReceivedDate).TotalDays);
                    var severity = days > 90 ? "Rất Cao (Hàng om >90 ngày)" : (days > 60 ? "Cảnh báo (Tồn chậm 61-90 ngày)" : "An toàn");
                    var action = days > 90 
                        ? "Flash Sale giảm sâu -25% xả kho thu hồi vốn" 
                        : (days > 60 ? "Tặng voucher kích cầu hoặc combo phụ kiện" : "Kinh doanh luân chuyển bình thường");

                    csvBuilder.AppendLine(string.Format(
                        "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",{7},{8},\"{9}\",\"{10}\"",
                        EscapeCsv(inv.SerialNumber),
                        EscapeCsv(inv.ProductId),
                        EscapeCsv(inv.ProductName),
                        EscapeCsv(inv.VariantSku),
                        EscapeCsv(inv.WarehouseLocation),
                        EscapeCsv(inv.SupplierName ?? "Nhà phân phối"),
                        inv.ReceivedDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        days,
                        inv.CostPrice > 0 ? inv.CostPrice : 15000000m,
                        severity,
                        action
                    ));
                }
                break;

            case "financial_summary":
                // 4. Financial KPI Summary Balance
                fileName = $"BangCanDoi_ThuChi_TechZone_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                csvBuilder.AppendLine("Nhóm Chỉ Tiêu,Chỉ Số Tài Chính,Giá Trị,Đơn Vị Tính,Ghi Chú");

                var validForSum = filteredOrders.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();
                var grossSum = validForSum.Sum(o => o.TotalAmount);
                var netSum = validForSum.Sum(o => o.Subtotal - o.OrderDiscountAmount);
                var voucherSum = validForSum.Sum(o => o.OrderDiscountAmount);
                var shipSum = validForSum.Sum(o => o.ShippingFee);
                var shipDiscSum = validForSum.Sum(o => o.ShippingDiscountAmount);

                decimal cogsSum = 0;
                foreach (var o in validForSum)
                {
                    foreach (var it in o.Items)
                    {
                        var inv = allInventory.FirstOrDefault(i => i.ProductId == it.ProductId && i.CostPrice > 0);
                        cogsSum += (inv?.CostPrice ?? (it.UnitPrice * 0.82m)) * it.Quantity;
                    }
                }
                var grossProfitSum = Math.Max(0, netSum - cogsSum);
                var grossMarginPctSum = netSum > 0 ? Math.Round((double)(grossProfitSum / netSum) * 100, 2) : 0;
                var opProfitSum = Math.Max(0, grossProfitSum - voucherSum - shipDiscSum);
                var opMarginPctSum = netSum > 0 ? Math.Round((double)(opProfitSum / netSum) * 100, 2) : 0;

                csvBuilder.AppendLine($"\"Doanh Thu\",\"Tổng doanh thu gộp (GMV)\",{grossSum},\"VND\",\"Tổng tiền thanh toán từ đơn hàng\"");
                csvBuilder.AppendLine($"\"Doanh Thu\",\"Doanh thu thuần (Net Revenue)\",{netSum},\"VND\",\"Doanh thu sau trừ chiết khấu\"");
                csvBuilder.AppendLine($"\"Chi Phí\",\"Giá vốn hàng bán (COGS)\",{cogsSum},\"VND\",\"Tổng giá trị nhập kho của thiết bị đã bán\"");
                csvBuilder.AppendLine($"\"Chi Phí\",\"Trợ giá Voucher khuyến mãi\",{voucherSum},\"VND\",\"Ngân sách khuyến mãi shop tài trợ\"");
                csvBuilder.AppendLine($"\"Chi Phí\",\"Bù lỗ chi phí vận chuyển\",{shipDiscSum},\"VND\",\"Hỗ trợ phí ship cho người mua\"");
                csvBuilder.AppendLine($"\"Lợi Nhuận\",\"Lợi nhuận gộp (Gross Profit)\",{grossProfitSum},\"VND\",\"Net Revenue - COGS\"");
                csvBuilder.AppendLine($"\"Lợi Nhuận\",\"Biên lợi nhuận gộp (Gross Margin)\",{grossMarginPctSum},\"%\",\"Gross Profit / Net Revenue\"");
                csvBuilder.AppendLine($"\"Lợi Nhuận\",\"Lợi nhuận ròng vận hành\",{opProfitSum},\"VND\",\"Gross Profit - Voucher - Ship\"");
                csvBuilder.AppendLine($"\"Lợi Nhuận\",\"Biên lợi nhuận vận hành\",{opMarginPctSum},\"%\",\"Operating Profit / Net Revenue\"");
                csvBuilder.AppendLine($"\"Đơn Hàng\",\"Tổng đơn hàng phát sinh\",{filteredOrders.Count},\"Đơn\",\"Bao gồm mọi trạng thái\"");
                csvBuilder.AppendLine($"\"Đơn Hàng\",\"Đơn giao thành công\",{filteredOrders.Count(o => o.OrderStatus == OrderStatus.Delivered)},\"Đơn\",\"Khách hàng đã nhận và đối soát\"");
                csvBuilder.AppendLine($"\"Đơn Hàng\",\"Đơn hủy\",{filteredOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled)},\"Đơn\",\"Tỷ lệ hủy: {(filteredOrders.Count > 0 ? Math.Round((double)filteredOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled)/filteredOrders.Count*100, 1) : 0)}%\"");
                break;

            case "orders":
            default:
                // 1. Detailed Retail Orders & VAT Invoices
                fileName = $"SoChiTiet_DonHang_HoaDon_TechZone_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                csvBuilder.AppendLine("Mã Đơn Hàng,Ngày Tạo,Khách Hàng,Số Điện Thoại,Email,Kênh Bán,Số Lượng Thiết Bị,Tạm Tính (VND),Giảm Giá Đơn (VND),Giảm Giá Ship (VND),Phí Vận Chuyển (VND),Tổng Tiền (VND),Phương Thức TT,Trạng Thái TT,Trạng Thái Đơn,Đơn Vị Giao Hàng,Mã Vận Đơn,Yêu Cầu VAT,Tên Công Ty,Mã Số Thuế");

                foreach (var order in filteredOrders)
                {
                    csvBuilder.AppendLine(string.Format(
                        "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",{6},{7},{8},{9},{10},{11},\"{12}\",\"{13}\",\"{14}\",\"{15}\",\"{16}\",\"{17}\",\"{18}\",\"{19}\"",
                        EscapeCsv(order.OrderCode),
                        order.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                        EscapeCsv(order.CustomerName),
                        EscapeCsv(order.CustomerPhone),
                        EscapeCsv(order.CustomerEmail),
                        EscapeCsv(string.IsNullOrWhiteSpace(order.SourceChannel) ? "Website" : order.SourceChannel),
                        order.Items.Sum(i => i.Quantity),
                        order.Subtotal,
                        order.OrderDiscountAmount,
                        order.ShippingDiscountAmount,
                        order.ShippingFee,
                        order.TotalAmount,
                        order.PaymentMethod,
                        order.PaymentStatus,
                        order.OrderStatus,
                        EscapeCsv(order.CarrierName),
                        EscapeCsv(order.TrackingNumber ?? "Chưa phát sinh"),
                        order.RequireVatInvoice ? "Có" : "Không",
                        EscapeCsv(order.CompanyName ?? string.Empty),
                        EscapeCsv(order.TaxId ?? string.Empty)
                    ));
                }
                break;
        }

        // Log audit event
        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "ExportFinancialData",
            Module = "Finance",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xuất báo cáo tài chính/kế toán loại '{type}' ({filteredOrders.Count} đơn/mục) - Chu kỳ: {range} - Từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}"
        }, cancellationToken);

        var fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        return File(fileBytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportFinancialCsv(
        string range = "all",
        CancellationToken cancellationToken = default)
    {
        // Legacy redirect to keep backwards compatibility with previous links
        return await ExportFinancialData(type: "orders", format: "csv", range: range, cancellationToken: cancellationToken);
    }

    private static List<TimeSeriesRevenuePoint> GenerateTimeSeriesPoints(
        List<Order> validOrders,
        DateTime startDate,
        DateTime endDate,
        string granularity,
        Dictionary<string, decimal> costDict)
    {
        var points = new List<TimeSeriesRevenuePoint>();

        switch (granularity.ToLowerInvariant())
        {
            case "week":
                // Group by weekly intervals (last 8-12 weeks)
                var currentWeekStart = startDate;
                while (currentWeekStart <= endDate)
                {
                    var weekEnd = currentWeekStart.AddDays(7).AddTicks(-1);
                    var weekOrders = validOrders.Where(o => o.CreatedAt >= currentWeekStart && o.CreatedAt <= weekEnd).ToList();
                    
                    var weekRev = weekOrders.Sum(o => o.TotalAmount);
                    var weekNet = weekOrders.Sum(o => o.Subtotal - o.OrderDiscountAmount);
                    decimal weekCost = 0;
                    foreach (var o in weekOrders)
                    {
                        foreach (var it in o.Items)
                        {
                            var unitCost = costDict.GetValueOrDefault(it.ProductId, it.UnitPrice * 0.82m);
                            weekCost += unitCost * it.Quantity;
                        }
                    }

                    var cal = CultureInfo.CurrentCulture.Calendar;
                    var weekNum = cal.GetWeekOfYear(currentWeekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                    points.Add(new TimeSeriesRevenuePoint
                    {
                        PeriodLabel = $"Tuần {weekNum} ({currentWeekStart:dd/MM})",
                        DateKey = currentWeekStart,
                        GrossRevenue = weekRev,
                        NetRevenue = weekNet,
                        Cost = weekCost,
                        Profit = Math.Max(0, weekNet - weekCost),
                        OrderCount = weekOrders.Count
                    });

                    currentWeekStart = currentWeekStart.AddDays(7);
                }
                break;

            case "month":
                // Group by months
                var currentMonthStart = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                while (currentMonthStart <= endDate)
                {
                    var monthEnd = currentMonthStart.AddMonths(1).AddTicks(-1);
                    var monthOrders = validOrders.Where(o => o.CreatedAt >= currentMonthStart && o.CreatedAt <= monthEnd).ToList();

                    var monthRev = monthOrders.Sum(o => o.TotalAmount);
                    var monthNet = monthOrders.Sum(o => o.Subtotal - o.OrderDiscountAmount);
                    decimal monthCost = 0;
                    foreach (var o in monthOrders)
                    {
                        foreach (var it in o.Items)
                        {
                            var unitCost = costDict.GetValueOrDefault(it.ProductId, it.UnitPrice * 0.82m);
                            monthCost += unitCost * it.Quantity;
                        }
                    }

                    points.Add(new TimeSeriesRevenuePoint
                    {
                        PeriodLabel = $"Tháng {currentMonthStart.Month:D2}/{currentMonthStart.Year}",
                        DateKey = currentMonthStart,
                        GrossRevenue = monthRev,
                        NetRevenue = monthNet,
                        Cost = monthCost,
                        Profit = Math.Max(0, monthNet - monthCost),
                        OrderCount = monthOrders.Count
                    });

                    currentMonthStart = currentMonthStart.AddMonths(1);
                }
                break;

            case "quarter":
                // Group by quarters (Q1, Q2, Q3, Q4)
                var currentYear = startDate.Year;
                var endYear = endDate.Year;
                for (int y = currentYear; y <= endYear; y++)
                {
                    for (int q = 1; q <= 4; q++)
                    {
                        var qStart = new DateTime(y, (q - 1) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        var qEnd = qStart.AddMonths(3).AddTicks(-1);
                        if (qStart > endDate || qEnd < startDate) continue;

                        var qOrders = validOrders.Where(o => o.CreatedAt >= qStart && o.CreatedAt <= qEnd).ToList();
                        var qRev = qOrders.Sum(o => o.TotalAmount);
                        var qNet = qOrders.Sum(o => o.Subtotal - o.OrderDiscountAmount);
                        decimal qCost = 0;
                        foreach (var o in qOrders)
                        {
                            foreach (var it in o.Items)
                            {
                                var unitCost = costDict.GetValueOrDefault(it.ProductId, it.UnitPrice * 0.82m);
                                qCost += unitCost * it.Quantity;
                            }
                        }

                        points.Add(new TimeSeriesRevenuePoint
                        {
                            PeriodLabel = $"Quý {q}/{y}",
                            DateKey = qStart,
                            GrossRevenue = qRev,
                            NetRevenue = qNet,
                            Cost = qCost,
                            Profit = Math.Max(0, qNet - qCost),
                            OrderCount = qOrders.Count
                        });
                    }
                }
                break;

            case "day":
            default:
                // Group by day (capped to reasonable span, e.g. last 30 days)
                var totalDays = (int)(endDate.Date - startDate.Date).TotalDays + 1;
                if (totalDays > 60)
                {
                    // If span is large, adjust start date to last 30 days for daily view to keep graph crisp
                    startDate = endDate.Date.AddDays(-30);
                    totalDays = 31;
                }

                for (int i = 0; i < totalDays; i++)
                {
                    var targetDay = startDate.Date.AddDays(i);
                    var nextDay = targetDay.AddDays(1);
                    var dayOrders = validOrders.Where(o => o.CreatedAt >= targetDay && o.CreatedAt < nextDay).ToList();

                    var dayRev = dayOrders.Sum(o => o.TotalAmount);
                    var dayNet = dayOrders.Sum(o => o.Subtotal - o.OrderDiscountAmount);
                    decimal dayCost = 0;
                    foreach (var o in dayOrders)
                    {
                        foreach (var it in o.Items)
                        {
                            var unitCost = costDict.GetValueOrDefault(it.ProductId, it.UnitPrice * 0.82m);
                            dayCost += unitCost * it.Quantity;
                        }
                    }

                    points.Add(new TimeSeriesRevenuePoint
                    {
                        PeriodLabel = targetDay.ToString("dd/MM"),
                        DateKey = targetDay,
                        GrossRevenue = dayRev,
                        NetRevenue = dayNet,
                        Cost = dayCost,
                        Profit = Math.Max(0, dayNet - dayCost),
                        OrderCount = dayOrders.Count
                    });
                }
                break;
        }

        return points;
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        return field.Replace("\"", "\"\"");
    }
}
