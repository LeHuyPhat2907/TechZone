using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Domain.Common;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class RbacController : AdminBaseController
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<SecurityAuditLog> _auditLogRepo;
    private readonly IPasswordHasher _passwordHasher;

    public RbacController(
        IRepository<User> userRepo,
        IRepository<Role> roleRepo,
        IRepository<SecurityAuditLog> auditLogRepo,
        IPasswordHasher passwordHasher)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _auditLogRepo = auditLogRepo;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? roleFilter = "All",
        string tab = "staff",
        CancellationToken cancellationToken = default)
    {
        var allUsers = await _userRepo.GetAllAsync(cancellationToken);
        var allRoles = await _roleRepo.GetAllAsync(cancellationToken);

        // Auto-seed default roles if empty
        if (!allRoles.Any())
        {
            await SeedDefaultRolesAsync(cancellationToken);
            allRoles = await _roleRepo.GetAllAsync(cancellationToken);
        }

        // Map user count into roles
        foreach (var r in allRoles)
        {
            r.UserCount = allUsers.Count(u => u.Role == r.BaseRole || u.CustomRoleId == r.Id);
        }

        // Filter Staff Members
        var staffQuery = allUsers.Where(u => u.Role != UserRole.Customer).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            staffQuery = staffQuery.Where(u =>
                (!string.IsNullOrEmpty(u.FullName) && u.FullName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.Department) && u.Department.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter) && !roleFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<UserRole>(roleFilter, out var parsedRole))
            {
                staffQuery = staffQuery.Where(u => u.Role == parsedRole);
            }
        }

        var viewModel = new RbacViewModel
        {
            Users = allUsers,
            StaffMembers = staffQuery.OrderByDescending(u => u.Role).ThenBy(u => u.FullName).ToList(),
            Roles = allRoles.OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name).ToList(),
            ActiveTab = tab.ToLowerInvariant() switch
            {
                "roles" => "roles",
                "matrix" => "matrix",
                "guardrails" => "guardrails",
                _ => "staff"
            },
            SearchQuery = search,
            RoleFilter = string.IsNullOrWhiteSpace(roleFilter) ? "All" : roleFilter
        };

        return View(viewModel);
    }

    // ==============================================================
    // 1.6.1 STAFF MANAGEMENT (staff.create | update | delete)
    // ==============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(
        [FromForm] CreateStaffInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên, Email và Mật khẩu khởi tạo.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        var normalizedEmail = input.Email.Trim().ToLowerInvariant();
        var existing = (await _userRepo.GetAllAsync(cancellationToken))
            .FirstOrDefault(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            TempData["ErrorMessage"] = $"Email [{normalizedEmail}] đã được sử dụng bởi một tài khoản khác.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        // Determine permissions
        var permissions = input.Permissions?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList() ?? new List<string>();
        if (!permissions.Any())
        {
            permissions = AppPermissions.GetDefaultPermissionsForRole(input.Role);
        }

        string? customRoleName = null;
        if (!string.IsNullOrWhiteSpace(input.CustomRoleId))
        {
            var roleObj = await _roleRepo.GetByIdAsync(input.CustomRoleId, cancellationToken);
            if (roleObj != null)
            {
                customRoleName = roleObj.Name;
                if (input.Permissions == null || !input.Permissions.Any())
                {
                    permissions = roleObj.Permissions.ToList();
                }
            }
        }

        var staff = new User
        {
            FullName = input.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = input.PhoneNumber?.Trim(),
            Role = input.Role,
            CustomRoleId = input.CustomRoleId,
            CustomRoleName = customRoleName,
            Department = input.Department?.Trim() ?? GetDefaultDepartmentForRole(input.Role),
            PasswordHash = _passwordHasher.HashPassword(input.Password),
            Permissions = permissions,
            IsActive = true,
            IsEmailVerified = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(staff, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateStaffAccount",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Khởi tạo nhân viên mới [{staff.FullName}] ({staff.Email}) - Vai trò: {staff.Role} - Phòng ban: {staff.Department}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản nhân viên [{staff.FullName}] ({staff.Email})!";
        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStaff(
        [FromForm] UpdateStaffInputModel input,
        CancellationToken cancellationToken = default)
    {
        var staff = await _userRepo.GetByIdAsync(input.UserId, cancellationToken);
        if (staff == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        // Root superadmin safety
        if (staff.Email.Equals("admin@techzone.vn", StringComparison.OrdinalIgnoreCase) && input.Role != UserRole.SuperAdmin)
        {
            TempData["ErrorMessage"] = "Không thể hạ cấp vai trò của tài khoản Quản trị viên tối cao gốc (Root SuperAdmin).";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        staff.FullName = input.FullName.Trim();
        staff.PhoneNumber = input.PhoneNumber?.Trim();
        staff.Role = input.Role;
        staff.Department = input.Department?.Trim() ?? GetDefaultDepartmentForRole(input.Role);
        staff.CustomRoleId = input.CustomRoleId;
        staff.IsActive = input.IsActive;

        if (!string.IsNullOrWhiteSpace(input.CustomRoleId))
        {
            var roleObj = await _roleRepo.GetByIdAsync(input.CustomRoleId, cancellationToken);
            if (roleObj != null)
            {
                staff.CustomRoleName = roleObj.Name;
            }
        }
        else
        {
            staff.CustomRoleName = null;
        }

        // Update password if requested
        if (!string.IsNullOrWhiteSpace(input.NewPassword))
        {
            staff.PasswordHash = _passwordHasher.HashPassword(input.NewPassword);
            staff.SecurityStamp = Guid.NewGuid().ToString("N"); // Revoke existing sessions
        }

        if (input.Permissions != null && input.Permissions.Any())
        {
            staff.Permissions = input.Permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        }

        staff.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(staff, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UpdateStaffAccount",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Cập nhật tài khoản nhân sự [{staff.FullName}] ({staff.Email}) - Role: {staff.Role} - Dept: {staff.Department}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật hồ sơ nhân viên [{staff.FullName}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStaff(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var staff = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (staff == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        if (staff.Email.Equals("admin@techzone.vn", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Tuyệt đối không thể xóa tài khoản Quản trị viên tối cao gốc (admin@techzone.vn).";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        var currentEmail = User.Identity?.Name;
        if (staff.Email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản đang thực hiện phiên làm việc hiện tại.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        await _userRepo.DeleteAsync(staff.Id, softDelete: true, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DeleteStaffAccount",
            Module = "IAM",
            UserEmail = currentEmail ?? "SuperAdmin",
            Details = $"Xóa tài khoản nhân viên [{staff.FullName}] ({staff.Email}) khỏi hệ thống"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa tài khoản nhân viên [{staff.FullName}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    // ==============================================================
    // 1.6.2 ROLE MANAGEMENT (role.manage)
    // ==============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(
        [FromForm] CreateRoleInputModel input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Code))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập Tên vai trò và Mã định danh (Code).";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }

        var codeNormalized = input.Code.Trim().ToUpperInvariant();
        var allRoles = await _roleRepo.GetAllAsync(cancellationToken);
        if (allRoles.Any(r => r.Code.Equals(codeNormalized, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["ErrorMessage"] = $"Mã vai trò [{codeNormalized}] đã tồn tại trong công ty.";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }

        var permissions = input.Permissions?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList()
            ?? AppPermissions.GetDefaultPermissionsForRole(input.BaseRole);

        var role = new Role
        {
            Name = input.Name.Trim(),
            Code = codeNormalized,
            Description = input.Description?.Trim(),
            BaseRole = input.BaseRole,
            Department = input.Department?.Trim() ?? GetDefaultDepartmentForRole(input.BaseRole),
            Permissions = permissions,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        await _roleRepo.AddAsync(role, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "CreateRole",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Định nghĩa Role mới [{role.Name}] (Mã: {role.Code}) - Base: {role.BaseRole} - {role.Permissions.Count} quyền hạn"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã định nghĩa thành công vai trò mới [{role.Name}]!";
        return RedirectToAction(nameof(Index), new { tab = "roles" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(
        [FromForm] UpdateRoleInputModel input,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.GetByIdAsync(input.RoleId, cancellationToken);
        if (role == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy vai trò cần chỉnh sửa.";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }

        role.Name = input.Name.Trim();
        role.Description = input.Description?.Trim();
        role.Department = input.Department?.Trim();

        if (input.Permissions != null)
        {
            role.Permissions = input.Permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        }

        role.UpdatedAt = DateTime.UtcNow;
        await _roleRepo.UpdateAsync(role, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UpdateRole",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Cập nhật vai trò [{role.Name}] (Mã: {role.Code}) - {role.Permissions.Count} quyền hạn"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã cập nhật cấu hình vai trò [{role.Name}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "roles" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, cancellationToken);
        if (role == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy vai trò.";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }

        if (role.IsSystem)
        {
            TempData["ErrorMessage"] = $"Không thể xóa vai trò hệ thống mặc định [{role.Name}].";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }

        var allUsers = await _userRepo.GetAllAsync(cancellationToken);
        if (allUsers.Any(u => u.CustomRoleId == role.Id))
        {
            TempData["ErrorMessage"] = $"Không thể xóa vai trò [{role.Name}] vì đang có nhân viên được gán vai trò này. Vui lòng chuyển vai trò cho nhân viên trước.";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }

        await _roleRepo.DeleteAsync(role.Id, softDelete: true, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "DeleteRole",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Xóa vai trò nội bộ [{role.Name}] (Mã: {role.Code})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã xóa vai trò [{role.Name}] thành công!";
        return RedirectToAction(nameof(Index), new { tab = "roles" });
    }

    // ==============================================================
    // 1.6.3 PERMISSION ASSIGNMENT (permission.assign)
    // ==============================================================
    [HttpGet]
    public async Task<IActionResult> GetRolePermissions(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, cancellationToken);
        if (role == null)
        {
            return Json(new { success = false, message = "Không tìm thấy vai trò." });
        }

        return Json(new
        {
            success = true,
            roleId = role.Id,
            roleName = role.Name,
            roleCode = role.Code,
            permissions = role.Permissions
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserPermissions(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        }

        return Json(new
        {
            success = true,
            userId = user.Id,
            userName = user.FullName,
            role = user.Role.ToString(),
            permissions = user.Permissions
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermissions(
        [FromForm] AssignPermissionsInputModel input,
        CancellationToken cancellationToken = default)
    {
        var selectedPermissions = input.Permissions?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList()
            ?? new List<string>();

        if (input.TargetType.Equals("Role", StringComparison.OrdinalIgnoreCase))
        {
            var role = await _roleRepo.GetByIdAsync(input.TargetId, cancellationToken);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Role cần phân quyền.";
                return RedirectToAction(nameof(Index), new { tab = "roles" });
            }

            role.Permissions = selectedPermissions;
            role.UpdatedAt = DateTime.UtcNow;
            await _roleRepo.UpdateAsync(role, cancellationToken);

            await _auditLogRepo.AddAsync(new SecurityAuditLog
            {
                Action = "AssignRolePermissions",
                Module = "IAM",
                UserEmail = User.Identity?.Name ?? "SuperAdmin",
                Details = $"Gán {selectedPermissions.Count} quyền hạn cụ thể cho Role [{role.Name}]"
            }, cancellationToken);

            TempData["SuccessMessage"] = $"Đã cập nhật {selectedPermissions.Count} quyền hạn cho Role [{role.Name}]!";
            return RedirectToAction(nameof(Index), new { tab = "roles" });
        }
        else // TargetType == "User"
        {
            var user = await _userRepo.GetByIdAsync(input.TargetId, cancellationToken);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần phân quyền.";
                return RedirectToAction(nameof(Index), new { tab = "staff" });
            }

            user.Permissions = selectedPermissions;
            user.SecurityStamp = Guid.NewGuid().ToString("N"); // Refresh credentials
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user, cancellationToken);

            await _auditLogRepo.AddAsync(new SecurityAuditLog
            {
                Action = "AssignUserPermissions",
                Module = "IAM",
                UserEmail = User.Identity?.Name ?? "SuperAdmin",
                Details = $"Gán {selectedPermissions.Count} quyền hạn đặc thù cho nhân viên [{user.FullName}] ({user.Email})"
            }, cancellationToken);

            TempData["SuccessMessage"] = $"Đã cập nhật {selectedPermissions.Count} quyền hạn đặc thù cho nhân viên [{user.FullName}]!";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }
    }

    // ==============================================================
    // 1.6.4 SESSION REVOKE & FORCE LOGOUT (session.revoke)
    // ==============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest();
        }

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "RevokeSession",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Thu hồi phiên đăng nhập toàn bộ thiết bị (Force Logout) của tài khoản [{user.FullName}] ({user.Email})"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã thu hồi phiên đăng nhập (Force Logout) của [{user.FullName}]. Nhân viên sẽ bị buộc đăng xuất ngay lập tức trên tất cả thiết bị!";
        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest();
        }

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        if (user.Email.Equals("admin@techzone.vn", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Không thể vô hiệu hóa tài khoản Quản trị viên tối cao gốc.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        user.IsActive = !user.IsActive;
        if (!user.IsActive)
        {
            user.LockoutEnd = DateTime.UtcNow.AddYears(1);
        }
        else
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "ToggleUserStatus",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Thay đổi trạng thái tài khoản [{user.FullName}] thành {(user.IsActive ? "Hoạt động" : "Bị khóa")}"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã {(user.IsActive ? "mở khóa hoạt động" : "khóa truy cập (Ban)")} tài khoản '{user.FullName}'.";
        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(
        string userId,
        UserRole newRole,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest();
        }

        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        if (user.Email.Equals("admin@techzone.vn", StringComparison.OrdinalIgnoreCase) && newRole != UserRole.SuperAdmin)
        {
            TempData["ErrorMessage"] = "Không thể thay đổi vai trò của tài khoản Quản trị viên tối cao (Root SuperAdmin).";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        var oldRole = user.Role;
        user.Role = newRole;
        user.Department = GetDefaultDepartmentForRole(newRole);
        user.CustomRoleId = null;
        user.CustomRoleName = null;
        user.Permissions = AppPermissions.GetDefaultPermissionsForRole(newRole);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, cancellationToken);

        await _auditLogRepo.AddAsync(new SecurityAuditLog
        {
            Action = "UpdateUserRole",
            Module = "IAM",
            UserEmail = User.Identity?.Name ?? "SuperAdmin",
            Details = $"Chuyển đổi vai trò của [{user.FullName}] từ [{oldRole}] sang [{newRole}]"
        }, cancellationToken);

        TempData["SuccessMessage"] = $"Đã chuyển đổi vai trò của '{user.FullName}' từ '{oldRole}' sang '{newRole}' thành công.";
        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    private static string GetDefaultDepartmentForRole(UserRole role) => role switch
    {
        UserRole.SuperAdmin => "Ban Quản Trị",
        UserRole.SalesStaff => "Kinh Doanh & CSKH",
        UserRole.WarehouseStaff => "Kho Vận & Quản Lý IMEI",
        UserRole.Technician => "Kỹ Thuật & Bảo Hành",
        UserRole.Accountant => "Tài Chính & Kế Toán",
        UserRole.MarketingStaff => "Phòng Marketing & SEO",
        _ => "Khách Hàng"
    };

    private async Task SeedDefaultRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new List<Role>
        {
            new()
            {
                Name = "Quản Trị Viên Tối Cao (SuperAdmin)",
                Code = "SUPER_ADMIN",
                Description = "Nắm toàn bộ quyền hạn cao nhất trong hệ thống, cấu hình tham số bảo mật, hạ tầng thanh toán và kiểm toán.",
                BaseRole = UserRole.SuperAdmin,
                IsSystem = true,
                Department = "Ban Quản Trị",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.SuperAdmin)
            },
            new()
            {
                Name = "Nhân Viên Bán Hàng & CSKH (Sales Staff)",
                Code = "SALES_STAFF",
                Description = "Quản lý đơn hàng, tạo đơn thủ công hotline/fanpage, hỗ trợ địa chỉ giao hàng và mã ưu đãi khuyến mãi.",
                BaseRole = UserRole.SalesStaff,
                IsSystem = true,
                Department = "Kinh Doanh",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.SalesStaff)
            },
            new()
            {
                Name = "Thủ Kho & Quản Lý Thiết Bị (Warehouse Staff)",
                Code = "WAREHOUSE_STAFF",
                Description = "Kiểm soát số lượng tồn kho vật lý, tạo phiếu nhập/xuất kho (GRN/GDN), quản lý mã IMEI/Serial từng máy.",
                BaseRole = UserRole.WarehouseStaff,
                IsSystem = true,
                Department = "Kho Vận",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.WarehouseStaff)
            },
            new()
            {
                Name = "Kỹ Thuật Viên Bảo Hành & Sửa Chữa (Technician)",
                Code = "TECHNICIAN",
                Description = "Tiếp nhận phiếu RMA sửa chữa, thẩm định tình trạng máy theo IMEI, thay thế linh kiện và cấu hình chính sách bảo hành.",
                BaseRole = UserRole.Technician,
                IsSystem = true,
                Department = "Kỹ Thuật",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.Technician)
            },
            new()
            {
                Name = "Kế Toán & Quản Trị Tài Chính (Accountant)",
                Code = "ACCOUNTANT",
                Description = "Kiểm soát công nợ nhà cung cấp, phê duyệt lệnh hoàn tiền đơn đổi trả, xem báo cáo doanh thu & lãi gộp.",
                BaseRole = UserRole.Accountant,
                IsSystem = true,
                Department = "Kế Toán",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.Accountant)
            },
            new()
            {
                Name = "Chuyên Viên Tiếp Thị & SEO (Marketing Staff)",
                Code = "MARKETING_STAFF",
                Description = "Thiết lập mã giảm giá voucher, chiến dịch Flash Sale giờ vàng, banner trang chủ và thẻ meta SEO landing page.",
                BaseRole = UserRole.MarketingStaff,
                IsSystem = true,
                Department = "Marketing",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.MarketingStaff)
            },
            new()
            {
                Name = "Trưởng Nhóm Telesales & CSKH Cao Cấp",
                Code = "CS_LEAD",
                Description = "Role tùy biến mở rộng: Giám sát toàn bộ đơn hàng, phê duyệt chiết khấu đặc biệt cho khách hàng VIP.",
                BaseRole = UserRole.SalesStaff,
                IsSystem = false,
                Department = "Kinh Doanh",
                Permissions = new List<string>
                {
                    AppPermissions.CategoryView, AppPermissions.ProductView, AppPermissions.ReviewManage,
                    AppPermissions.OrderViewAll, AppPermissions.OrderCreateManual, AppPermissions.OrderUpdateStatus, AppPermissions.OrderCancel,
                    AppPermissions.CustomerView, AppPermissions.CustomerUpdate, AppPermissions.CustomerAddressManage,
                    AppPermissions.CouponManage, AppPermissions.BannerSeoManage, AppPermissions.WarrantyLookup
                }
            }
        };

        foreach (var r in roles)
        {
            await _roleRepo.AddAsync(r, cancellationToken);
        }
    }
}
