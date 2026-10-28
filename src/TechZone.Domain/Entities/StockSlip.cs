using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class StockSlip : BaseEntity, IAggregateRoot
{
    [BsonElement("slipCode")]
    public string SlipCode { get; set; } = string.Empty; // Mã phiếu (GRN-..., GDN-...)

    [BsonElement("type")]
    public StockSlipType Type { get; set; } = StockSlipType.Import; // Nhập hoặc Xuất

    [BsonElement("exportReason")]
    public StockExportReason? ExportReason { get; set; } // Lý do xuất nếu Type == Export

    [BsonElement("supplierId")]
    public string? SupplierId { get; set; } // Nhà cung cấp nếu nhập hoặc xuất trả

    [BsonElement("supplierName")]
    public string? SupplierName { get; set; }

    [BsonElement("warehouseLocation")]
    public string WarehouseLocation { get; set; } = "Kệ A1-Kho Tổng";

    [BsonElement("createdByUserId")]
    public string? CreatedByUserId { get; set; }

    [BsonElement("createdByName")]
    public string? CreatedByName { get; set; }

    [BsonElement("approvedByUserId")]
    public string? ApprovedByUserId { get; set; }

    [BsonElement("approvedByName")]
    public string? ApprovedByName { get; set; }

    [BsonElement("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    [BsonElement("status")]
    public StockSlipStatus Status { get; set; } = StockSlipStatus.Approved;

    [BsonElement("totalQuantity")]
    public int TotalQuantity { get; set; } = 0;

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; } = 0; // Tổng giá trị tiền hàng (VNĐ)

    [BsonElement("items")]
    public List<StockSlipItem> Items { get; set; } = new();

    [BsonElement("note")]
    public string? Note { get; set; }
}

[BsonIgnoreExtraElements]
public class StockSlipItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; } = 0; // Đơn giá vốn hoặc giá xuất

    [BsonElement("subTotal")]
    public decimal SubTotal { get; set; } = 0;

    [BsonElement("serialNumbers")]
    public List<string> SerialNumbers { get; set; } = new(); // Danh sách IMEI/Serial tương ứng
}
