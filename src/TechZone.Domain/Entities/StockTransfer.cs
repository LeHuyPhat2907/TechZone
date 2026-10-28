using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class StockTransfer : BaseEntity, IAggregateRoot
{
    [BsonElement("transferCode")]
    public string TransferCode { get; set; } = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

    [BsonElement("fromWarehouse")]
    public string FromWarehouse { get; set; } = "Tổng Kho Tân Bình - TP.HCM";

    [BsonElement("toWarehouse")]
    public string ToWarehouse { get; set; } = "Chi Nhánh Showroom Quận 1";

    [BsonElement("createdByStaff")]
    public string CreatedByStaff { get; set; } = "Thủ Kho";

    [BsonElement("carrierName")]
    public string CarrierName { get; set; } = "Đội Giao Hàng Nội Bộ TechZone";

    [BsonElement("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [BsonElement("totalItems")]
    public int TotalItems { get; set; } = 0;

    [BsonElement("status")]
    public string Status { get; set; } = "InTransit"; // Pending, InTransit, Received, Cancelled

    [BsonElement("sentDate")]
    public DateTime SentDate { get; set; } = DateTime.UtcNow;

    [BsonElement("receivedDate")]
    public DateTime? ReceivedDate { get; set; }

    [BsonElement("receivedByStaff")]
    public string? ReceivedByStaff { get; set; }

    [BsonElement("items")]
    public List<StockTransferItem> Items { get; set; } = new();

    [BsonElement("notes")]
    public string? Notes { get; set; }
}

[BsonIgnoreExtraElements]
public class StockTransferItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("serialNumbers")]
    public List<string> SerialNumbers { get; set; } = new();
}
