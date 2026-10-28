using TechZone.Domain.Enums;

namespace TechZone.Application.DTOs.Warranty;

public class WarrantyLookupResultDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WarrantyMonths { get; set; }
    public WarrantyStatus Status { get; set; }
    public bool IsActive => Status == WarrantyStatus.Active && DateTime.UtcNow <= EndDate;
    public int DaysRemaining => Math.Max(0, (EndDate - DateTime.UtcNow).Days);
    public List<WarrantyClaimItemDto> Claims { get; set; } = new();
}

public class WarrantyClaimItemDto
{
    public string ClaimCode { get; set; } = string.Empty;
    public string FaultDescription { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TechnicianNotes { get; set; }
}

public class DeviceLifecycleDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }

    // 1. Warehouse & Inventory Origin
    public bool HasInventoryRecord { get; set; }
    public string? ImportBatchCode { get; set; }
    public string? SupplierName { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? WarehouseLocation { get; set; }
    public string? ExportSlipCode { get; set; }
    public string? InventoryStatus { get; set; }

    // 2. Commercial Order History
    public bool HasOrderRecord { get; set; }
    public string? OrderId { get; set; }
    public string? OrderCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal SalePrice { get; set; }
    public string? SourceChannel { get; set; }

    // 3. Electronic Warranty (E-Warranty)
    public bool HasWarrantyRecord { get; set; }
    public string? WarrantyId { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public int WarrantyMonths { get; set; }
    public WarrantyStatus? WarrantyStatus { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsWarrantyActive { get; set; }

    // 4. RMA & Technical Service History
    public List<RmaTicketSummaryDto> RmaTickets { get; set; } = new();

    // 5. Current Machine Status
    public string CurrentStage { get; set; } = "Unknown";
    public string CurrentStageText { get; set; } = string.Empty;
    public string BadgeCssClass { get; set; } = "bg-secondary";
}

public class RmaTicketSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string RmaCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ReasonType { get; set; } = string.Empty;
    public string CustomerDescription { get; set; } = string.Empty;
    public RmaStatus Status { get; set; }
    public string ResolutionType { get; set; } = string.Empty;
    public string? AssignedTechnicianName { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsCustomerPaid { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int ReplacedPartsCount { get; set; }
    public string? ReplacementSerialNumber { get; set; }
    public DateTime? CompletedAt { get; set; }
}
