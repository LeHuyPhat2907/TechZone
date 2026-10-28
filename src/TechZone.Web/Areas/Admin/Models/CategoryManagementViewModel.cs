using TechZone.Domain.Entities;

namespace TechZone.Web.Areas.Admin.Models;

public class CategoryManagementViewModel
{
    public List<CategoryItemDto> Categories { get; set; } = new();
    public List<Category> ParentCategories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();

    // KPIs
    public int TotalCategories { get; set; }
    public int RootCategoriesCount { get; set; }
    public int SubCategoriesCount { get; set; }
    public int ActiveCategoriesCount { get; set; }
    public int TotalBrands { get; set; }
    public int TotalProductsAcrossCategories { get; set; }

    // Filter & Tab
    public string? SearchQuery { get; set; }
    public string ActiveTab { get; set; } = "categories"; // "categories" or "brands"
}

public class CategoryItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; } = 1;
    public string? IconUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ParentId { get; set; }
    public string? IconUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryInputModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ParentId { get; set; }
    public string? IconUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class CreateBrandInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? WebsiteUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
}
