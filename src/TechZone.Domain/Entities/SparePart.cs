using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class SparePart : BaseEntity, IAggregateRoot
{
    [BsonElement("partCode")]
    public string PartCode { get; set; } = string.Empty; // e.g. PART-SCR-15PM, PART-BAT-M3

    [BsonElement("partName")]
    public string PartName { get; set; } = string.Empty; // e.g. Màn hình OLED Super Retina XDR

    [BsonElement("category")]
    public string Category { get; set; } = "Màn hình"; // Màn hình, Pin, Bo mạch chủ, Cụm sạc, Camera, Quạt & Tản nhiệt, Bàn phím, Vỏ máy

    [BsonElement("compatibleModels")]
    public List<string> CompatibleModels { get; set; } = new();

    [BsonElement("warehouseLocation")]
    public string WarehouseLocation { get; set; } = "Tủ Kỹ Thuật T1-Kệ B";

    [BsonElement("stockQuantity")]
    public int StockQuantity { get; set; } = 0; // Tồn khả dụng trong kho kỹ thuật

    [BsonElement("defectiveQuantity")]
    public int DefectiveQuantity { get; set; } = 0; // Linh kiện hỏng cũ thu hồi chờ trả hãng/tiêu hủy

    [BsonElement("minStockAlert")]
    public int MinStockAlert { get; set; } = 2; // Cảnh báo sắp hết hàng

    [BsonElement("costPrice")]
    public decimal CostPrice { get; set; } // Giá vốn nhập kho nội bộ

    [BsonElement("servicePrice")]
    public decimal ServicePrice { get; set; } // Giá niêm yết thay thế cho khách tính phí

    [BsonElement("warrantyMonths")]
    public int WarrantyMonths { get; set; } = 6; // Hạn bảo hành linh kiện sau sửa chữa

    [BsonElement("supplierName")]
    public string? SupplierName { get; set; } = "TTBH Hãng";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
