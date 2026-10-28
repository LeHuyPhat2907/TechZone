using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

public class Category : BaseEntity, IAggregateRoot
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("parentId")]
    public string? ParentId { get; set; }

    [BsonElement("level")]
    public int Level { get; set; } = 1; // 1: Cấp gốc (Laptop, Điện thoại), 2: Cấp phụ (Gaming, Văn phòng)

    [BsonElement("ancestors")]
    public List<CategoryAncestor> Ancestors { get; set; } = new();

    [BsonElement("iconUrl")]
    public string? IconUrl { get; set; }

    [BsonElement("imageUrl")]
    public string? ImageUrl { get; set; }

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; } = 0;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("productCount")]
    public int ProductCount { get; set; } = 0;
}

public class CategoryAncestor
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;
}
