using TechZone.Application.Common.Models;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class InventoryManagementViewModel
{
    public string ActiveTab { get; set; } = "imei"; // "imei", "stock", "slips", "suppliers"

    // Collections
    public PagedResult<InventoryItem> Items { get; set; } = new();
    public IReadOnlyList<Product> Products { get; set; } = Array.Empty<Product>();
    public IReadOnlyList<StockSlip> StockSlips { get; set; } = Array.Empty<StockSlip>();
    public IReadOnlyList<Supplier> Suppliers { get; set; } = Array.Empty<Supplier>();
    public List<ProductStockOverviewDto> StockOverview { get; set; } = new();

    // Filters
    public string? SearchKeyword { get; set; }
    public InventoryStatus? StatusFilter { get; set; }
    public string? SelectedProductId { get; set; }
    public string? SelectedSupplierId { get; set; }
    public StockSlipType? SlipTypeFilter { get; set; }

    // KPI Summary
    public int TotalItems { get; set; }
    public int InStockCount { get; set; }
    public int SoldCount { get; set; }
    public int DamagedCount { get; set; }
    public int ReservedCount { get; set; }

    // Financial & Operational Inventory KPIs
    public decimal TotalInventoryValue { get; set; } // Tổng giá trị tồn kho vật lý (VNĐ)
    public decimal TotalSupplierDebt { get; set; }    // Tổng công nợ hiện tại phải trả NCC (VNĐ)
    public int LowStockProductsCount { get; set; }    // Sản phẩm cảnh báo tồn kho thấp (<= 5)
    public int TotalSlipsCount { get; set; }          // Tổng số phiếu xuất nhập
}

public class ProductStockOverviewDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }      // Tồn kho vật lý
    public int AvailableQuantity { get; set; }  // Tồn kho khả dụng
    public int ReservedQuantity { get; set; }   // Giữ đơn
    public int DefectiveCount { get; set; }     // Hàng lỗi cách ly
    public bool IsLowStock => StockQuantity <= 5;
}

public class ImportStockDto
{
    public string ProductId { get; set; } = string.Empty;
    public string? SupplierId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public string WarehouseLocation { get; set; } = "Kệ A1-Kho Tổng";
    public string SerialNumbersText { get; set; } = string.Empty; // Mỗi dòng 1 Serial/IMEI
    public string? Note { get; set; }
}

public class ExportStockDto
{
    public string ProductId { get; set; } = string.Empty;
    public string? SupplierId { get; set; }
    public StockExportReason ExportReason { get; set; } = StockExportReason.ReturnToSupplier;
    public string SlipCode { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = "Kho Tổng";
    public string SerialNumbersText { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}

public class StockAdjustmentDto
{
    public string ProductId { get; set; } = string.Empty;
    public int ActualPhysicalStock { get; set; }
    public string Reason { get; set; } = "Kiểm kê định kỳ phát hiện chênh lệch";
    public string? Note { get; set; }
}

public class SupplierInputDto
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string PaymentTerms { get; set; } = "Net 30 ngày";
    public decimal TotalDebt { get; set; }
    public string? Note { get; set; }
}

public class SupplierPaymentDto
{
    public string SupplierId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Chuyển khoản ngân hàng (Ủy nhiệm chi)";
    public string? Note { get; set; }
}
