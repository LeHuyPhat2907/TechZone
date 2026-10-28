using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class ShippingCarrierConfig : BaseEntity, IAggregateRoot
{
    [BsonElement("carrierCode")]
    public string CarrierCode { get; set; } = string.Empty; // GHN, GHTK, VIETTELPOST, GRABEXPRESS

    [BsonElement("carrierName")]
    public string CarrierName { get; set; } = string.Empty;

    [BsonElement("logoUrl")]
    public string LogoUrl { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isSandbox")]
    public bool IsSandbox { get; set; } = true;

    [BsonElement("apiEndpoint")]
    public string ApiEndpoint { get; set; } = string.Empty;

    [BsonElement("apiToken")]
    public string ApiToken { get; set; } = string.Empty;

    [BsonElement("shopIdOrPartnerCode")]
    public string? ShopIdOrPartnerCode { get; set; }

    [BsonElement("baseShippingFee")]
    public decimal BaseShippingFee { get; set; } = 30000m;

    [BsonElement("freeShippingThreshold")]
    public decimal FreeShippingThreshold { get; set; } = 500000m;

    [BsonElement("estimatedDeliveryTime")]
    public string EstimatedDeliveryTime { get; set; } = "1-3 ngày làm việc";

    [BsonElement("supportSameDayDelivery")]
    public bool SupportSameDayDelivery { get; set; } = false;

    [BsonElement("trackingUrlTemplate")]
    public string? TrackingUrlTemplate { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }
}
