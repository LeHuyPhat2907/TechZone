namespace TechZone.Application.DTOs.Catalog;

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public int ProductCount { get; set; }
}

public class BrandDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? Description { get; set; }
    public bool IsFeatured { get; set; }
}
