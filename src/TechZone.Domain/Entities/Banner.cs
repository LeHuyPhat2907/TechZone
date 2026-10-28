using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

public class Banner : BaseEntity, IAggregateRoot
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("mobileImageUrl")]
    public string? MobileImageUrl { get; set; }

    [BsonElement("linkUrl")]
    public string? LinkUrl { get; set; }

    [BsonElement("position")]
    public BannerPosition Position { get; set; } = BannerPosition.HomeMainSlider;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; } = 0;

    [BsonElement("startDate")]
    public DateTime? StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("clickCount")]
    public int ClickCount { get; set; } = 0;

    [BsonElement("metaTitle")]
    public string? MetaTitle { get; set; }

    [BsonElement("metaDescription")]
    public string? MetaDescription { get; set; }

    [BsonElement("metaKeywords")]
    public string? MetaKeywords { get; set; }

    [BsonElement("canonicalUrl")]
    public string? CanonicalUrl { get; set; }

    [BsonElement("ogImageUrl")]
    public string? OgImageUrl { get; set; }

    [BsonElement("eventSlug")]
    public string? EventSlug { get; set; }

    [BsonElement("htmlContent")]
    public string? HtmlContent { get; set; }
}
