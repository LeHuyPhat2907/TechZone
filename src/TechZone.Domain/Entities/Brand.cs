using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

public class Brand : BaseEntity, IAggregateRoot
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("logoUrl")]
    public string? LogoUrl { get; set; }

    [BsonElement("bannerUrl")]
    public string? BannerUrl { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [BsonElement("countryOfOrigin")]
    public string? CountryOfOrigin { get; set; }

    [BsonElement("categoryIds")]
    public List<string> CategoryIds { get; set; } = new();

    [BsonElement("isFeatured")]
    public bool IsFeatured { get; set; } = false;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; } = 0;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
