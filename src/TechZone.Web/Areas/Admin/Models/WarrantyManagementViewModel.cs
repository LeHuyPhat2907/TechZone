using TechZone.Application.DTOs.Warranty;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class WarrantyManagementViewModel
{
    public List<Warranty> Warranties { get; set; } = new();
    public List<RmaRequest> RmaRequests { get; set; } = new();
    public List<WarrantyPolicy> WarrantyPolicies { get; set; } = new();
    public List<User> AvailableTechnicians { get; set; } = new();
    public DeviceLifecycleDto? LookupLifecycleResult { get; set; }

    // KPI Metrics
    public int TotalWarranties { get; set; }
    public int ActiveWarranties { get; set; }
    public int ExpiringSoonWarranties { get; set; }
    public int OpenRmaTickets { get; set; }
    public int CompletedRmaTickets { get; set; }
    public int TotalPolicies { get; set; }

    // Filter & Pagination
    public string? SearchQuery { get; set; }
    public string? LookupSerialQuery { get; set; }
    public WarrantyStatus? WarrantyStatusFilter { get; set; }
    public RmaStatus? RmaStatusFilter { get; set; }
    public string ActiveTab { get; set; } = "tickets"; // "tickets", "lookup", "policies", "warranties"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalFilteredCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalFilteredCount / PageSize);
}

public class CreateRmaTicketInputModel
{
    public string OrderCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ReasonType { get; set; } = "HardwareDefect"; // HardwareDefect, DOA, DamagedInTransit, ScreenFault, BatteryIssue
    public string CustomerDescription { get; set; } = string.Empty;
    public string ResolutionType { get; set; } = "FreeRepair"; // FreeRepair, ExchangeNew, Refund
    public string? AssignedTechnicianName { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public string Priority { get; set; } = "Normal"; // Normal, Urgent, VIP
    public string? StaffNotes { get; set; }
}

public class DispatchTechnicianInputModel
{
    public string RmaId { get; set; } = string.Empty;
    public string TechnicianId { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime? EstimatedCompletionDate { get; set; }
    public string Priority { get; set; } = "Normal";
    public string? Instructions { get; set; }
}

public class UpdateTicketProgressInputModel
{
    public string RmaId { get; set; } = string.Empty;
    public RmaStatus NewStatus { get; set; }
    public string? ResolutionType { get; set; }
    public string? ReplacementSerialNumber { get; set; }
    public string? TechnicianNotes { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsCustomerPaid { get; set; }
    public string PaymentStatus { get; set; } = "FreeUnderWarranty";
    public string? ReplacedPartsJson { get; set; } // JSON list of replaced parts
}

public class AddWarrantyClaimInputModel
{
    public string WarrantyId { get; set; } = string.Empty;
    public string FaultDescription { get; set; } = string.Empty;
    public string? TechnicianNotes { get; set; }
    public string? ReplacedParts { get; set; } // Comma-separated parts
    public string Status { get; set; } = "Processing"; // Processing, Repaired, ReplacedDevice, Rejected
}
