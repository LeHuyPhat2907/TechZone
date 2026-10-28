using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

public class Post : BaseEntity, IAggregateRoot
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [BsonElement("authorId")]
    public string AuthorId { get; set; } = string.Empty;

    [BsonElement("authorName")]
    public string AuthorName { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [BsonElement("viewCount")]
    public int ViewCount { get; set; } = 0;

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;

    [BsonElement("publishedAt")]
    public DateTime? PublishedAt { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("relatedProductIds")]
    public List<string> RelatedProductIds { get; set; } = new(); // Content-to-commerce integration
}

public class PostCategory : BaseEntity, IAggregateRoot
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; } = 0;
}
