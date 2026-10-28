using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,Technician,SalesStaff")]
public class WarrantyController : AdminBaseController
{
    private readonly IRepository<Warranty> _warrantyRepo;
    private readonly IRepository<RmaRequest> _rmaRepo;
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<WarrantyPolicy> _policyRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly IWarrantyService _warrantyService;

    public WarrantyController(
        IRepository<Warranty> warrantyRepo,
        IRepository<RmaRequest> rmaRepo,
        IRepository<InventoryItem> inventoryRepo,
        IRepository<User> userRepo,
        IRepository<WarrantyPolicy> policyRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        IWarrantyService warrantyService)
    {
        _warrantyRepo = warrantyRepo;
        _rmaRepo = rmaRepo;
        _inventoryRepo = inventoryRepo;
        _userRepo = userRepo;
        _policyRepo = policyRepo;
        _auditLogRepo = auditLogRepo;
        _warrantyService = warrantyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? lookupSerial = null,
        WarrantyStatus? warrantyStatus = null,
        RmaStatus? rmaStatus = null,
        string tab = "tickets",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 12;
        page = Math.Max(1, page);

        var allWarranties = await _warrantyRepo.GetAllAsync(cancellationToken);
        var allRmas = await _rmaRepo.GetAllAsync(cancellationToken);
        var allPolicies = await _policyRepo.GetAllAsync(cancellationToken);

        // Fetch technicians for ticket dispatching
        var technicians = (await _userRepo.FindAsync(u => u.Role == UserRole.Technician && u.IsActive, cancellationToken)).ToList();
        if (!technicians.Any())
        {
            technicians.Add(new User
            {
                FullName = "Lê Kỹ Thuật (Chuyên Apple/Laptop)",
                Email = "technician1@techzone.vn",
                PhoneNumber = "0909123456",
                Role = UserRole.Technician
            });
            technicians.Add(new User
            {
                FullName = "Trần Phần Cứng (Chuyên PC/VGA)",
                Email = "technician2@techzone.vn",
                PhoneNumber = "0909654321",
                Role = UserRole.Technician
            });
        }

        // Seed initial data if database is empty
        if (!allRmas.Any())
        {
            await SeedInitialRmaAndWarrantyDataAsync(cancellationToken);
            allWarranties = await _warrantyRepo.GetAllAsync(cancellationToken);
            allRmas = await _rmaRepo.GetAllAsync(cancellationToken);
        }

        if (!allPolicies.Any())
        {
            await ApplyDefaultPoliciesInternalAsync(cancellationToken);
            allPolicies = await _policyRepo.GetAllAsync(cancellationToken);
        }

        // Calculate KPIs
        var totalWarranties = allWarranties.Count;
        var activeWarranties = allWarranties.Count(w => w.IsActive);
        var expiringSoon = allWarranties.Count(w => w.Status == WarrantyStatus.ExpiringSoon || (w.IsActive && w.DaysRemaining <= 30));
        var openRmas = allRmas.Count(r => r.Status != RmaStatus.Completed && r.Status != RmaStatus.Rejected);
        var completedRmas = allRmas.Count(r => r.Status == RmaStatus.Completed);
        var totalPolicies = allPolicies.Count;

        // Normalize tab
        var normalizedTab = tab.ToLowerInvariant() switch
        {
            "rma" => "tickets",
            "ticket" => "tickets",
            "lookup" => "lookup",
            "policies" => "policies",
            "policy" => "policies",
            "warranties" => "warranties",
            _ => "tickets"
        };

        var model = new WarrantyManagementViewModel
        {
            TotalWarranties = totalWarranties,
            ActiveWarranties = activeWarranties,
            ExpiringSoonWarranties = expiringSoon,
            OpenRmaTickets = openRmas,
            CompletedRmaTickets = completedRmas,
            TotalPolicies = totalPolicies,
            SearchQuery = search,
            LookupSerialQuery = lookupSerial,
            WarrantyStatusFilter = warrantyStatus,
            RmaStatusFilter = rmaStatus,
            ActiveTab = normalizedTab,
            Page = page,
            PageSize = pageSize,
            WarrantyPolicies = allPolicies.OrderBy(p => p.DisplayOrder).ToList(),
            AvailableTechnicians = technicians
        };

        // Handle Tab 1: Tickets (RMA)
        if (model.ActiveTab == "tickets")
        {
            var rmaQuery = allRmas.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                rmaQuery = rmaQuery.Where(r =>
                    (!string.IsNullOrEmpty(r.RmaCode) && r.RmaCode.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.OrderCode) && r.OrderCode.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.SerialNumber) && r.SerialNumber.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.CustomerName) && r.CustomerName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.CustomerPhone) && r.CustomerPhone.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.ProductName) && r.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.AssignedTechnicianName) && r.AssignedTechnicianName.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }

            if (rmaStatus.HasValue)
            {
                rmaQuery = rmaQuery.Where(r => r.Status == rmaStatus.Value);
            }

            var filteredRmas = rmaQuery.OrderByDescending(r => r.CreatedAt).ToList();
            model.TotalFilteredCount = filteredRmas.Count;
            model.RmaRequests = filteredRmas.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        // Handle Tab 2: Device Lifecycle & Warranty Lookup
        else if (model.ActiveTab == "lookup")
        {
            if (!string.IsNullOrWhiteSpace(lookupSerial))
            {
                var lookupRes = await _warrantyService.GetDeviceLifecycleAsync(lookupSerial.Trim(), cancellationToken);
                if (lookupRes.Succeeded)
                {
                    model.LookupLifecycleResult = lookupRes.Data;
                }
                else
                {
                    TempData["ErrorMessage"] = lookupRes.Message;
                }
            }
        }
        // Handle Tab 3: Policies
        else if (model.ActiveTab == "policies")
        {
            model.WarrantyPolicies = (await _warrantyService.GetWarrantyPoliciesAsync(cancellationToken)).ToList();
        }
        // Handle Tab 4: Warranties Registry
        else
        {
            var warrantyQuery = allWarranties.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                warrantyQuery = warrantyQuery.Where(w =>
                    (!string.IsNullOrEmpty(w.SerialNumber) && w.SerialNumber.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(w.Imei) && w.Imei.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(w.CustomerPhone) && w.CustomerPhone.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(w.CustomerName) && w.CustomerName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(w.ProductName) && w.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(w.OrderCode) && w.OrderCode.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }

            if (warrantyStatus.HasValue)
            {
                warrantyQuery = warrantyQuery.Where(w => w.Status == warrantyStatus.Value);
            }

            var filteredWarranties = warrantyQuery.OrderByDescending(w => w.CreatedAt).ToList();
            model.TotalFilteredCount = filteredWarranties.Count;
            model.Warranties = filteredWarranties.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        return View(model);
    }

    /// <summary>
    /// ticket.manage: Tạo phiếu tiếp nhận bảo hành & sửa chữa
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Technician,SalesStaff")]
    public async Task<IActionResult> CreateRmaTicket(
        [FromForm] CreateRmaTicketInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerName) || string.IsNullOrWhiteSpace(input.CustomerPhone) || string.IsNullOrWhiteSpace(input.ProductName))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ Tên khách hàng, Số điện thoại và Tên thiết bị.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        var rma = new RmaRequest
        {
            RmaCode = "RMA-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
            OrderCode = string.IsNullOrWhiteSpace(input.OrderCode) ? "WALK-IN" : input.OrderCode.Trim().ToUpper(),
            ProductName = input.ProductName.Trim(),
            SerialNumber = input.SerialNumber?.Trim().ToUpper() ?? "N/A",
            CustomerName = input.CustomerName.Trim(),
            CustomerPhone = input.CustomerPhone.Trim(),
            ReasonType = input.ReasonType,
            CustomerDescription = string.IsNullOrWhiteSpace(input.CustomerDescription) ? "Khách báo lỗi phần cứng ngoại quan" : input.CustomerDescription.Trim(),
            ResolutionType = input.ResolutionType,
            Status = !string.IsNullOrWhiteSpace(input.AssignedTechnicianName) ? RmaStatus.AssignedToTech : RmaStatus.DeviceReceived,
            AssignedTechnicianName = input.AssignedTechnicianName,
            AssignedAt = !string.IsNullOrWhiteSpace(input.AssignedTechnicianName) ? DateTime.UtcNow : null,
            EstimatedCompletionDate = input.EstimatedCompletionDate ?? DateTime.UtcNow.AddDays(3),
            Priority = input.Priority ?? "Normal",
            PickupCode = "TZ" + Random.Shared.Next(1000, 9999),
            StaffNotes = input.StaffNotes?.Trim(),
            InspectedByStaffName = User.Identity?.Name ?? "Kỹ thuật viên tiếp nhận",
            CreatedAt = DateTime.UtcNow
        };

        await _rmaRepo.AddAsync(rma, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateRmaTicket",
            Module = "Warranty",
            UserEmail = User.Identity?.Name ?? "Technician",
            Details = $"Tiếp nhận phiếu bảo hành RMA [{rma.RmaCode}] cho S/N [{rma.SerialNumber}] - Khách: {rma.CustomerName} - Gán: {rma.AssignedTechnicianName ?? "Chưa gán"}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo thành công phiếu tiếp nhận bảo hành [{rma.RmaCode}]! Mã nhận máy của khách: [{rma.PickupCode}].";
        return RedirectToAction(nameof(Index), new { tab = "tickets" });
    }

    /// <summary>
    /// ticket.manage: Điều phối kỹ thuật viên sửa chữa
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Technician")]
    public async Task<IActionResult> DispatchTechnician(
        [FromForm] DispatchTechnicianInputModel input,
        CancellationToken cancellationToken = default)
    {
        var rma = await _rmaRepo.GetByIdAsync(input.RmaId, cancellationToken);
        if (rma == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu RMA cần điều phối.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        rma.AssignedTechnicianId = input.TechnicianId;
        rma.AssignedTechnicianName = input.TechnicianName;
        rma.AssignedAt = DateTime.UtcNow;
        rma.Priority = input.Priority;

        if (input.EstimatedCompletionDate.HasValue)
        {
            rma.EstimatedCompletionDate = input.EstimatedCompletionDate.Value;
        }

        if (rma.Status == RmaStatus.DeviceReceived || rma.Status == RmaStatus.Submitted || rma.Status == RmaStatus.UnderReview)
        {
            rma.Status = RmaStatus.AssignedToTech;
        }

        var noteEntry = $"[{DateTime.UtcNow:dd/MM HH:mm}] Điều phối cho KTV {input.TechnicianName} (Ưu tiên: {input.Priority}). Ghi chú: {input.Instructions ?? "Xử lý theo quy trình"}";
        rma.StaffNotes = string.IsNullOrWhiteSpace(rma.StaffNotes) ? noteEntry : $"{rma.StaffNotes}\n{noteEntry}";

        await _rmaRepo.UpdateAsync(rma, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DispatchTechnician",
            Module = "Warranty",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Điều phối KTV [{input.TechnicianName}] xử lý phiếu RMA [{rma.RmaCode}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã điều phối thành công phiếu RMA [{rma.RmaCode}] cho KTV {input.TechnicianName}!";
        return RedirectToAction(nameof(Index), new { tab = "tickets" });
    }

    /// <summary>
    /// ticket.manage: Cập nhật tiến độ sửa chữa, linh kiện thay thế & chi phí dịch vụ
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Technician")]
    public async Task<IActionResult> UpdateTicketProgress(
        [FromForm] UpdateTicketProgressInputModel input,
        CancellationToken cancellationToken = default)
    {
        var rma = await _rmaRepo.GetByIdAsync(input.RmaId, cancellationToken);
        if (rma == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu RMA cần cập nhật.";
            return RedirectToAction(nameof(Index), new { tab = "tickets" });
        }

        var oldStatus = rma.Status;
        rma.Status = input.NewStatus;
        rma.InspectedByStaffName = User.Identity?.Name ?? "Kỹ thuật viên";

        if (!string.IsNullOrWhiteSpace(input.ResolutionType))
        {
            rma.ResolutionType = input.ResolutionType;
        }

        if (!string.IsNullOrWhiteSpace(input.ReplacementSerialNumber))
        {
            rma.ReplacementSerialNumber = input.ReplacementSerialNumber.Trim().ToUpper();
        }

        // Cost calculations
        rma.PartsCost = Math.Max(0, input.PartsCost);
        rma.LaborCost = Math.Max(0, input.LaborCost);
        rma.DiscountAmount = Math.Max(0, input.DiscountAmount);
        rma.TotalCost = Math.Max(0, rma.PartsCost + rma.LaborCost - rma.DiscountAmount);
        rma.IsCustomerPaid = input.IsCustomerPaid;
        rma.PaymentStatus = input.IsCustomerPaid ? input.PaymentStatus : "FreeUnderWarranty";

        // Parse replaced parts if provided
        if (!string.IsNullOrWhiteSpace(input.ReplacedPartsJson))
        {
            try
            {
                var parts = JsonSerializer.Deserialize<List<RmaReplacedPart>>(input.ReplacedPartsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parts != null && parts.Any())
                {
                    rma.ReplacedPartsList = parts;
                    if (rma.PartsCost == 0)
                    {
                        rma.PartsCost = parts.Sum(p => p.SubTotal);
                        rma.TotalCost = Math.Max(0, rma.PartsCost + rma.LaborCost - rma.DiscountAmount);
                    }
                }
            }
            catch
            {
                // Fallback to text parsing
            }
        }

        if (!string.IsNullOrWhiteSpace(input.TechnicianNotes))
        {
            var noteEntry = $"[{DateTime.UtcNow:dd/MM HH:mm}] [{input.NewStatus}] {input.TechnicianNotes}";
            rma.StaffNotes = string.IsNullOrWhiteSpace(rma.StaffNotes) ? noteEntry : $"{rma.StaffNotes}\n{noteEntry}";
        }

        if (input.NewStatus == RmaStatus.Completed || input.NewStatus == RmaStatus.Rejected)
        {
            rma.CompletedAt = DateTime.UtcNow;
        }

        await _rmaRepo.UpdateAsync(rma, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UpdateTicketProgress",
            Module = "Warranty",
            UserEmail = User.Identity?.Name ?? "Technician",
            Details = $"Cập nhật phiếu RMA [{rma.RmaCode}]: Trạng thái {oldStatus} -> {input.NewStatus}, Tổng chi phí: {rma.TotalCost:N0}₫ (Khách trả: {rma.IsCustomerPaid})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật tiến độ RMA [{rma.RmaCode}] sang [{input.NewStatus}] với chi phí {rma.TotalCost:N0}₫!";
        return RedirectToAction(nameof(Index), new { tab = "tickets" });
    }

    /// <summary>
    /// warranty.policy_manage: Lưu / Cập nhật chính sách bảo hành
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Technician")]
    public async Task<IActionResult> SavePolicy(
        WarrantyPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var result = await _warrantyService.SaveWarrantyPolicyAsync(policy, cancellationToken);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "policies" });
    }

    /// <summary>
    /// warranty.policy_manage: Xóa chính sách bảo hành
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeletePolicy(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _warrantyService.DeleteWarrantyPolicyAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "policies" });
    }

    /// <summary>
    /// warranty.policy_manage: Khôi phục chính sách mẫu chuẩn công nghiệp
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Technician")]
    public async Task<IActionResult> ResetDefaultPolicies(CancellationToken cancellationToken = default)
    {
        await ApplyDefaultPoliciesInternalAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã áp dụng bộ chính sách bảo hành chuẩn công nghiệp (Apple AASP, Asus ROG, Synnex FPT)!";
        return RedirectToAction(nameof(Index), new { tab = "policies" });
    }

    /// <summary>
    /// ticket.manage: In biên nhận bảo hành giao khách hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PrintReceipt(string rmaId, CancellationToken cancellationToken = default)
    {
        var rma = await _rmaRepo.GetByIdAsync(rmaId, cancellationToken);
        if (rma == null)
        {
            return NotFound();
        }

        var warranty = await _warrantyRepo.FindOneAsync(w => w.SerialNumber == rma.SerialNumber, cancellationToken);
        ViewBag.Warranty = warranty;

        return PartialView("_RmaPrintReceiptPartial", rma);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Technician")]
    public async Task<IActionResult> AddClaim(
        [FromForm] AddWarrantyClaimInputModel input,
        CancellationToken cancellationToken = default)
    {
        var warranty = await _warrantyRepo.GetByIdAsync(input.WarrantyId, cancellationToken);
        if (warranty == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ bảo hành điện tử.";
            return RedirectToAction(nameof(Index), new { tab = "warranties" });
        }

        var partsList = new List<string>();
        if (!string.IsNullOrWhiteSpace(input.ReplacedParts))
        {
            partsList = input.ReplacedParts
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        var claim = new WarrantyClaim
        {
            ClaimCode = "CLM-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            FaultDescription = input.FaultDescription.Trim(),
            ReceivedDate = DateTime.UtcNow,
            TechnicianName = User.Identity?.Name ?? "Kỹ thuật viên",
            TechnicianNotes = input.TechnicianNotes?.Trim(),
            Status = input.Status,
            ReplacedParts = partsList
        };

        if (input.Status == "Repaired" || input.Status == "ReplacedDevice" || input.Status == "Rejected")
        {
            claim.CompletedDate = DateTime.UtcNow;
        }

        warranty.Claims.Add(claim);
        await _warrantyRepo.UpdateAsync(warranty, cancellationToken);

        TempData["SuccessMessage"] = $"Đã ghi nhận lần sửa chữa [{claim.ClaimCode}] cho máy S/N [{warranty.SerialNumber}]!";
        return RedirectToAction(nameof(Index), new { tab = "warranties" });
    }

    private async Task ApplyDefaultPoliciesInternalAsync(CancellationToken cancellationToken)
    {
        var policies = new List<WarrantyPolicy>
        {
            new()
            {
                PolicyCode = "POL-PHONE",
                PolicyName = "Chính sách Bảo hành Điện thoại Flagship & iPhone Chính Hãng",
                CategoryGroup = "Điện thoại",
                StandardWarrantyMonths = 12,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 3,
                BatteryHealthThreshold = 80,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = true,
                TermsAndConditions = "Bảo hành thân máy, camera, màn hình, FaceID/vân tay chính hãng. Thẩm định qua cổng kỹ thuật Apple GSX & Samsung Smart Tool. 1 đổi 1 trong 30 ngày nếu phát hiện lỗi phần cứng nhà sản xuất.",
                ExclusionClauses = "Từ chối bảo hành đối với các trường hợp: Máy có vết cấn móp biến dạng khung sườn, rơi vỡ mặt kính, quỳ tím chuyển màu đỏ/hồng (dính nước), can thiệp mạch điện tử hoặc bị khóa iCloud/Knox.",
                IsActive = true,
                DisplayOrder = 1,
                Notes = "Tiêu chuẩn ủy quyền AASP (Apple Authorised Service Provider) & TTBH Samsung Premium"
            },
            new()
            {
                PolicyCode = "POL-LAPTOP",
                PolicyName = "Chính sách Bảo hành Laptop Doanh Nhân & Gaming Cao Cấp",
                CategoryGroup = "Laptop",
                StandardWarrantyMonths = 24,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 3,
                BatteryHealthThreshold = 75,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = true,
                TermsAndConditions = "Bảo hành 24 tháng chính hãng cho toàn bộ CPU, GPU, Bo mạch chủ, Bàn phím và Màn hình. Miễn phí vệ sinh, thay keo tản nhiệt gốm cao cấp định kỳ trọn đời máy tại trung tâm TechZone.",
                ExclusionClauses = "Không bảo hành bàn phím bị đổ chất lỏng (nước ngọt, cà phê), bản lề cong gãy do mở quá góc thiết kế, màn hình nứt chảy mực, rách tem niêm phong ốc vít bo mạch.",
                IsActive = true,
                DisplayOrder = 2,
                Notes = "Áp dụng cho Asus ROG, Dell XPS, Lenovo Legion, HP Omen và MacBook Pro/Air"
            },
            new()
            {
                PolicyCode = "POL-PC-PARTS",
                PolicyName = "Chính sách Bảo hành Linh kiện PC & Bo Mạch Chủ (Mainboard/VGA)",
                CategoryGroup = "Linh kiện PC",
                StandardWarrantyMonths = 36,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 0,
                BatteryHealthThreshold = 0,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = false,
                TermsAndConditions = "Bảo hành 36 tháng theo serial number đối soát trực tiếp với nhà phân phối chính thức (Synnex FPT, Viễn Sơn, Digiworld). 1 đổi 1 ngay lập tức trong 30 ngày đầu tiên nếu phát sinh lỗi.",
                ExclusionClauses = "Cong vênh hoặc gãy chân socket CPU, cháy nổ nứt IC nguồn do sét đánh/chập điện gia dụng, rỉ sét linh kiện do môi trường ẩm mốc hoặc sử dụng cho mục đích đào coin (crypto mining).",
                IsActive = true,
                DisplayOrder = 3,
                Notes = "Bảo hành chuẩn nhà phân phối Synnex FPT & Viễn Sơn ủy quyền"
            },
            new()
            {
                PolicyCode = "POL-MONITOR",
                PolicyName = "Chính sách Bảo hành Màn hình Đồ Họa & Gaming Siêu Tần Số",
                CategoryGroup = "Màn hình",
                StandardWarrantyMonths = 36,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 3,
                BatteryHealthThreshold = 0,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = true,
                TermsAndConditions = "Bảo hành 36 tháng chính hãng. Áp dụng chính sách Zero Bright Dot (0 điểm sáng đổi mới ngay lập tức trong 1 năm đầu). Miễn phí dịch vụ cân chỉnh màu sắc chuyên nghiệp bằng Spyder X Elite.",
                ExclusionClauses = "Nứt vỡ panel màn hình do tác động cơ học ngoại lực, sọc màn hình do cấn vật cứng khi gập hoặc lau chùi bằng dung dịch tẩy rửa ăn mòn.",
                IsActive = true,
                DisplayOrder = 4,
                Notes = "Hỗ trợ Dell UltraSharp, Asus ProArt, LG UltraGear, Samsung Odyssey"
            },
            new()
            {
                PolicyCode = "POL-ACCESSORY",
                PolicyName = "Chính sách Bảo hành Phụ kiện Âm thanh & Củ Cáp Sạc Nhanh",
                CategoryGroup = "Phụ kiện",
                StandardWarrantyMonths = 12,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 0,
                BatteryHealthThreshold = 0,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = false,
                TermsAndConditions = "Chính sách 1 đổi 1 trực tiếp ngay tại quầy thu ngân trong suốt 12 tháng nếu củ sạc, cáp sạc, sạc dự phòng không vào điện hoặc tai nghe lỗi kết nối Bluetooth.",
                ExclusionClauses = "Đứt gãy đầu cáp sạc do lực kéo giật mạnh, biến dạng chân cắm kim loại, tai nghe ngấm nước mồ hôi hoặc mất hộp sạc dock charging.",
                IsActive = true,
                DisplayOrder = 5,
                Notes = "Áp dụng Anker, Belkin, Sony, Apple, Marshall, Ugreen"
            }
        };

        foreach (var p in policies)
        {
            var exists = await _policyRepo.FindOneAsync(x => x.PolicyCode == p.PolicyCode, cancellationToken);
            if (exists == null)
            {
                await _policyRepo.AddAsync(p, cancellationToken);
            }
        }
    }

    private async Task SeedInitialRmaAndWarrantyDataAsync(CancellationToken cancellationToken)
    {
        var sampleWarranties = new List<Warranty>
        {
            new()
            {
                SerialNumber = "IP16P-VN-009124",
                Imei = "358712345678901",
                ProductName = "iPhone 16 Pro Max 256GB Titan Tự Nhiên",
                VariantSku = "IP16PM-256-NAT",
                OrderCode = "TZ-20261014-9841",
                CustomerName = "Nguyễn Hoàng Nam",
                CustomerPhone = "0908123456",
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(10),
                WarrantyMonths = 12,
                Status = WarrantyStatus.Active,
                Claims = new List<WarrantyClaim>
                {
                    new()
                    {
                        ClaimCode = "CLM-881201",
                        FaultDescription = "Loa trong nghe rè nhẹ khi đàm thoại",
                        ReceivedDate = DateTime.UtcNow.AddDays(-14),
                        CompletedDate = DateTime.UtcNow.AddDays(-12),
                        Status = "Repaired",
                        TechnicianName = "Lê Kỹ Thuật",
                        TechnicianNotes = "Vệ sinh màng loa âm thanh, hiệu chỉnh dải tần loa thoại",
                        ReplacedParts = new List<string> { "Ron kháng nước loa trong" }
                    }
                }
            },
            new()
            {
                SerialNumber = "MACM3-VN-883192",
                Imei = "C02G1234MD6R",
                ProductName = "MacBook Pro 14 M3 Pro 18GB/512GB Space Black",
                VariantSku = "MBP14-M3PRO-BLK",
                OrderCode = "TZ-20260905-1123",
                CustomerName = "Trần Thị Mai Anh",
                CustomerPhone = "0912345678",
                StartDate = DateTime.UtcNow.AddMonths(-11),
                EndDate = DateTime.UtcNow.AddDays(25),
                WarrantyMonths = 12,
                Status = WarrantyStatus.ExpiringSoon,
                Claims = new List<WarrantyClaim>()
            },
            new()
            {
                SerialNumber = "SAM-S24U-771239",
                Imei = "359981273618291",
                ProductName = "Samsung Galaxy S24 Ultra 12GB/512GB Xám Titan",
                VariantSku = "S24U-512-GRY",
                OrderCode = "TZ-20260810-4491",
                CustomerName = "Võ Văn Tuấn",
                CustomerPhone = "0977889900",
                StartDate = DateTime.UtcNow.AddMonths(-3),
                EndDate = DateTime.UtcNow.AddMonths(9),
                WarrantyMonths = 12,
                Status = WarrantyStatus.Active,
                Claims = new List<WarrantyClaim>()
            }
        };

        foreach (var w in sampleWarranties)
        {
            await _warrantyRepo.AddAsync(w, cancellationToken);
        }

        var sampleRmas = new List<RmaRequest>
        {
            new()
            {
                RmaCode = "RMA-261015-A101",
                OrderCode = "TZ-20261014-9841",
                ProductName = "iPhone 16 Pro Max 256GB Titan Tự Nhiên",
                SerialNumber = "IP16P-VN-009124",
                CustomerName = "Nguyễn Hoàng Nam",
                CustomerPhone = "0908123456",
                ReasonType = "ScreenFault",
                CustomerDescription = "Màn hình xuất hiện sọc xanh dọc sau khi cắm sạc qua đêm, không cấn móp.",
                Status = RmaStatus.Repairing,
                ResolutionType = "ExchangeNew",
                AssignedTechnicianName = "Lê Kỹ Thuật (Chuyên Apple/Laptop)",
                AssignedAt = DateTime.UtcNow.AddHours(-16),
                EstimatedCompletionDate = DateTime.UtcNow.AddDays(2),
                Priority = "Urgent",
                PartsCost = 6500000m,
                LaborCost = 0,
                TotalCost = 0,
                IsCustomerPaid = false,
                PaymentStatus = "FreeUnderWarranty",
                StaffNotes = "Đã kiểm tra quỳ tím trắng, đủ điều kiện bảo hành Apple AASP. Đang thay cụm màn hình OLED chính hãng.",
                InspectedByStaffName = "Lê Kỹ Thuật",
                PickupCode = "TZ8891",
                ReplacedPartsList = new List<RmaReplacedPart>
                {
                    new()
                    {
                        PartCode = "PART-IP16PM-DISP",
                        PartName = "Cụm Màn hình Super Retina XDR OLED iPhone 16 Pro Max",
                        UnitPrice = 6500000m,
                        Quantity = 1,
                        WarrantyMonths = 6
                    }
                },
                CreatedAt = DateTime.UtcNow.AddHours(-18)
            },
            new()
            {
                RmaCode = "RMA-261014-B202",
                OrderCode = "TZ-20261001-3312",
                ProductName = "Bàn phím cơ không dây Asus ROG Azoth Moonlight",
                SerialNumber = "ROG-KB-882194",
                CustomerName = "Đặng Quang Huy",
                CustomerPhone = "0981122334",
                ReasonType = "HardwareDefect",
                CustomerDescription = "Phím Spacebar bị chập chờn, Bluetooth thỉnh thoảng mất kết nối sau 10 phút.",
                Status = RmaStatus.ReadyForPickup,
                ResolutionType = "FreeRepair",
                AssignedTechnicianName = "Trần Phần Cứng (Chuyên PC/VGA)",
                AssignedAt = DateTime.UtcNow.AddDays(-2),
                EstimatedCompletionDate = DateTime.UtcNow,
                Priority = "Normal",
                PartsCost = 150000m,
                LaborCost = 0,
                TotalCost = 0,
                IsCustomerPaid = false,
                PaymentStatus = "FreeUnderWarranty",
                StaffNotes = "Đã thay switch ROG NX Snow phím Space. Đã nạp firmware 1.0.8, test kết nối ổn định 100%.",
                InspectedByStaffName = "Trần Phần Cứng",
                PickupCode = "TZ3412",
                ReplacedPartsList = new List<RmaReplacedPart>
                {
                    new()
                    {
                        PartCode = "SW-ROG-SNOW",
                        PartName = "Switch cơ học Asus ROG NX Snow",
                        UnitPrice = 150000m,
                        Quantity = 1,
                        WarrantyMonths = 12
                    }
                },
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                RmaCode = "RMA-261010-C303",
                OrderCode = "TZ-20260920-5591",
                ProductName = "Tai nghe Sony WH-1000XM5 Đen",
                SerialNumber = "SNY-XM5-110942",
                CustomerName = "Phạm Quỳnh Như",
                CustomerPhone = "0933557799",
                ReasonType = "DOA",
                CustomerDescription = "Khách mở hộp sạc không vào điện, đèn đỏ nhấp nháy liên tục.",
                Status = RmaStatus.Completed,
                ResolutionType = "ExchangeNew",
                ReplacementSerialNumber = "SNY-XM5-998201",
                AssignedTechnicianName = "Lê Kỹ Thuật (Chuyên Apple/Laptop)",
                PartsCost = 0,
                LaborCost = 0,
                TotalCost = 0,
                IsCustomerPaid = false,
                PaymentStatus = "FreeUnderWarranty",
                StaffNotes = "Đã xác nhận lỗi nguồn xuất xưởng. Đã xuất kho đổi mới 1-1 cho khách hàng.",
                InspectedByStaffName = "Lê Kỹ Thuật",
                PickupCode = "TZ9910",
                CompletedAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        foreach (var r in sampleRmas)
        {
            await _rmaRepo.AddAsync(r, cancellationToken);
        }
    }
}
