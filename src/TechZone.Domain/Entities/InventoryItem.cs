using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class InventoryItem : BaseEntity, IAggregateRoot
{
    [BsonElement("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty; // S/N hoặc IMEI duy nhất

    [BsonElement("imei2")]
    public string? Imei2 { get; set; } // IMEI 2 cho máy 2 sim

    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("variantName")]
    public string VariantName { get; set; } = string.Empty;

    [BsonElement("status")]
    public InventoryStatus Status { get; set; } = InventoryStatus.InStock;

    [BsonElement("warehouseLocation")]
    public string WarehouseLocation { get; set; } = "Kệ A1-Kho Tổng";

    [BsonElement("costPrice")]
    public decimal CostPrice { get; set; } // Giá vốn nhập kho

    [BsonElement("importBatchCode")]
    public string ImportBatchCode { get; set; } = string.Empty; // Mã lô hàng nhập (GRN)

    [BsonElement("supplierId")]
    public string? SupplierId { get; set; }

    [BsonElement("supplierName")]
    public string? SupplierName { get; set; }

    [BsonElement("receivedDate")]
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("assignedOrderId")]
    public string? AssignedOrderId { get; set; }

    [BsonElement("assignedOrderCode")]
    public string? AssignedOrderCode { get; set; }

    [BsonElement("shippedDate")]
    public DateTime? ShippedDate { get; set; }

    [BsonElement("exportSlipCode")]
    public string? ExportSlipCode { get; set; } // Mã phiếu xuất kho nếu đã xuất (GDN)

    [BsonElement("warrantyId")]
    public string? WarrantyId { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }
}

public class InventoryTransaction : BaseEntity, IAggregateRoot
{
    [BsonElement("transactionCode")]
    public string TransactionCode { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpper();

    [BsonElement("type")]
    public InventoryTransactionType Type { get; set; }

    [BsonElement("serialNumber")]
    public string? SerialNumber { get; set; }

    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("orderId")]
    public string? OrderId { get; set; }

    [BsonElement("orderCode")]
    public string? OrderCode { get; set; }

    [BsonElement("performedByUserId")]
    public string? PerformedByUserId { get; set; }

    [BsonElement("performedByName")]
    public string? PerformedByName { get; set; }

    [BsonElement("note")]
    public string? Note { get; set; }
}
