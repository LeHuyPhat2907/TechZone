using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class PurchaseOrder : BaseEntity, IAggregateRoot
{
    [BsonElement("poCode")]
    public string PoCode { get; set; } = $"PO-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

    [BsonElement("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [BsonElement("supplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [BsonElement("targetWarehouse")]
    public string TargetWarehouse { get; set; } = "Tổng Kho Tân Bình - TP.HCM";

    [BsonElement("orderDate")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [BsonElement("expectedDeliveryDate")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [BsonElement("approvedBy")]
    public string ApprovedBy { get; set; } = "Ban Giám Đốc";

    [BsonElement("approvedAt")]
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public string Status { get; set; } = "Approved"; // Approved (Chờ nhận), Inspecting (Đang kiểm định), FullyReceived (Đã nhập đủ), Cancelled

    [BsonElement("totalQuantity")]
    public int TotalQuantity { get; set; } = 0;

    [BsonElement("receivedQuantity")]
    public int ReceivedQuantity { get; set; } = 0;

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; } = 0;

    [BsonElement("items")]
    public List<PurchaseOrderItem> Items { get; set; } = new();

    [BsonElement("notes")]
    public string? Notes { get; set; }
}

[BsonIgnoreExtraElements]
public class PurchaseOrderItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("orderedQuantity")]
    public int OrderedQuantity { get; set; } = 1;

    [BsonElement("receivedQuantity")]
    public int ReceivedQuantity { get; set; } = 0;

    [BsonElement("unitCost")]
    public decimal UnitCost { get; set; } = 0;

    [BsonElement("subTotal")]
    public decimal SubTotal => UnitCost * OrderedQuantity;
}
