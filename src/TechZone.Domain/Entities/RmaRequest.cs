using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

public class RmaRequest : BaseEntity, IAggregateRoot
{
    [BsonElement("rmaCode")]
    public string RmaCode { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpper();

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("orderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string? CustomerId { get; set; }

    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [BsonElement("reasonType")]
    public string ReasonType { get; set; } = "HardwareDefect"; // HardwareDefect, DOA, DamagedInTransit, ScreenFault, BatteryIssue, LiquidDamage, PowerFailure

    [BsonElement("customerDescription")]
    public string CustomerDescription { get; set; } = string.Empty;

    // 2.2 ticket.create: Physical inspection notes & accessories received
    [BsonElement("physicalConditionNotes")]
    public string PhysicalConditionNotes { get; set; } = "Thân máy nguyên vẹn, ít trầy xước viền"; // Ngoại quan tiếp nhận

    [BsonElement("accessoriesIncluded")]
    public string AccessoriesIncluded { get; set; } = "Thân máy, Hộp zin"; // Phụ kiện đi kèm

    [BsonElement("evidenceMediaUrls")]
    public List<string> EvidenceMediaUrls { get; set; } = new();

    [BsonElement("status")]
    public RmaStatus Status { get; set; } = RmaStatus.Submitted;

    [BsonElement("resolutionType")]
    public string ResolutionType { get; set; } = "FreeRepair"; // FreeRepair, PaidRepair, ExchangeNew, Refund, VendorEscalation

    [BsonElement("replacementSerialNumber")]
    public string? ReplacementSerialNumber { get; set; }

    [BsonElement("staffNotes")]
    public string? StaffNotes { get; set; }

    [BsonElement("inspectedByStaffName")]
    public string? InspectedByStaffName { get; set; }

    // 2.2 ticket.update_diagnosis: Diagnosis results
    [BsonElement("diagnosisConclusion")]
    public string? DiagnosisConclusion { get; set; } // "ManufacturerFault" (Lỗi NSX), "UserFault_Drop" (Rơi vỡ), "UserFault_Water" (Ẩm nước quỳ tím đổi màu), "OutOfWarranty" (Hết hạn), "NoFaultFound" (NFF)

    [BsonElement("diagnosisDetails")]
    public string? DiagnosisDetails { get; set; }

    [BsonElement("diagnosisCompletedAt")]
    public DateTime? DiagnosisCompletedAt { get; set; }

    // 1.4 & 2.2 ticket.manage: Dispatch technician & replacement costs
    [BsonElement("assignedTechnicianId")]
    public string? AssignedTechnicianId { get; set; }

    [BsonElement("assignedTechnicianName")]
    public string? AssignedTechnicianName { get; set; }

    [BsonElement("assignedAt")]
    public DateTime? AssignedAt { get; set; }

    [BsonElement("estimatedCompletionDate")]
    public DateTime? EstimatedCompletionDate { get; set; }

    // 2.4 service_cost.estimate & service_cost.submit_for_approval: Quotation
    [BsonElement("partsCost")]
    public decimal PartsCost { get; set; } = 0;

    [BsonElement("laborCost")]
    public decimal LaborCost { get; set; } = 0;

    [BsonElement("discountAmount")]
    public decimal DiscountAmount { get; set; } = 0;

    [BsonElement("totalCost")]
    public decimal TotalCost { get; set; } = 0;

    [BsonElement("isCustomerPaid")]
    public bool IsCustomerPaid { get; set; } = false;

    [BsonElement("paymentStatus")]
    public string PaymentStatus { get; set; } = "FreeUnderWarranty"; // FreeUnderWarranty, Unpaid, Paid

    [BsonElement("quotationStatus")]
    public string QuotationStatus { get; set; } = "None"; // None, Draft, SubmittedToCustomer, ApprovedByCustomer, RejectedByCustomer

    [BsonElement("quotationNotes")]
    public string? QuotationNotes { get; set; }

    [BsonElement("pickupCode")]
    public string? PickupCode { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; } = "Normal"; // Normal, Urgent, VIP

    [BsonElement("replacedPartsList")]
    public List<RmaReplacedPart> ReplacedPartsList { get; set; } = new();

    [BsonElement("inspectionChecklist")]
    public List<string> InspectionChecklist { get; set; } = new();

    // 2.4 customer_note.add: History of customer communications
    [BsonElement("customerNotesHistory")]
    public List<TicketCustomerNote> CustomerNotesHistory { get; set; } = new();

    // 2.4 vendor_rma: Escalation to vendor authorized service center
    [BsonElement("vendorRmaCode")]
    public string? VendorRmaCode { get; set; }

    [BsonElement("vendorName")]
    public string? VendorName { get; set; } // AASP, ROG Asus, Synnex FPT...

    [BsonElement("vendorStatus")]
    public string? VendorStatus { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

[BsonIgnoreExtraElements]
public class RmaReplacedPart
{
    [BsonElement("partCode")]
    public string PartCode { get; set; } = string.Empty;

    [BsonElement("partName")]
    public string PartName { get; set; } = string.Empty;

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("subTotal")]
    public decimal SubTotal => UnitPrice * Quantity;

    [BsonElement("warrantyMonths")]
    public int WarrantyMonths { get; set; } = 6;
}

[BsonIgnoreExtraElements]
public class TicketCustomerNote
{
    [BsonElement("noteId")]
    public string NoteId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [BsonElement("staffName")]
    public string StaffName { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("promisedReturnDate")]
    public DateTime? PromisedReturnDate { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
