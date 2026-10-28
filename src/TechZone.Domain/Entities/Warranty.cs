using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

public class Warranty : BaseEntity, IAggregateRoot
{
    [BsonElement("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [BsonElement("imei")]
    public string? Imei { get; set; }

    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("variantName")]
    public string VariantName { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("orderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string? CustomerId { get; set; }

    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

    [BsonElement("warrantyMonths")]
    public int WarrantyMonths { get; set; } = 12;

    [BsonElement("status")]
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Active;

    [BsonElement("claims")]
    public List<WarrantyClaim> Claims { get; set; } = new();

    [BsonIgnore]
    public bool IsActive => Status == WarrantyStatus.Active && DateTime.UtcNow <= EndDate;

    [BsonIgnore]
    public int DaysRemaining => Math.Max(0, (EndDate - DateTime.UtcNow).Days);
}

public class WarrantyClaim
{
    [BsonElement("claimCode")]
    public string ClaimCode { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();

    [BsonElement("faultDescription")]
    public string FaultDescription { get; set; } = string.Empty;

    [BsonElement("receivedDate")]
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("completedDate")]
    public DateTime? CompletedDate { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Processing"; // Processing, Repaired, ReplacedDevice, Rejected

    [BsonElement("technicianName")]
    public string? TechnicianName { get; set; }

    [BsonElement("technicianNotes")]
    public string? TechnicianNotes { get; set; }

    [BsonElement("replacedParts")]
    public List<string> ReplacedParts { get; set; } = new();
}
