using TechZone.Domain.Entities;

namespace TechZone.Web.Areas.Admin.Models;

public class ReportsViewModel
{
    // Navigation & Filter State
    public string ActiveTab { get; set; } = "revenue"; // "revenue", "profit", "inventory", "export"
    public string DateRange { get; set; } = "30d"; // "7d", "30d", "90d", "year", "custom", "all"
    public string Granularity { get; set; } = "day"; // "day", "week", "month", "quarter"
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // ==========================================
    // 1. REVENUE METRICS (report.revenue)
    // ==========================================
    public decimal GrossRevenue { get; set; } // Doanh thu gộp (tổng tiền thanh toán)
    public decimal NetRevenue { get; set; } // Doanh thu thuần (sau trừ giảm giá)
    public decimal TotalDiscounts { get; set; } // Tổng giá trị voucher & khuyến mãi đã áp dụng
    public decimal TotalShippingCollected { get; set; } // Tổng cước vận chuyển thu của khách
    public decimal AverageOrderValue { get; set; } // AOV (Giá trị đơn bình quân)
    
    // Order Volumes & Conversion
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public double DeliverySuccessRate { get; set; } // % Đơn giao thành công
    public double CancellationRate { get; set; } // % Đơn bị hủy

    // Time Series Data for Revenue Chart (Daily, Weekly, Monthly, Quarterly)
    public List<TimeSeriesRevenuePoint> ChartPoints { get; set; } = new();

    // Sales Channel Breakdown (Website, Telesales, Fanpage, DirectStore...)
    public List<ChannelRevenueMetric> ChannelBreakdowns { get; set; } = new();

    // Payment Method Breakdown (COD, VNPay, BankTransfer, MoMo...)
    public List<PaymentMethodMetric> PaymentBreakdowns { get; set; } = new();

    // ==========================================
    // 2. PROFIT MARGIN METRICS (report.profit_margin)
    // ==========================================
    public decimal TotalCostOfGoodsSold { get; set; } // Giá vốn nhập kho (COGS)
    public decimal GrossProfit { get; set; } // Lợi nhuận gộp (NetRevenue - COGS)
    public double ProfitMarginPercent { get; set; } // Biên lợi nhuận gộp % = (GrossProfit / NetRevenue) * 100
    public decimal TotalOrderVouchers { get; set; } // Trợ giá voucher đơn hàng
    public decimal TotalShippingSubsidy { get; set; } // Bù lỗ phí vận chuyển (nếu có)
    public decimal EstimatedOperatingProfit { get; set; } // Lợi nhuận sau trừ voucher & phí ship
    public double OperatingProfitMarginPercent { get; set; } // Biên lợi nhuận ròng vận hành %

    // Top Profit Products & Category Profitability
    public List<ProductProfitMetric> TopProfitProducts { get; set; } = new();
    public List<CategoryProfitMetric> CategoryProfits { get; set; } = new();

    // ==========================================
    // 3. INVENTORY TURNOVER & AGING (report.inventory_turnover)
    // ==========================================
    public int TotalInventoryItems { get; set; }
    public int InStockInventoryItems { get; set; }
    public int SoldInventoryItems { get; set; }
    public int DefectiveInventoryItems { get; set; }
    public decimal TotalInventoryAssetValue { get; set; } // Tổng giá trị vốn kho còn lưu
    public double InventoryTurnoverRate { get; set; } // Tỉ lệ quay vòng hàng tồn = COGS / AvgInventoryVal (hoặc % sold/total)
    public double DaysSalesOfInventory { get; set; } // Số ngày luân chuyển tồn kho (DSI) = 365 / Turnover
    
    // Aging Breakdown
    public int AgingUnder30Days { get; set; } // Tồn kho an toàn (< 30 ngày)
    public int Aging30To60Days { get; set; } // Tồn kho trung bình (30 - 60 ngày)
    public int Aging60To90Days { get; set; } // Cảnh báo tồn chậm (61 - 90 ngày)
    public int AgingOver90Days { get; set; } // Hàng om, tồn lâu (> 90 ngày)
    public decimal StaleCapitalLocked { get; set; } // Vốn tồn đọng trong hàng > 60 ngày

    // Stale & Om Inventory Alert Items (Cảnh báo danh sách máy cụ thể)
    public List<StaleInventoryAlertItem> StaleInventoryItems { get; set; } = new();

    // Category Turnover Breakdown
    public List<CategoryTurnoverMetric> CategoryTurnovers { get; set; } = new();

    // ==========================================
    // 4. EXPORT & AUDIT TRAIL (export.financial_data)
    // ==========================================
    public List<Order> RecentTransactions { get; set; } = new();
    public List<SecurityAuditLog> RecentExportAuditLogs { get; set; } = new();
}

public class TimeSeriesRevenuePoint
{
    public string PeriodLabel { get; set; } = string.Empty; // e.g. "20/10", "Tuần 42", "Tháng 10", "Q3"
    public DateTime DateKey { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int OrderCount { get; set; }
}

public class ChannelRevenueMetric
{
    public string ChannelName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public double Percentage { get; set; }
}

public class PaymentMethodMetric
{
    public string MethodName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
}

public class ProductProfitMetric
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public double MarginPercent { get; set; }
}

public class CategoryProfitMetric
{
    public string CategoryName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public double MarginPercent { get; set; }
}

public class CategoryTurnoverMetric
{
    public string CategoryName { get; set; } = string.Empty;
    public int TotalStock { get; set; }
    public int InStock { get; set; }
    public int SoldUnits { get; set; }
    public decimal AssetValue { get; set; }
    public double TurnoverRate { get; set; }
}

public class StaleInventoryAlertItem
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public DateTime ReceivedDate { get; set; }
    public int AgingDays { get; set; }
    public string AlertSeverity { get; set; } = "Warning"; // "Warning" (61-90 days), "Critical" (>90 days)
    public string Recommendation { get; set; } = string.Empty;
}
