using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Web.Areas.Admin.Models;

public class CustomerItemDto
{
    public User User { get; set; } = null!;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public string MembershipTier => User.MembershipTier;
    public string MembershipTierName => User.MembershipTierName;
}

public class CustomerManagementViewModel
{
    public List<CustomerItemDto> Customers { get; set; } = new();

    // KPIs
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int BannedCustomers { get; set; }
    public int DiamondCustomers { get; set; }
    public int GoldCustomers { get; set; }
    public int SilverCustomers { get; set; }
    public int BronzeCustomers { get; set; }
    public int TotalRewardPoints { get; set; }

    // Filters & Pagination
    public string? SearchQuery { get; set; }
    public string? TierFilter { get; set; } = "All"; // All, Diamond, Gold, Silver, Bronze
    public string? StatusFilter { get; set; } = "All"; // All, Active, Banned
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalFilteredCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalFilteredCount / PageSize);
}

public class UpdateCustomerInputModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AdminNotes { get; set; }
}

public class CustomerBanInputModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class CustomerAddressInputModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string? AddressId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;
    public string AddressType { get; set; } = "Home"; // Home, Office, Other
    public bool IsDefault { get; set; } = false;
}

public class AdjustCustomerPointsInputModel
{
    public string CustomerId { get; set; } = string.Empty;
    public int PointsDelta { get; set; }
    public string? Reason { get; set; }
}
