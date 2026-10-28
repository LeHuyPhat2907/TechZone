using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class StocktakeSession : BaseEntity, IAggregateRoot
{
    [BsonElement("sessionCode")]
    public string SessionCode { get; set; } = $"ST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

    [BsonElement("title")]
    public string Title { get; set; } = "Kiểm kê định kỳ Kho";

    [BsonElement("warehouseLocation")]
    public string WarehouseLocation { get; set; } = "Tổng Kho Tân Bình - TP.HCM";

    [BsonElement("conductedByStaff")]
    public string ConductedByStaff { get; set; } = "Thủ Kho";

    [BsonElement("conductedDate")]
    public DateTime ConductedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("totalSystemItems")]
    public int TotalSystemItems { get; set; } = 0;

    [BsonElement("totalCountedItems")]
    public int TotalCountedItems { get; set; } = 0;

    [BsonElement("discrepancyCount")]
    public int DiscrepancyCount { get; set; } = 0; // Số chênh lệch (+ Thừa / - Thiếu)

    [BsonElement("status")]
    public string Status { get; set; } = "Completed"; // InProgress, Completed, Audited

    [BsonElement("items")]
    public List<StocktakeDiscrepancyItem> Items { get; set; } = new();

    [BsonElement("auditNotes")]
    public string? AuditNotes { get; set; }
}

[BsonIgnoreExtraElements]
public class StocktakeDiscrepancyItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("systemQty")]
    public int SystemQty { get; set; } = 0;

    [BsonElement("countedQty")]
    public int CountedQty { get; set; } = 0;

    [BsonElement("diffQty")]
    public int DiffQty => CountedQty - SystemQty;

    [BsonElement("scannedSerials")]
    public List<string> ScannedSerials { get; set; } = new();

    [BsonElement("missingSerials")]
    public List<string> MissingSerials { get; set; } = new();

    [BsonElement("discrepancyReason")]
    public string? DiscrepancyReason { get; set; }
}
