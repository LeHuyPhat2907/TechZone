using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class WarrantyPolicy : BaseEntity, IAggregateRoot
{
    [BsonElement("policyCode")]
    public string PolicyCode { get; set; } = string.Empty; // e.g. POL-PHONE, POL-LAPTOP, POL-PARTS, POL-MONITOR

    [BsonElement("policyName")]
    public string PolicyName { get; set; } = string.Empty;

    [BsonElement("categoryGroup")]
    public string CategoryGroup { get; set; } = string.Empty; // Điện thoại, Laptop, Linh kiện PC, Màn hình, Phụ kiện, Tablet

    [BsonElement("standardWarrantyMonths")]
    public int StandardWarrantyMonths { get; set; } = 12;

    [BsonElement("doaPeriodDays")]
    public int DoaPeriodDays { get; set; } = 30; // 30 ngày 1 đổi 1 nếu lỗi NSX

    [BsonElement("screenDeadPixelThreshold")]
    public int ScreenDeadPixelThreshold { get; set; } = 3; // Số điểm chết đổi mới màn hình

    [BsonElement("batteryHealthThreshold")]
    public int BatteryHealthThreshold { get; set; } = 80; // Dung lượng pin dưới 80% được thay miễn phí trong hạn

    [BsonElement("isReturnExchangeSupported")]
    public bool IsReturnExchangeSupported { get; set; } = true;

    [BsonElement("extendedWarrantySupported")]
    public bool ExtendedWarrantySupported { get; set; } = true; // TechZone Care+

    [BsonElement("termsAndConditions")]
    public string TermsAndConditions { get; set; } = string.Empty; // Điều kiện bảo hành hợp lệ

    [BsonElement("exclusionClauses")]
    public string ExclusionClauses { get; set; } = string.Empty; // Điều khoản loại trừ / Từ chối bảo hành

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; } = 0;

    [BsonElement("notes")]
    public string? Notes { get; set; }
}
