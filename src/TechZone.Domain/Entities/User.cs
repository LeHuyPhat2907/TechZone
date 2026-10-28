using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class User : BaseEntity, IAggregateRoot
{
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("role")]
    public UserRole Role { get; set; } = UserRole.Customer;

    [BsonElement("customRoleId")]
    public string? CustomRoleId { get; set; }

    [BsonElement("customRoleName")]
    public string? CustomRoleName { get; set; }

    [BsonElement("department")]
    public string? Department { get; set; }

    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isEmailVerified")]
    public bool IsEmailVerified { get; set; } = false;

    [BsonElement("emailVerificationToken")]
    public string? EmailVerificationToken { get; set; }

    [BsonElement("emailVerificationExpiresAt")]
    public DateTime? EmailVerificationExpiresAt { get; set; }

    [BsonElement("passwordResetToken")]
    public string? PasswordResetToken { get; set; }

    [BsonElement("passwordResetExpiresAt")]
    public DateTime? PasswordResetExpiresAt { get; set; }

    [BsonElement("securityStamp")]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("rewardPoints")]
    public int RewardPoints { get; set; } = 0;

    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [BsonElement("twoFactorEnabled")]
    public bool TwoFactorEnabled { get; set; } = false;

    [BsonElement("twoFactorMethod")]
    public string TwoFactorMethod { get; set; } = "Email"; // Email, SMS

    [BsonElement("wishlistProductIds")]
    public List<string> WishlistProductIds { get; set; } = new();

    [BsonElement("savedCouponCodes")]
    public List<string> SavedCouponCodes { get; set; } = new();

    [BsonElement("deleteRequestedAt")]
    public DateTime? DeleteRequestedAt { get; set; }

    [BsonElement("deleteRequestReason")]
    public string? DeleteRequestReason { get; set; }

    [BsonElement("addresses")]
    public List<UserAddress> Addresses { get; set; } = new();

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("accessFailedCount")]
    public int AccessFailedCount { get; set; } = 0;

    [BsonElement("lockoutEnd")]
    public DateTime? LockoutEnd { get; set; }

    [BsonElement("bannedReason")]
    public string? BannedReason { get; set; }

    [BsonElement("bannedAt")]
    public DateTime? BannedAt { get; set; }

    [BsonElement("bannedBy")]
    public string? BannedBy { get; set; }

    [BsonElement("adminNotes")]
    public string? AdminNotes { get; set; }

    [BsonIgnore]
    public string MembershipTier => RewardPoints switch
    {
        >= 5000 => "Diamond",
        >= 2000 => "Gold",
        >= 500 => "Silver",
        _ => "Bronze"
    };

    [BsonIgnore]
    public string MembershipTierName => RewardPoints switch
    {
        >= 5000 => "Kim cương (Diamond)",
        >= 2000 => "Vàng VIP (Gold)",
        >= 500 => "Bạc (Silver)",
        _ => "Đồng (Bronze)"
    };
}

[BsonIgnoreExtraElements]
public class UserAddress
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("receiverName")]
    public string ReceiverName { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("province")]
    public string Province { get; set; } = string.Empty;

    [BsonElement("district")]
    public string District { get; set; } = string.Empty;

    [BsonElement("ward")]
    public string Ward { get; set; } = string.Empty;

    [BsonElement("detailAddress")]
    public string DetailAddress { get; set; } = string.Empty;

    [BsonElement("isDefault")]
    public bool IsDefault { get; set; } = false;

    [BsonElement("addressType")]
    public string AddressType { get; set; } = "Home"; // Home, Office

    [BsonIgnore]
    public string FullAddress => $"{DetailAddress}, {Ward}, {District}, {Province}".Trim(',', ' ');
}
