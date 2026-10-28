using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class Role : BaseEntity, IAggregateRoot
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("baseRole")]
    public UserRole BaseRole { get; set; } = UserRole.SalesStaff;

    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = new();

    [BsonElement("isSystem")]
    public bool IsSystem { get; set; } = false;

    [BsonElement("department")]
    public string? Department { get; set; }

    [BsonIgnore]
    public int UserCount { get; set; } = 0;
}
