using TechZone.Application.Common.Interfaces;
using TechZone.Application.Common.Models;
using TechZone.Application.DTOs.Warranty;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;

namespace TechZone.Application.Services;

public interface IWarrantyService
{
    Task<Result<WarrantyLookupResultDto>> LookupAsync(string serialOrImei, CancellationToken cancellationToken = default);
    Task<Result<DeviceLifecycleDto>> GetDeviceLifecycleAsync(string serialOrImei, CancellationToken cancellationToken = default);
    Task<List<WarrantyPolicy>> GetWarrantyPoliciesAsync(CancellationToken cancellationToken = default);
    Task<WarrantyPolicy?> GetWarrantyPolicyByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<WarrantyPolicy>> SaveWarrantyPolicyAsync(WarrantyPolicy policy, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteWarrantyPolicyAsync(string id, CancellationToken cancellationToken = default);
}

public class WarrantyService : IWarrantyService
{
    private readonly IRepository<Warranty> _warrantyRepo;
    private readonly IRepository<InventoryItem> _inventoryRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<RmaRequest> _rmaRepo;
    private readonly IRepository<WarrantyPolicy> _policyRepo;
    private readonly ICacheService _cache;

    public WarrantyService(
        IRepository<Warranty> warrantyRepo,
        IRepository<InventoryItem> inventoryRepo,
        IRepository<Order> orderRepo,
        IRepository<RmaRequest> rmaRepo,
        IRepository<WarrantyPolicy> policyRepo,
        ICacheService cache)
    {
        _warrantyRepo = warrantyRepo;
        _inventoryRepo = inventoryRepo;
        _orderRepo = orderRepo;
        _rmaRepo = rmaRepo;
        _policyRepo = policyRepo;
        _cache = cache;
    }

    public async Task<Result<WarrantyLookupResultDto>> LookupAsync(string serialOrImei, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialOrImei))
        {
            return Result<WarrantyLookupResultDto>.Failure("Vui lòng nhập số Serial hoặc IMEI thiết bị.");
        }

        var normalized = serialOrImei.Trim().ToUpper();
        var cacheKey = $"warranty:{normalized}";

        var cached = await _cache.GetAsync<WarrantyLookupResultDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return Result<WarrantyLookupResultDto>.Success(cached);
        }

        var warranty = await _warrantyRepo.FindOneAsync(
            w => w.SerialNumber.ToUpper() == normalized || (w.Imei != null && w.Imei.ToUpper() == normalized),
            cancellationToken);

        if (warranty == null)
        {
            return Result<WarrantyLookupResultDto>.Failure("Không tìm thấy thông tin bảo hành cho số Serial/IMEI này.");
        }

        var rawPhone = warranty.CustomerPhone ?? string.Empty;
        var maskedPhone = rawPhone.Length > 6
            ? rawPhone[..3] + "****" + rawPhone[^3..]
            : rawPhone;

        var dto = new WarrantyLookupResultDto
        {
            SerialNumber = warranty.SerialNumber,
            Imei = warranty.Imei,
            ProductName = warranty.ProductName,
            VariantName = warranty.VariantName,
            OrderCode = warranty.OrderCode,
            MaskedPhone = maskedPhone,
            StartDate = warranty.StartDate,
            EndDate = warranty.EndDate,
            WarrantyMonths = warranty.WarrantyMonths,
            Status = warranty.Status,
            Claims = warranty.Claims.Select(c => new WarrantyClaimItemDto
            {
                ClaimCode = c.ClaimCode,
                FaultDescription = c.FaultDescription,
                ReceivedDate = c.ReceivedDate,
                CompletedDate = c.CompletedDate,
                Status = c.Status,
                TechnicianNotes = c.TechnicianNotes
            }).ToList()
        };

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(24), cancellationToken);
        return Result<WarrantyLookupResultDto>.Success(dto);
    }

    public async Task<Result<DeviceLifecycleDto>> GetDeviceLifecycleAsync(string serialOrImei, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialOrImei))
        {
            return Result<DeviceLifecycleDto>.Failure("Vui lòng nhập số Serial hoặc IMEI thiết bị.");
        }

        var normalized = serialOrImei.Trim().ToUpper();

        // 1. Check physical warehouse inventory
        var invItem = await _inventoryRepo.FindOneAsync(
            i => i.SerialNumber.ToUpper() == normalized || (i.Imei2 != null && i.Imei2.ToUpper() == normalized),
            cancellationToken);

        // 2. Check electronic warranty registry
        var warranty = await _warrantyRepo.FindOneAsync(
            w => w.SerialNumber.ToUpper() == normalized || (w.Imei != null && w.Imei.ToUpper() == normalized),
            cancellationToken);

        // 3. Check RMA technical service tickets
        var rmas = await _rmaRepo.FindAsync(
            r => r.SerialNumber.ToUpper() == normalized,
            cancellationToken);

        if (invItem == null && warranty == null && (!rmas.Any()))
        {
            return Result<DeviceLifecycleDto>.Failure($"Không tìm thấy bất kỳ thông tin nào gắn với mã Serial/IMEI [{normalized}] trong toàn bộ hệ thống.");
        }

        // 4. Check corresponding commercial order
        string? targetOrderCode = warranty?.OrderCode ?? invItem?.AssignedOrderCode;
        Order? order = null;
        if (!string.IsNullOrWhiteSpace(targetOrderCode))
        {
            order = await _orderRepo.FindOneAsync(o => o.OrderCode == targetOrderCode, cancellationToken);
        }

        var productName = warranty?.ProductName ?? invItem?.ProductName ?? rmas.FirstOrDefault()?.ProductName ?? "Thiết bị điện tử";
        var variantName = warranty?.VariantName ?? invItem?.VariantName ?? "Tiêu chuẩn";
        var variantSku = warranty?.VariantSku ?? invItem?.VariantSku ?? "N/A";

        var lifecycle = new DeviceLifecycleDto
        {
            SerialNumber = warranty?.SerialNumber ?? invItem?.SerialNumber ?? normalized,
            Imei = warranty?.Imei ?? invItem?.Imei2,
            ProductName = productName,
            VariantName = variantName,
            VariantSku = variantSku
        };

        // Populate Warehouse & Inventory stage
        if (invItem != null)
        {
            lifecycle.HasInventoryRecord = true;
            lifecycle.ImportBatchCode = invItem.ImportBatchCode;
            lifecycle.SupplierName = invItem.SupplierName ?? "Nhà phân phối ủy quyền";
            lifecycle.CostPrice = invItem.CostPrice;
            lifecycle.ReceivedDate = invItem.ReceivedDate;
            lifecycle.WarehouseLocation = invItem.WarehouseLocation;
            lifecycle.ExportSlipCode = invItem.ExportSlipCode;
            lifecycle.InventoryStatus = invItem.Status.ToString();
        }

        // Populate Order stage
        if (order != null)
        {
            lifecycle.HasOrderRecord = true;
            lifecycle.OrderId = order.Id;
            lifecycle.OrderCode = order.OrderCode;
            lifecycle.CustomerName = order.CustomerName;
            lifecycle.CustomerPhone = order.CustomerPhone;
            lifecycle.CustomerEmail = order.CustomerEmail;
            lifecycle.OrderDate = order.CreatedAt;
            lifecycle.SalePrice = order.Items.FirstOrDefault(i => i.AssignedSerialNumbers.Contains(lifecycle.SerialNumber) || i.VariantSku == variantSku)?.UnitPrice ?? order.TotalAmount;
            lifecycle.SourceChannel = order.SourceChannel;
        }

        // Populate E-Warranty stage
        if (warranty != null)
        {
            lifecycle.HasWarrantyRecord = true;
            lifecycle.WarrantyId = warranty.Id;
            lifecycle.WarrantyStartDate = warranty.StartDate;
            lifecycle.WarrantyEndDate = warranty.EndDate;
            lifecycle.WarrantyMonths = warranty.WarrantyMonths;
            lifecycle.WarrantyStatus = warranty.Status;
            lifecycle.DaysRemaining = warranty.DaysRemaining;
            lifecycle.IsWarrantyActive = warranty.IsActive;
        }

        // Populate RMA Technical Service history
        lifecycle.RmaTickets = rmas.OrderByDescending(r => r.CreatedAt).Select(r => new RmaTicketSummaryDto
        {
            Id = r.Id,
            RmaCode = r.RmaCode,
            CreatedAt = r.CreatedAt,
            ReasonType = r.ReasonType,
            CustomerDescription = r.CustomerDescription,
            Status = r.Status,
            ResolutionType = r.ResolutionType,
            AssignedTechnicianName = r.AssignedTechnicianName ?? r.InspectedByStaffName ?? "Chưa phân công",
            PartsCost = r.PartsCost,
            LaborCost = r.LaborCost,
            TotalCost = r.TotalCost,
            IsCustomerPaid = r.IsCustomerPaid,
            PaymentStatus = r.PaymentStatus,
            ReplacedPartsCount = r.ReplacedPartsList.Count,
            ReplacementSerialNumber = r.ReplacementSerialNumber,
            CompletedAt = r.CompletedAt
        }).ToList();

        // Determine current overall lifecycle stage
        var activeTicket = rmas.FirstOrDefault(r => r.Status != RmaStatus.Completed && r.Status != RmaStatus.Rejected);
        if (activeTicket != null)
        {
            lifecycle.CurrentStage = "InRepair";
            lifecycle.CurrentStageText = $"Đang sửa chữa tại TTBH ({activeTicket.Status})";
            lifecycle.BadgeCssClass = "bg-danger";
        }
        else if (warranty != null)
        {
            if (warranty.IsActive && warranty.DaysRemaining > 30)
            {
                lifecycle.CurrentStage = "SoldActiveWarranty";
                lifecycle.CurrentStageText = $"Khách đang sử dụng - Còn hạn bảo hành ({warranty.DaysRemaining} ngày)";
                lifecycle.BadgeCssClass = "bg-success";
            }
            else if (warranty.IsActive && warranty.DaysRemaining <= 30)
            {
                lifecycle.CurrentStage = "SoldWarrantyExpiring";
                lifecycle.CurrentStageText = $"Sắp hết hạn bảo hành ({warranty.DaysRemaining} ngày)";
                lifecycle.BadgeCssClass = "bg-warning text-dark";
            }
            else
            {
                lifecycle.CurrentStage = "SoldWarrantyExpired";
                lifecycle.CurrentStageText = "Đã hết hạn bảo hành chính hãng";
                lifecycle.BadgeCssClass = "bg-secondary";
            }
        }
        else if (invItem != null)
        {
            if (invItem.Status == InventoryStatus.InStock)
            {
                lifecycle.CurrentStage = "InStock";
                lifecycle.CurrentStageText = "Thiết bị nguyên seal - Đang lưu kho vật lý";
                lifecycle.BadgeCssClass = "bg-primary";
            }
            else
            {
                lifecycle.CurrentStage = "SoldActiveWarranty";
                lifecycle.CurrentStageText = "Đã xuất bán - Chưa kích hoạt E-Warranty";
                lifecycle.BadgeCssClass = "bg-info text-dark";
            }
        }

        return Result<DeviceLifecycleDto>.Success(lifecycle);
    }

    public async Task<List<WarrantyPolicy>> GetWarrantyPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _policyRepo.GetAllAsync(cancellationToken);
        return policies.OrderBy(p => p.DisplayOrder).ToList();
    }

    public async Task<WarrantyPolicy?> GetWarrantyPolicyByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _policyRepo.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<WarrantyPolicy>> SaveWarrantyPolicyAsync(WarrantyPolicy policy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policy.PolicyCode) || string.IsNullOrWhiteSpace(policy.PolicyName))
        {
            return Result<WarrantyPolicy>.Failure("Mã chính sách và Tên chính sách không được để trống.");
        }

        if (string.IsNullOrEmpty(policy.Id))
        {
            // Insert
            await _policyRepo.AddAsync(policy, cancellationToken);
            return Result<WarrantyPolicy>.Success(policy, $"Đã tạo thành công chính sách bảo hành [{policy.PolicyName}]!");
        }
        else
        {
            var existing = await _policyRepo.GetByIdAsync(policy.Id, cancellationToken);
            if (existing == null)
            {
                return Result<WarrantyPolicy>.Failure("Không tìm thấy chính sách bảo hành để cập nhật.");
            }

            existing.PolicyCode = policy.PolicyCode.Trim().ToUpper();
            existing.PolicyName = policy.PolicyName.Trim();
            existing.CategoryGroup = policy.CategoryGroup.Trim();
            existing.StandardWarrantyMonths = policy.StandardWarrantyMonths;
            existing.DoaPeriodDays = policy.DoaPeriodDays;
            existing.ScreenDeadPixelThreshold = policy.ScreenDeadPixelThreshold;
            existing.BatteryHealthThreshold = policy.BatteryHealthThreshold;
            existing.IsReturnExchangeSupported = policy.IsReturnExchangeSupported;
            existing.ExtendedWarrantySupported = policy.ExtendedWarrantySupported;
            existing.TermsAndConditions = policy.TermsAndConditions;
            existing.ExclusionClauses = policy.ExclusionClauses;
            existing.IsActive = policy.IsActive;
            existing.DisplayOrder = policy.DisplayOrder;
            existing.Notes = policy.Notes;

            await _policyRepo.UpdateAsync(existing, cancellationToken);
            return Result<WarrantyPolicy>.Success(existing, $"Đã cập nhật chính sách bảo hành [{existing.PolicyName}]!");
        }
    }

    public async Task<Result<bool>> DeleteWarrantyPolicyAsync(string id, CancellationToken cancellationToken = default)
    {
        var existing = await _policyRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return Result<bool>.Failure("Chính sách không tồn tại.");
        }

        await _policyRepo.DeleteAsync(id, softDelete: false, cancellationToken);
        return Result<bool>.Success(true, $"Đã xóa chính sách bảo hành [{existing.PolicyName}].");
    }
}
