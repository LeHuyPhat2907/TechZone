using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class WarehousePortalViewModel
{
    public string ActiveTab { get; set; } = "inbound"; // "inbound", "imei", "outbound", "stocktake", "transfer", "returns"

    // Current Operator Info
    public string CurrentStaffId { get; set; } = string.Empty;
    public string CurrentStaffName { get; set; } = "Thủ Kho";
    public string CurrentWarehouseName { get; set; } = "Tổng Kho Tân Bình - TP.HCM";

    // 1. KPI Operational Metrics
    public int PendingPoCount { get; set; }
    public int InStockCount { get; set; }
    public int LowStockAlertCount { get; set; }
    public int OutboundPendingOrdersCount { get; set; }
    public int ActiveTransfersCount { get; set; }
    public int DamagedDefectiveCount { get; set; }

    // 2. Section 3.1: Inbound Logistics (inbound.view_po, inbound.create_receipt, inbound.inspect, inbound.scan_imei_batch, inbound.confirm)
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
    public List<StockSlip> InboundReceipts { get; set; } = new();
    public List<Supplier> Suppliers { get; set; } = new();
    public string? PoFilterStatus { get; set; }
    public string? PoSearchQuery { get; set; }

    // 3. Section 3.2: IMEI / Serial Tracking (serial_imei.view, serial_imei.bind_sku, serial_imei.update_status, serial_imei.print_barcode)
    public List<InventoryItem> InventoryItems { get; set; } = new();
    public List<Product> AvailableProducts { get; set; } = new();
    public string? ImeiSearchQuery { get; set; }
    public string? ImeiFilterStatus { get; set; }
    public string? ImeiFilterSku { get; set; }

    // 4. Section 3.3: Outbound & Fulfillment (outbound.view_pending, outbound.create_pick_list, outbound.scan_dispatch, outbound.print_shipping_label, outbound.handover_carrier)
    public List<Order> OutboundPendingOrders { get; set; } = new();
    public List<PickListItemDto> PickList { get; set; } = new();
    public string? OutboundFilterCarrier { get; set; }
    public string? OutboundSearchQuery { get; set; }

    // 5. Section 3.4: Stocktake, Transfer & Reverse Logistics (stocktake.perform, stocktake.log_discrepancy, inventory.adjust_damaged, inventory.threshold_alert, transfer.create/receive, returns.receive/inspect/restock)
    public List<StocktakeSession> StocktakeSessions { get; set; } = new();
    public List<StockTransfer> StockTransfers { get; set; } = new();
    public List<Product> SafetyStockAlertProducts { get; set; } = new();
    public List<InventoryItem> ReverseLogisticsItems { get; set; } = new();
}

public class PickListItemDto
{
    public string WarehouseLocation { get; set; } = "Kệ A1-Kho Tổng";
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int InStockQuantity { get; set; }
    public List<string> OrderCodes { get; set; } = new();
}

public class BarcodePrintItemDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? Imei2 { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = "Kệ A1";
    public string BatchCode { get; set; } = string.Empty;
}
