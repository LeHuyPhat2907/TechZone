using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class SparePartRequest : BaseEntity, IAggregateRoot
{
    [BsonElement("requestCode")]
    public string RequestCode { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpper();

    [BsonElement("ticketId")]
    public string? TicketId { get; set; }

    [BsonElement("ticketCode")]
    public string? TicketCode { get; set; }

    [BsonElement("partId")]
    public string PartId { get; set; } = string.Empty;

    [BsonElement("partCode")]
    public string PartCode { get; set; } = string.Empty;

    [BsonElement("partName")]
    public string PartName { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("type")]
    public string Type { get; set; } = "ExportForRepair"; // "ExportForRepair" (Xuất sửa chữa) or "ReturnDefective" (Trả linh kiện hỏng về kho)

    [BsonElement("technicianId")]
    public string? TechnicianId { get; set; }

    [BsonElement("technicianName")]
    public string? TechnicianName { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Approved"; // Approved, Returned, Disposed

    [BsonElement("notes")]
    public string? Notes { get; set; }
}
