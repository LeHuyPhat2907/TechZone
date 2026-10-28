using TechZone.Domain.Entities;

namespace TechZone.Web.Areas.Admin.Models;

public class AttributesViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public Product? SelectedProduct { get; set; }
    public string? SelectedProductId { get; set; }
    public string? SearchKeyword { get; set; }
    public string ActiveTab { get; set; } = "product_specs"; // "product_specs" or "templates"

    // KPIs
    public int TotalProductsWithSpecs { get; set; }
    public int TotalProductsWithoutSpecs { get; set; }
    public int TotalStandardTemplates { get; set; }

    // Predefined / Configured Category Attribute Templates
    public List<CategorySpecTemplate> PredefinedTemplates { get; set; } = new();
}

public class CategorySpecTemplate
{
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-folder";
    public string Description { get; set; } = string.Empty;
    public List<SpecificationGroup> Groups { get; set; } = new();
    public List<string> SuggestedHighlightSpecs { get; set; } = new();
}

public class SaveProductSpecsInputModel
{
    public string ProductId { get; set; } = string.Empty;
    public string SpecsJson { get; set; } = string.Empty; // Serialized SpecificationGroup list
    public string? HighlightSpecsText { get; set; } // Comma or newline separated highlight pills
}

public class QuickAddSpecItemInputModel
{
    public string ProductId { get; set; } = string.Empty;
    public string GroupName { get; set; } = "Thông số chung";
    public string SpecName { get; set; } = string.Empty; // RAM, CPU, Pin...
    public string SpecValue { get; set; } = string.Empty;
    public bool IsHighlight { get; set; } = false;
}
