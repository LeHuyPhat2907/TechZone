using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.DTOs.Warranty;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,Technician")]
public class TechnicianController : AdminBaseController
{
    private readonly IRepository<RmaRequest> _rmaRepo;
    private readonly IRepository<Warranty> _warrantyRepo;
    private readonly IRepository<WarrantyPolicy> _policyRepo;
    private readonly IRepository<SparePart> _sparePartRepo;
    private readonly IRepository<SparePartRequest> _partRequestRepo;
    private readonly IRepository<VendorRma> _vendorRmaRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly IWarrantyService _warrantyService;

    public TechnicianController(
        IRepository<RmaRequest> rmaRepo,
        IRepository<Warranty> warrantyRepo,
        IRepository<WarrantyPolicy> policyRepo,
        IRepository<SparePart> sparePartRepo,
        IRepository<SparePartRequest> partRequestRepo,
        IRepository<VendorRma> vendorRmaRepo,
        IRepository<Order> orderRepo,
        IRepository<InventoryItem> inventoryRepo,
        IRepository<User> userRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        IWarrantyService warrantyService)
    {
        _rmaRepo = rmaRepo;
        _warrantyRepo = warrantyRepo;
        _policyRepo = policyRepo;
        _sparePartRepo = sparePartRepo;
        _partRequestRepo = partRequestRepo;
        _vendorRmaRepo = vendorRmaRepo;
        _orderRepo = orderRepo;
        _inventoryRepo = inventoryRepo;
        _userRepo = userRepo;
        _auditLogRepo = auditLogRepo;
        _warrantyService = warrantyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "dashboard",
        string? search = null,
        string? lookupSerial = null,
        string? status = null,
        string? priority = null,
        string? category = null,
        bool myTickets = false,
        string? selectedTicketId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var currentUserName = User.Identity?.Name ?? "Kỹ thuật viên";

        var allTickets = await _rmaRepo.GetAllAsync(cancellationToken);
        var allWarranties = await _warrantyRepo.GetAllAsync(cancellationToken);
        var allSpareParts = await _sparePartRepo.GetAllAsync(cancellationToken);
        var allPartRequests = await _partRequestRepo.GetAllAsync(cancellationToken);
        var allVendorRmas = await _vendorRmaRepo.GetAllAsync(cancellationToken);
        var allPolicies = (await _policyRepo.GetAllAsync(cancellationToken)).OrderBy(p => p.DisplayOrder).ToList();
        var allUsers = await _userRepo.GetAllAsync(cancellationToken);
        var technicians = allUsers.Where(u => u.Role == UserRole.Technician && u.IsActive).ToList();

        // 1. KPI Counts
        var myTicketsCount = allTickets.Count(t => 
            (!string.IsNullOrEmpty(t.AssignedTechnicianId) && t.AssignedTechnicianId == currentUserId) ||
            (!string.IsNullOrEmpty(t.AssignedTechnicianName) && t.AssignedTechnicianName.Contains(currentUserName, StringComparison.OrdinalIgnoreCase)));
        var urgentCount = allTickets.Count(t => (t.Priority == "Urgent" || t.Priority == "VIP") && t.Status != RmaStatus.Completed && t.Status != RmaStatus.Rejected);
        var pendingQuotationCount = allTickets.Count(t => t.Status == RmaStatus.QuotationPending || t.QuotationStatus == "SubmittedToCustomer");
        var waitingPartsCount = allTickets.Count(t => t.Status == RmaStatus.WaitingForParts);
        var repairingCount = allTickets.Count(t => t.Status == RmaStatus.Repairing || t.Status == RmaStatus.Approved);
        var readyForPickupCount = allTickets.Count(t => t.Status == RmaStatus.ReadyForPickup || t.Status == RmaStatus.TestedQC);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var completedThisMonth = allTickets.Count(t => t.Status == RmaStatus.Completed && t.CompletedAt >= startOfMonth);
        var lowStockParts = allSpareParts.Count(p => p.StockQuantity <= p.MinStockAlert);

        // Filter tickets
        var ticketsQuery = allTickets.AsEnumerable();
        if (myTickets)
        {
            ticketsQuery = ticketsQuery.Where(t => 
                (!string.IsNullOrEmpty(t.AssignedTechnicianId) && t.AssignedTechnicianId == currentUserId) ||
                (!string.IsNullOrEmpty(t.AssignedTechnicianName) && t.AssignedTechnicianName.Contains(currentUserName, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RmaStatus>(status, true, out var parsedStatus))
        {
            ticketsQuery = ticketsQuery.Where(t => t.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            ticketsQuery = ticketsQuery.Where(t => string.Equals(t.Priority, priority, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            ticketsQuery = ticketsQuery.Where(t =>
                (!string.IsNullOrEmpty(t.RmaCode) && t.RmaCode.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.SerialNumber) && t.SerialNumber.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.ProductName) && t.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.CustomerName) && t.CustomerName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.CustomerPhone) && t.CustomerPhone.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredTickets = ticketsQuery.OrderByDescending(t => t.CreatedAt).ToList();

        // Selected ticket for modal / detail pane
        RmaRequest? selectedTicket = null;
        if (!string.IsNullOrWhiteSpace(selectedTicketId))
        {
            selectedTicket = allTickets.FirstOrDefault(t => t.Id == selectedTicketId);
        }

        // Section 2.1: Device Lookup & Verification
        DeviceLifecycleDto? lifecycleResult = null;
        MaskedOrderDto? maskedOrder = null;

        if (!string.IsNullOrWhiteSpace(lookupSerial))
        {
            var lookupClean = lookupSerial.Trim();
            var lifecycleRes = await _warrantyService.GetDeviceLifecycleAsync(lookupClean, cancellationToken);
            if (lifecycleRes.Succeeded)
            {
                lifecycleResult = lifecycleRes.Data;

                // Lookup original order and mask sensitive financial / personal info
                var allOrders = await _orderRepo.GetAllAsync(cancellationToken);
                var origOrder = allOrders.FirstOrDefault(o => 
                    (!string.IsNullOrEmpty(lifecycleResult?.OrderCode) && o.OrderCode == lifecycleResult.OrderCode) ||
                    o.Items.Any(i => i.AssignedSerialNumbers.Contains(lookupClean)));

                if (origOrder != null)
                {
                    maskedOrder = new MaskedOrderDto
                    {
                        OrderId = origOrder.Id,
                        OrderCode = origOrder.OrderCode,
                        CreatedAt = origOrder.CreatedAt,
                        MaskedCustomerName = origOrder.CustomerName,
                        MaskedPhone = MaskPhone(origOrder.CustomerPhone),
                        MaskedEmail = MaskEmail(origOrder.CustomerEmail),
                        MaskedAddress = $"{origOrder.ShippingAddress?.District}, {origOrder.ShippingAddress?.Province} (Địa chỉ chi tiết đã ẩn theo bảo mật KTV)",
                        TotalAmount = origOrder.TotalAmount,
                        MaskedPaymentMethod = $"{origOrder.PaymentMethod} (Đã ẩn thông tin thẻ/tài khoản khách hàng)",
                        PaymentStatus = origOrder.PaymentStatus.ToString(),
                        OrderStatus = origOrder.OrderStatus.ToString(),
                        Items = origOrder.Items.Select(it => new MaskedOrderItemDto
                        {
                            ProductId = it.ProductId,
                            ProductName = it.ProductName,
                            VariantSku = it.VariantSku,
                            UnitPrice = it.UnitPrice,
                            Quantity = it.Quantity,
                            WarrantyMonths = it.WarrantyMonths
                        }).ToList()
                    };
                }
            }
            else
            {
                TempData["ErrorMessage"] = lifecycleRes.Message;
            }
        }

        // Spare parts filter
        var sparePartsQuery = allSpareParts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            sparePartsQuery = sparePartsQuery.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        var model = new TechnicianPortalViewModel
        {
            ActiveTab = tab.ToLowerInvariant(),
            CurrentTechnicianId = currentUserId,
            CurrentTechnicianName = currentUserName,

            MyAssignedTicketsCount = myTicketsCount,
            UrgentTicketsCount = urgentCount,
            PendingQuotationCount = pendingQuotationCount,
            WaitingForPartsCount = waitingPartsCount,
            RepairingCount = repairingCount,
            ReadyForPickupCount = readyForPickupCount,
            CompletedThisMonthCount = completedThisMonth,
            LowStockPartsCount = lowStockParts,

            LookupSerialQuery = lookupSerial,
            LookupLifecycleResult = lifecycleResult,
            MaskedOriginalOrder = maskedOrder,
            WarrantyPolicies = allPolicies,

            TicketsList = filteredTickets,
            SelectedTicket = selectedTicket,
            TicketFilterStatus = status,
            TicketFilterPriority = priority,
            ShowOnlyMyTickets = myTickets,
            SearchQuery = search,
            AvailableTechnicians = technicians,

            SparePartsList = sparePartsQuery.OrderBy(p => p.Category).ThenBy(p => p.PartName).ToList(),
            PartRequestsList = allPartRequests.OrderByDescending(r => r.CreatedAt).Take(20).ToList(),
            PartFilterCategory = category,

            QuotationTicketsList = allTickets.Where(t => t.ResolutionType == "PaidRepair" || t.QuotationStatus != "None" || t.TotalCost > 0).OrderByDescending(t => t.CreatedAt).ToList(),
            VendorRmaList = allVendorRmas.OrderByDescending(v => v.SentDate).ToList()
        };

        return View(model);
    }

    // ==========================================
    // 2.2 TICKET.CREATE: Lập phiếu tiếp nhận thiết bị
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(
        string customerName,
        string customerPhone,
        string productName,
        string serialNumber,
        string orderCode,
        string reasonType,
        string customerDescription,
        string physicalConditionNotes,
        string accessoriesIncluded,
        string priority,
        string? evidenceImageUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(productName))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên khách hàng, số điện thoại và tên thiết bị.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        var rma = new RmaRequest
        {
            RmaCode = $"RMA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            CustomerName = customerName.Trim(),
            CustomerPhone = customerPhone.Trim(),
            ProductName = productName.Trim(),
            SerialNumber = serialNumber?.Trim() ?? string.Empty,
            OrderCode = orderCode?.Trim() ?? string.Empty,
            ReasonType = string.IsNullOrWhiteSpace(reasonType) ? "HardwareDefect" : reasonType.Trim(),
            CustomerDescription = customerDescription?.Trim() ?? "Yêu cầu kiểm tra phần cứng",
            PhysicalConditionNotes = string.IsNullOrWhiteSpace(physicalConditionNotes) ? "Ngoại quan bình thường" : physicalConditionNotes.Trim(),
            AccessoriesIncluded = string.IsNullOrWhiteSpace(accessoriesIncluded) ? "Thân máy" : accessoriesIncluded.Trim(),
            Priority = string.IsNullOrWhiteSpace(priority) ? "Normal" : priority.Trim(),
            Status = RmaStatus.DeviceReceived,
            PickupCode = $"PK-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            AssignedTechnicianName = User.Identity?.Name ?? "Kỹ thuật viên tiếp nhận",
            InspectedByStaffName = User.Identity?.Name ?? "Kỹ thuật viên tiếp nhận",
            EvidenceMediaUrls = !string.IsNullOrWhiteSpace(evidenceImageUrl) ? new List<string> { evidenceImageUrl.Trim() } : new List<string>()
        };

        await _rmaRepo.AddAsync(rma, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateServiceTicket",
            Module = "Technician",
            UserEmail = User.Identity?.Name ?? "Technician",
            Details = $"Lập phiếu tiếp nhận máy mới mã {rma.RmaCode} - Thiết bị: {rma.ProductName} (Serial: {rma.SerialNumber})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu tiếp nhận {rma.RmaCode} thành công! Mã nhận máy của khách: {rma.PickupCode}";
        return RedirectToAction(nameof(Index), new { tab = "tickets", selectedTicketId = rma.Id });
    }

    // ==========================================
    // 2.2 TICKET.UPDATE_DIAGNOSIS: Chẩn đoán kỹ thuật
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDiagnosis(
        string ticketId,
        string diagnosisConclusion,
        string diagnosisDetails,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu sửa chữa tương ứng.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        ticket.DiagnosisConclusion = diagnosisConclusion;
        ticket.DiagnosisDetails = diagnosisDetails?.Trim();
        ticket.DiagnosisCompletedAt = DateTime.UtcNow;

        // Auto adjust resolution type based on conclusion
        if (diagnosisConclusion == "ManufacturerFault")
        {
            ticket.ResolutionType = "FreeRepair";
            ticket.PaymentStatus = "FreeUnderWarranty";
        }
        else if (diagnosisConclusion == "UserFault_Drop" || diagnosisConclusion == "UserFault_Water" || diagnosisConclusion == "OutOfWarranty")
        {
            ticket.ResolutionType = "PaidRepair";
            ticket.PaymentStatus = "Unpaid";
        }

        if (ticket.Status == RmaStatus.DeviceReceived || ticket.Status == RmaStatus.Submitted)
        {
            ticket.Status = RmaStatus.Inspecting;
        }

        await _rmaRepo.UpdateAsync(ticket, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật kết luận chẩn đoán kỹ thuật cho phiếu {ticket.RmaCode}.";
        return RedirectToAction(nameof(Index), new { tab = "tickets", selectedTicketId = ticket.Id });
    }

    // ==========================================
    // 2.2 TICKET.UPDATE_STATUS: Quy trình 6 bước
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTicketStatus(
        string ticketId,
        RmaStatus newStatus,
        string? staffNotes,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu sửa chữa.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        ticket.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(staffNotes))
        {
            ticket.StaffNotes = staffNotes.Trim();
        }

        if (newStatus == RmaStatus.Completed)
        {
            ticket.CompletedAt = DateTime.UtcNow;
        }

        await _rmaRepo.UpdateAsync(ticket, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái phiếu {ticket.RmaCode} thành: {newStatus}.";
        return RedirectToAction(nameof(Index), new { tab = "tickets", selectedTicketId = ticket.Id });
    }

    // ==========================================
    // 2.2 TICKET.UPLOAD_EVIDENCE: Tải ảnh bằng chứng
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEvidence(
        string ticketId,
        string mediaUrl,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        if (!string.IsNullOrWhiteSpace(mediaUrl))
        {
            ticket.EvidenceMediaUrls.Add(mediaUrl.Trim());
            await _rmaRepo.UpdateAsync(ticket, cancellationToken);
            TempData["SuccessMessage"] = "Đã lưu bằng chứng thẩm định thiết bị thành công.";
        }

        return RedirectToAction(nameof(Index), new { tab = "tickets", selectedTicketId = ticket.Id });
    }

    // ==========================================
    // 2.2 TICKET.PRINT_RECEIPT: In biên nhận
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> PrintReceipt(string ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        return View(ticket);
    }

    // ==========================================
    // 2.3 SPARE_PARTS.REQUEST: Yêu cầu xuất linh kiện
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestSparePart(
        string ticketId,
        string partId,
        int quantity = 1,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        var part = await _sparePartRepo.GetByIdAsync(partId, cancellationToken);

        if (ticket == null || part == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu hoặc linh kiện yêu cầu.";
            return RedirectToAction(nameof(Index), new { tab = "spareparts" });
        }

        if (part.StockQuantity < quantity)
        {
            TempData["ErrorMessage"] = $"Linh kiện '{part.PartName}' trong kho chỉ còn {part.StockQuantity} đơn vị, không đủ xuất!";
            return RedirectToAction(nameof(Index), new { tab = "spareparts" });
        }

        // Deduct spare part stock
        part.StockQuantity -= quantity;
        await _sparePartRepo.UpdateAsync(part, cancellationToken);

        // Add to ticket's replaced parts list
        ticket.ReplacedPartsList.Add(new RmaReplacedPart
        {
            PartCode = part.PartCode,
            PartName = part.PartName,
            UnitPrice = part.ServicePrice,
            Quantity = quantity,
            WarrantyMonths = part.WarrantyMonths
        });

        ticket.PartsCost += part.ServicePrice * quantity;
        ticket.TotalCost = Math.Max(0, ticket.PartsCost + ticket.LaborCost - ticket.DiscountAmount);

        if (ticket.Status == RmaStatus.WaitingForParts)
        {
            ticket.Status = RmaStatus.Repairing;
        }

        await _rmaRepo.UpdateAsync(ticket, cancellationToken);

        // Log request record
        var requestRecord = new SparePartRequest
        {
            TicketId = ticket.Id,
            TicketCode = ticket.RmaCode,
            PartId = part.Id,
            PartCode = part.PartCode,
            PartName = part.PartName,
            Quantity = quantity,
            Type = "ExportForRepair",
            TechnicianId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            TechnicianName = User.Identity?.Name ?? "Technician",
            Status = "Approved",
            Notes = notes?.Trim() ?? $"Xuất thay thế cho ca RMA {ticket.RmaCode}"
        };
        await _partRequestRepo.AddAsync(requestRecord, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xuất {quantity}x '{part.PartName}' và gán vào phiếu {ticket.RmaCode} thành công.";
        return RedirectToAction(nameof(Index), new { tab = "tickets", selectedTicketId = ticket.Id });
    }

    // ==========================================
    // 2.3 SPARE_PARTS.RETURN_DEFECTIVE: Trả linh kiện hỏng
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnDefectivePart(
        string partId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var part = await _sparePartRepo.GetByIdAsync(partId, cancellationToken);
        if (part == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy linh kiện.";
            return RedirectToAction(nameof(Index), new { tab = "spareparts" });
        }

        part.DefectiveQuantity += quantity;
        await _sparePartRepo.UpdateAsync(part, cancellationToken);

        var requestRecord = new SparePartRequest
        {
            PartId = part.Id,
            PartCode = part.PartCode,
            PartName = part.PartName,
            Quantity = quantity,
            Type = "ReturnDefective",
            TechnicianId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            TechnicianName = User.Identity?.Name ?? "Technician",
            Status = "Returned",
            Notes = reason?.Trim() ?? "Linh kiện lỗi cũ thu hồi chờ đổi trả hãng/tiêu hủy"
        };
        await _partRequestRepo.AddAsync(requestRecord, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu trả {quantity} linh kiện hỏng '{part.PartName}' về kho trung tâm thành công.";
        return RedirectToAction(nameof(Index), new { tab = "spareparts" });
    }

    // ==========================================
    // 2.4 SERVICE_COST.ESTIMATE & SUBMIT_FOR_APPROVAL
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EstimateServiceCost(
        string ticketId,
        decimal laborCost,
        decimal discountAmount,
        string quotationNotes,
        bool submitForApproval = false,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu.";
            return RedirectToAction(nameof(Index), new { tab = "quotations" });
        }

        ticket.LaborCost = Math.Max(0, laborCost);
        ticket.DiscountAmount = Math.Max(0, discountAmount);
        ticket.TotalCost = Math.Max(0, ticket.PartsCost + ticket.LaborCost - ticket.DiscountAmount);
        ticket.QuotationNotes = quotationNotes?.Trim();
        ticket.ResolutionType = "PaidRepair";

        if (submitForApproval)
        {
            ticket.QuotationStatus = "SubmittedToCustomer";
            ticket.Status = RmaStatus.QuotationPending;
            ticket.StaffNotes = $"Đã gửi báo giá {ticket.TotalCost:N0}₫ cho khách chờ phê duyệt.";
            TempData["SuccessMessage"] = $"Đã gửi bảng dự toán chi phí {ticket.TotalCost:N0}₫ cho khách hàng/quản lý phê duyệt.";
        }
        else
        {
            ticket.QuotationStatus = "Draft";
            TempData["SuccessMessage"] = $"Đã lưu bảng dự toán chi phí nháp: {ticket.TotalCost:N0}₫.";
        }

        await _rmaRepo.UpdateAsync(ticket, cancellationToken);
        return RedirectToAction(nameof(Index), new { tab = "quotations" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuotationApproval(
        string ticketId,
        bool isApproved,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu.";
            return RedirectToAction(nameof(Index), new { tab = "quotations" });
        }

        if (isApproved)
        {
            ticket.QuotationStatus = "ApprovedByCustomer";
            ticket.Status = RmaStatus.Repairing;
            ticket.IsCustomerPaid = false;
            ticket.StaffNotes = "Khách hàng đã đồng ý báo giá. Bắt đầu tiến hành sửa chữa thay thế linh kiện.";
            TempData["SuccessMessage"] = $"Khách hàng đã duyệt báo giá {ticket.TotalCost:N0}₫! Phiếu chuyển sang trạng thái Đang sửa.";
        }
        else
        {
            ticket.QuotationStatus = "RejectedByCustomer";
            ticket.Status = RmaStatus.ReadyForPickup;
            ticket.StaffNotes = $"Khách từ chối báo giá ({note ?? "Chi phí cao"}). Lắp lại nguyên trạng hoàn trả khách.";
            TempData["SuccessMessage"] = "Khách từ chối sửa dịch vụ. Phiếu chuyển sang trạng thái Chờ trả máy nguyên trạng.";
        }

        await _rmaRepo.UpdateAsync(ticket, cancellationToken);
        return RedirectToAction(nameof(Index), new { tab = "quotations" });
    }

    // ==========================================
    // 2.4 CUSTOMER_NOTE.ADD: Ghi chú trao đổi & hẹn ngày
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCustomerNote(
        string ticketId,
        string content,
        DateTime? promisedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null || string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập nội dung ghi chú trao đổi với khách.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        ticket.CustomerNotesHistory.Add(new TicketCustomerNote
        {
            StaffName = User.Identity?.Name ?? "Technician",
            Content = content.Trim(),
            PromisedReturnDate = promisedReturnDate
        });

        if (promisedReturnDate.HasValue)
        {
            ticket.EstimatedCompletionDate = promisedReturnDate.Value;
        }

        await _rmaRepo.UpdateAsync(ticket, cancellationToken);
        TempData["SuccessMessage"] = "Đã lưu ghi chú trao đổi và lịch hẹn trả máy với khách hàng.";
        return RedirectToAction(nameof(Index), new { tab = "tickets", selectedTicketId = ticket.Id });
    }

    // ==========================================
    // 2.4 VENDOR_RMA.CREATE | TRACK: Điều phối hãng
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVendorRma(
        string ticketId,
        string vendorName,
        string? vendorTrackingCode,
        string? carrierName,
        string? carrierTrackingNumber,
        DateTime? expectedReturnDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _rmaRepo.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu sửa chữa tương ứng.";
            return RedirectToAction(nameof(Index), new { tab = "vendor_rma" });
        }

        var vendorRma = new VendorRma
        {
            VendorRmaCode = $"VRMA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
            TicketId = ticket.Id,
            TicketCode = ticket.RmaCode,
            SerialNumber = ticket.SerialNumber,
            ProductName = ticket.ProductName,
            VendorName = string.IsNullOrWhiteSpace(vendorName) ? "Apple Authorized Service Provider (Thakral One)" : vendorName.Trim(),
            VendorTrackingCode = vendorTrackingCode?.Trim(),
            CarrierName = string.IsNullOrWhiteSpace(carrierName) ? "Giao Hàng Nhanh (GHN)" : carrierName.Trim(),
            CarrierTrackingNumber = carrierTrackingNumber?.Trim(),
            SentDate = DateTime.UtcNow,
            ExpectedReturnDate = expectedReturnDate ?? DateTime.UtcNow.AddDays(7),
            Status = "SentToVendor",
            TechnicianNotes = notes?.Trim()
        };

        await _vendorRmaRepo.AddAsync(vendorRma, cancellationToken);

        ticket.Status = RmaStatus.SentToVendor;
        ticket.VendorRmaCode = vendorRma.VendorRmaCode;
        ticket.VendorName = vendorRma.VendorName;
        ticket.ResolutionType = "VendorEscalation";
        ticket.StaffNotes = $"Đã gửi máy về TTBH hãng {vendorRma.VendorName} (Mã gửi hãng: {vendorRma.VendorRmaCode})";
        await _rmaRepo.UpdateAsync(ticket, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lập phiếu gửi Hãng {vendorRma.VendorRmaCode} thành công!";
        return RedirectToAction(nameof(Index), new { tab = "vendor_rma" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVendorRmaStatus(
        string vendorRmaId,
        string newStatus,
        string? vendorDiagnosis,
        string? vendorResolution,
        CancellationToken cancellationToken = default)
    {
        var vRma = await _vendorRmaRepo.GetByIdAsync(vendorRmaId, cancellationToken);
        if (vRma == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu gửi hãng.";
            return RedirectToAction(nameof(Index), new { tab = "vendor_rma" });
        }

        vRma.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(vendorDiagnosis)) vRma.VendorDiagnosis = vendorDiagnosis.Trim();
        if (!string.IsNullOrWhiteSpace(vendorResolution)) vRma.VendorResolution = vendorResolution.Trim();

        if (newStatus == "ReceivedBack" || newStatus == "Completed")
        {
            vRma.ReceivedBackDate = DateTime.UtcNow;
            var ticket = await _rmaRepo.GetByIdAsync(vRma.TicketId, cancellationToken);
            if (ticket != null)
            {
                ticket.Status = RmaStatus.TestedQC;
                ticket.StaffNotes = $"Đã nhận máy từ Hãng về. Kết quả: {vRma.VendorResolution ?? "Đã xử lý theo chuẩn hãng"}. Đang test lại QC trước khi trả khách.";
                await _rmaRepo.UpdateAsync(ticket, cancellationToken);
            }
        }

        await _vendorRmaRepo.UpdateAsync(vRma, cancellationToken);
        TempData["SuccessMessage"] = $"Đã cập nhật tiến độ từ Hãng cho phiếu {vRma.VendorRmaCode}.";
        return RedirectToAction(nameof(Index), new { tab = "vendor_rma" });
    }

    // ==========================================
    // HELPERS & MASKING (Security Restrictions)
    // ==========================================
    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 6) return "***";
        return string.Concat(phone.AsSpan(0, 4), "***", phone.AsSpan(phone.Length - 3));
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "***";
        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];
        var maskedName = name.Length <= 2 ? name[0] + "***" : string.Concat(name.AsSpan(0, 2), "***", name.AsSpan(name.Length - 1));
        return $"{maskedName}@{domain}";
    }
}
