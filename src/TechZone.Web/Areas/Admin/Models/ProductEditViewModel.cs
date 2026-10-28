using System.ComponentModel.DataAnnotations;
using TechZone.Domain.Entities;

namespace TechZone.Web.Areas.Admin.Models;

public class ProductEditViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thiết bị/sản phẩm")]
    [MaxLength(250, ErrorMessage = "Tên sản phẩm không vượt quá 250 ký tự")]
    [Display(Name = "Tên thiết bị / sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Đường dẫn SEO (Slug)")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã SKU/Model sản phẩm")]
    [Display(Name = "Mã SKU / Model")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm")]
    [Display(Name = "Danh mục")]
    public string CategoryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hãng sản xuất")]
    [Display(Name = "Thương hiệu / Hãng SX")]
    public string BrandId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập giá niêm yết")]
    [Range(0, 1000000000, ErrorMessage = "Giá niêm yết không hợp lệ")]
    [Display(Name = "Giá niêm yết (Hãng)")]
    public decimal OriginalPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá bán lẻ")]
    [Range(0, 1000000000, ErrorMessage = "Giá bán lẻ không hợp lệ")]
    [Display(Name = "Giá bán lẻ")]
    public decimal Price { get; set; }

    [Display(Name = "Giá bán sỉ / Bán buôn (B2B)")]
    [Range(0, 1000000000, ErrorMessage = "Giá bán buôn không hợp lệ")]
    public decimal? WholesalePrice { get; set; }

    [Display(Name = "Thời hạn bảo hành (tháng)")]
    [Range(0, 120, ErrorMessage = "Thời hạn bảo hành từ 0 đến 120 tháng")]
    public int WarrantyMonths { get; set; } = 12;

    [Display(Name = "Tồn kho ban đầu")]
    [Range(0, 100000, ErrorMessage = "Số lượng tồn kho từ 0 đến 100.000")]
    public int StockQuantity { get; set; } = 10;

    [Display(Name = "Mô tả ngắn gọn (Tóm tắt)")]
    [MaxLength(500, ErrorMessage = "Mô tả ngắn không vượt quá 500 ký tự")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Bài viết mô tả chi tiết & Đánh giá chuyên sâu")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đường dẫn ảnh đại diện chính (Thumbnail)")]
    [Display(Name = "Ảnh đại diện chính")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [Display(Name = "Video mở hộp (Unbox) / Review (YouTube/MP4)")]
    public string? VideoUrl { get; set; }

    [Display(Name = "Thông số nổi bật (phân cách bằng dấu phẩy)")]
    public string? HighlightSpecsRaw { get; set; }

    [Display(Name = "Thẻ tìm kiếm / Tags (phân cách bằng dấu phẩy)")]
    public string? TagsRaw { get; set; }

    [Display(Name = "Mở bán trên Storefront")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sản phẩm nổi bật (Featured)")]
    public bool IsFeatured { get; set; } = false;

    [Display(Name = "Hàng mới về (New Arrival)")]
    public bool IsNewArrival { get; set; } = true;

    [Display(Name = "Bán chạy (Best Seller)")]
    public bool IsBestSeller { get; set; } = false;

    // Gallery images JSON or list
    public List<ProductImageItemDto> GalleryImages { get; set; } = new();
    public string? GalleryImagesJson { get; set; }

    // Dropdown lookups
    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();
    public IReadOnlyList<Brand> Brands { get; set; } = Array.Empty<Brand>();
}

public class ProductImageItemDto
{
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public string? ColorTag { get; set; }
}

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = new();
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public IReadOnlyList<Product> RelatedProducts { get; set; } = Array.Empty<Product>();
}
