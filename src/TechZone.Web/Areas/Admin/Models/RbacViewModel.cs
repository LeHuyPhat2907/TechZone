using TechZone.Domain.Common;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class RbacViewModel
{
    public IReadOnlyList<User> Users { get; set; } = Array.Empty<User>();
    public IReadOnlyList<User> StaffMembers { get; set; } = Array.Empty<User>();
    public IReadOnlyList<Role> Roles { get; set; } = Array.Empty<Role>();
    public IReadOnlyList<PermissionCategoryGroup> PermissionGroups { get; set; } = AppPermissions.AllGroups;

    // Counts
    public int SuperAdminCount => Users.Count(u => u.Role == UserRole.SuperAdmin);
    public int SalesStaffCount => Users.Count(u => u.Role == UserRole.SalesStaff);
    public int WarehouseStaffCount => Users.Count(u => u.Role == UserRole.WarehouseStaff);
    public int TechnicianCount => Users.Count(u => u.Role == UserRole.Technician);
    public int AccountantCount => Users.Count(u => u.Role == UserRole.Accountant);
    public int MarketingStaffCount => Users.Count(u => u.Role == UserRole.MarketingStaff);
    public int CustomerCount => Users.Count(u => u.Role == UserRole.Customer);
    public int TotalStaffCount => Users.Count(u => u.Role != UserRole.Customer);
    public int TotalUsersCount => Users.Count;
    public int TotalRolesCount => Roles.Count;

    // Filter
    public string ActiveTab { get; set; } = "staff"; // "staff", "roles", "matrix", "guardrails"
    public string? SearchQuery { get; set; }
    public string? RoleFilter { get; set; } = "All";
}

public class CreateStaffInputModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.SalesStaff;
    public string? CustomRoleId { get; set; }
    public string? Department { get; set; }
    public string Password { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}

public class UpdateStaffInputModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.SalesStaff;
    public string? CustomRoleId { get; set; }
    public string? Department { get; set; }
    public string? NewPassword { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Permissions { get; set; } = new();
}

public class CreateRoleInputModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UserRole BaseRole { get; set; } = UserRole.SalesStaff;
    public string? Department { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class UpdateRoleInputModel
{
    public string RoleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Department { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class AssignPermissionsInputModel
{
    public string TargetType { get; set; } = "User"; // "User" or "Role"
    public string TargetId { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
