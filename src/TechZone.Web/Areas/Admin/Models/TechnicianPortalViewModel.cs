using TechZone.Application.DTOs.Warranty;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class TechnicianPortalViewModel
{
    public string ActiveTab { get; set; } = "dashboard"; // "dashboard", "lookup", "tickets", "spareparts", "quotations", "vendor_rma"

    // Technician Profile Info
    public string CurrentTechnicianId { get; set; } = string.Empty;
    public string CurrentTechnicianName { get; set; } = "Kỹ thuật viên";

    // 1. KPI Dashboard Metrics
    public int MyAssignedTicketsCount { get; set; }
    public int UrgentTicketsCount { get; set; }
    public int PendingQuotationCount { get; set; }
    public int WaitingForPartsCount { get; set; }
    public int RepairingCount { get; set; }
    public int ReadyForPickupCount { get; set; }
    public int CompletedThisMonthCount { get; set; }
    public int LowStockPartsCount { get; set; }

    // 2. Section 2.1: Device Lookup & Verification (imei_serial.lookup, warranty_policy.view, order.lookup_history)
    public string? LookupSerialQuery { get; set; }
    public DeviceLifecycleDto? LookupLifecycleResult { get; set; }
    public MaskedOrderDto? MaskedOriginalOrder { get; set; }
    public List<WarrantyPolicy> WarrantyPolicies { get; set; } = new();

    // 3. Section 2.2: RMA & Service Tickets (ticket.create, view_assigned, update_diagnosis, update_status, upload_evidence, print_receipt)
    public List<RmaRequest> TicketsList { get; set; } = new();
    public RmaRequest? SelectedTicket { get; set; }
    public string? TicketFilterStatus { get; set; }
    public string? TicketFilterPriority { get; set; }
    public bool ShowOnlyMyTickets { get; set; } = false;
    public string? SearchQuery { get; set; }
    public List<User> AvailableTechnicians { get; set; } = new();

    // 4. Section 2.3: Spare Parts & Internal Inventory (spare_parts.view_stock, request, return_defective)
    public List<SparePart> SparePartsList { get; set; } = new();
    public List<SparePartRequest> PartRequestsList { get; set; } = new();
    public string? PartFilterCategory { get; set; }

    // 5. Section 2.4: Quotations & Vendor RMA (service_cost.estimate, submit_for_approval, customer_note.add, vendor_rma.create/track)
    public List<RmaRequest> QuotationTicketsList { get; set; } = new();
    public List<VendorRma> VendorRmaList { get; set; } = new();
}

public class MaskedOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string MaskedCustomerName { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public string MaskedAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string MaskedPaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public List<MaskedOrderItemDto> Items { get; set; } = new();
}

public class MaskedOrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int WarrantyMonths { get; set; }
}
