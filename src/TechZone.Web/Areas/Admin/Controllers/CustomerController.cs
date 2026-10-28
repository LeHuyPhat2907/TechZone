using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin,SalesStaff")]
public class CustomerController : AdminBaseController
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;

    public CustomerController(
        IRepository<User> userRepo,
        IRepository<Order> orderRepo,
        IRepository<SecurityAuditLog> auditLogRepo)
    {
        _userRepo = userRepo;
        _orderRepo = orderRepo;
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? tier = "All",
        string? status = "All",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 15;
        page = Math.Max(1, page);

        var allUsers = await _userRepo.GetAllAsync(cancellationToken);
        var allOrders = await _orderRepo.GetAllAsync(cancellationToken);

        var customerUsers = allUsers.Where(u => u.Role == UserRole.Customer).ToList();

        // Calculate KPI metrics
        var model = new CustomerManagementViewModel
        {
            TotalCustomers = customerUsers.Count,
            ActiveCustomers = customerUsers.Count(c => c.IsActive),
            BannedCustomers = customerUsers.Count(c => !c.IsActive),
            DiamondCustomers = customerUsers.Count(c => c.RewardPoints >= 5000),
            GoldCustomers = customerUsers.Count(c => c.RewardPoints >= 2000 && c.RewardPoints < 5000),
            SilverCustomers = customerUsers.Count(c => c.RewardPoints >= 500 && c.RewardPoints < 2000),
            BronzeCustomers = customerUsers.Count(c => c.RewardPoints < 500),
            TotalRewardPoints = customerUsers.Sum(c => c.RewardPoints),
            SearchQuery = search,
            TierFilter = string.IsNullOrWhiteSpace(tier) ? "All" : tier,
            StatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status,
            Page = page,
            PageSize = pageSize
        };

        // Group order statistics by UserId
        var orderStatsByUser = allOrders
            .Where(o => !string.IsNullOrEmpty(o.UserId))
            .GroupBy(o => o.UserId!)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    Spent = g.Where(o => o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                    LastOrder = g.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt
                });

        var customerItems = customerUsers.Select(u =>
        {
            var item = new CustomerItemDto { User = u };
            if (orderStatsByUser.TryGetValue(u.Id, out var stats))
            {
                item.TotalOrders = stats.Count;
                item.TotalSpent = stats.Spent;
                item.LastOrderDate = stats.LastOrder;
            }
            return item;
        }).AsEnumerable();

        // Filter: Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            customerItems = customerItems.Where(c =>
                (!string.IsNullOrEmpty(c.User.FullName) && c.User.FullName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.User.Email) && c.User.Email.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.User.PhoneNumber) && c.User.PhoneNumber.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.User.Id) && c.User.Id.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        // Filter: Tier
        if (!string.IsNullOrWhiteSpace(tier) && !tier.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            customerItems = customerItems.Where(c => c.MembershipTier.Equals(tier, StringComparison.OrdinalIgnoreCase));
        }

        // Filter: Status
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                customerItems = customerItems.Where(c => c.User.IsActive);
            }
            else if (status.Equals("Banned", StringComparison.OrdinalIgnoreCase))
            {
                customerItems = customerItems.Where(c => !c.User.IsActive);
            }
        }

        var filteredList = customerItems
            .OrderByDescending(c => c.TotalSpent)
            .ThenByDescending(c => c.User.RewardPoints)
            .ThenByDescending(c => c.User.CreatedAt)
            .ToList();

        model.TotalFilteredCount = filteredList.Count;
        model.Customers = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCustomer(
        [FromForm] UpdateCustomerInputModel input,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(input.CustomerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(input.FullName))
        {
            TempData["ErrorMessage"] = "Họ và tên khách hàng không được để trống.";
            return RedirectToAction(nameof(Index));
        }

        user.FullName = input.FullName.Trim();
        user.PhoneNumber = input.PhoneNumber?.Trim();
        user.AdminNotes = input.AdminNotes?.Trim();
        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UpdateCustomerProfile",
            Module = "CRM",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Cập nhật thông tin khách hàng [{user.FullName}] (Email: {user.Email}, SĐT: {user.PhoneNumber})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật hồ sơ khách hàng [{user.FullName}] thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustPoints(
        [FromForm] AdjustCustomerPointsInputModel input,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(input.CustomerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        var oldPoints = user.RewardPoints;
        user.RewardPoints = Math.Max(0, user.RewardPoints + input.PointsDelta);
        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "AdjustCustomerPoints",
            Module = "CRM",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Điều chỉnh điểm thưởng khách [{user.FullName}]: {oldPoints} -> {user.RewardPoints} (Delta: {input.PointsDelta:+0;-0}). Lý do: {input.Reason ?? "CSKH can thiệp"}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật điểm thưởng cho [{user.FullName}] thành {user.RewardPoints} điểm (Hạng: {user.MembershipTierName})!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanCustomer(
        [FromForm] CustomerBanInputModel input,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(input.CustomerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = false;
        user.BannedReason = string.IsNullOrWhiteSpace(input.Reason) ? "Vi phạm chính sách nền tảng" : input.Reason.Trim();
        user.BannedAt = DateTime.UtcNow;
        user.BannedBy = User.Identity?.Name ?? "Admin";

        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "BanCustomer",
            Module = "CRM",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Khóa tài khoản khách hàng [{user.FullName}] (Email: {user.Email}). Lý do: {user.BannedReason}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã khóa tài khoản khách hàng [{user.FullName}]. Lý do: {user.BannedReason}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbanCustomer(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(customerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = true;
        var prevReason = user.BannedReason;
        user.BannedReason = null;
        user.BannedAt = null;
        user.BannedBy = null;

        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UnbanCustomer",
            Module = "CRM",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"Mở khóa tài khoản khách hàng [{user.FullName}] (Trước đó bị khóa vì: {prevReason ?? "N/A"})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã mở khóa tài khoản cho khách hàng [{user.FullName}] thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetAddresses(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(customerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            return Json(new { success = false, message = "Không tìm thấy khách hàng." });
        }

        return Json(new
        {
            success = true,
            customerName = user.FullName,
            addresses = user.Addresses.Select(a => new
            {
                a.Id,
                a.ReceiverName,
                a.PhoneNumber,
                a.Province,
                a.District,
                a.Ward,
                a.DetailAddress,
                a.AddressType,
                a.IsDefault,
                a.FullAddress
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(
        [FromForm] CustomerAddressInputModel input,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(input.CustomerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(input.ReceiverName) || string.IsNullOrWhiteSpace(input.PhoneNumber) || string.IsNullOrWhiteSpace(input.DetailAddress))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên người nhận, số điện thoại và địa chỉ cụ thể.";
            return RedirectToAction(nameof(Index));
        }

        UserAddress targetAddress;
        bool isNew = string.IsNullOrWhiteSpace(input.AddressId);

        if (isNew)
        {
            targetAddress = new UserAddress
            {
                Id = Guid.NewGuid().ToString("N")
            };
            user.Addresses.Add(targetAddress);
        }
        else
        {
            var found = user.Addresses.FirstOrDefault(a => a.Id == input.AddressId);
            if (found == null)
            {
                targetAddress = new UserAddress { Id = input.AddressId ?? Guid.NewGuid().ToString("N") };
                user.Addresses.Add(targetAddress);
            }
            else
            {
                targetAddress = found;
            }
        }

        targetAddress.ReceiverName = input.ReceiverName.Trim();
        targetAddress.PhoneNumber = input.PhoneNumber.Trim();
        targetAddress.Province = input.Province?.Trim() ?? string.Empty;
        targetAddress.District = input.District?.Trim() ?? string.Empty;
        targetAddress.Ward = input.Ward?.Trim() ?? string.Empty;
        targetAddress.DetailAddress = input.DetailAddress.Trim();
        targetAddress.AddressType = string.IsNullOrWhiteSpace(input.AddressType) ? "Home" : input.AddressType.Trim();

        if (input.IsDefault || user.Addresses.Count == 1)
        {
            foreach (var a in user.Addresses)
            {
                a.IsDefault = false;
            }
            targetAddress.IsDefault = true;
        }

        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = isNew ? "AddCustomerAddress" : "EditCustomerAddress",
            Module = "CRM",
            UserEmail = User.Identity?.Name ?? "Admin",
            Details = $"{(isNew ? "Thêm mới" : "Chỉnh sửa")} địa chỉ giao hàng cho khách [{user.FullName}]: {targetAddress.FullAddress}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã lưu địa chỉ nhận hàng cho khách [{user.FullName}] thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(
        string customerId,
        string addressId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(customerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        var addr = user.Addresses.FirstOrDefault(a => a.Id == addressId);
        if (addr != null)
        {
            bool wasDefault = addr.IsDefault;
            user.Addresses.Remove(addr);

            if (wasDefault && user.Addresses.Any())
            {
                user.Addresses[0].IsDefault = true;
            }

            await _userRepo.UpdateAsync(user, cancellationToken);

            await _auditLogRepo.AddAsync(new SecurityAuditLog
            {
                Action = "DeleteCustomerAddress",
                Module = "CRM",
                UserEmail = User.Identity?.Name ?? "Admin",
                Details = $"Xóa địa chỉ giao hàng [{addr.FullAddress}] của khách [{user.FullName}]"
            }, cancellationToken);

            TempData["SuccessMessage"] = "Đã xóa địa chỉ thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(
        string customerId,
        string addressId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(customerId, cancellationToken);
        if (user == null || user.Role != UserRole.Customer)
        {
            TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        var addr = user.Addresses.FirstOrDefault(a => a.Id == addressId);
        if (addr != null)
        {
            foreach (var a in user.Addresses)
            {
                a.IsDefault = false;
            }
            addr.IsDefault = true;

            await _userRepo.UpdateAsync(user, cancellationToken);

            await _auditLogRepo.AddAsync(new SecurityAuditLog
            {
                Action = "SetDefaultAddress",
                Module = "CRM",
                UserEmail = User.Identity?.Name ?? "Admin",
                Details = $"Đặt địa chỉ [{addr.FullAddress}] làm mặc định cho khách [{user.FullName}]"
            }, cancellationToken);

            TempData["SuccessMessage"] = "Đã đặt làm địa chỉ mặc định!";
        }

        return RedirectToAction(nameof(Index));
    }
}
