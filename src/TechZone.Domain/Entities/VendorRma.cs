using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class VendorRma : BaseEntity, IAggregateRoot
{
    [BsonElement("vendorRmaCode")]
    public string VendorRmaCode { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpper();

    [BsonElement("ticketId")]
    public string TicketId { get; set; } = string.Empty;

    [BsonElement("ticketCode")]
    public string TicketCode { get; set; } = string.Empty;

    [BsonElement("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("vendorName")]
    public string VendorName { get; set; } = "Apple Authorized Service Provider (Thakral One)"; // AASP, ROG Asus, Synnex FPT, Dell ProSupport

    [BsonElement("vendorTrackingCode")]
    public string? VendorTrackingCode { get; set; }

    [BsonElement("carrierName")]
    public string? CarrierName { get; set; } = "Giao Hàng Nhanh (GHN)";

    [BsonElement("carrierTrackingNumber")]
    public string? CarrierTrackingNumber { get; set; }

    [BsonElement("sentDate")]
    public DateTime SentDate { get; set; } = DateTime.UtcNow;

    [BsonElement("expectedReturnDate")]
    public DateTime? ExpectedReturnDate { get; set; }

    [BsonElement("receivedBackDate")]
    public DateTime? ReceivedBackDate { get; set; }

    [BsonElement("vendorDiagnosis")]
    public string? VendorDiagnosis { get; set; }

    [BsonElement("vendorResolution")]
    public string? VendorResolution { get; set; } // ReplacedNewDevice, ReplacedPart, RejectedFault

    [BsonElement("status")]
    public string Status { get; set; } = "SentToVendor"; // SentToVendor, VendorInspecting, VendorRepaired, ReceivedBack, Completed

    [BsonElement("technicianNotes")]
    public string? TechnicianNotes { get; set; }
}
