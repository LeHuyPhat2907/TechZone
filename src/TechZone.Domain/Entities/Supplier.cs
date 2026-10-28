using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class Supplier : BaseEntity, IAggregateRoot
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty; // Tên nhà cung cấp / đối tác phân phối

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty; // Mã nhà cung cấp (VD: NCC-APL, NCC-DGW)

    [BsonElement("contactPerson")]
    public string? ContactPerson { get; set; } // Người liên hệ đại diện

    [BsonElement("phone")]
    public string? Phone { get; set; } // Số điện thoại

    [BsonElement("email")]
    public string? Email { get; set; } // Email liên hệ

    [BsonElement("address")]
    public string? Address { get; set; } // Địa chỉ kho / trụ sở

    [BsonElement("taxCode")]
    public string? TaxCode { get; set; } // Mã số thuế (MST)

    [BsonElement("paymentTerms")]
    public string PaymentTerms { get; set; } = "Net 30 ngày"; // Điều khoản thanh toán (Net 30, Net 45, COD B2B...)

    [BsonElement("totalDebt")]
    public decimal TotalDebt { get; set; } = 0; // Công nợ hiện tại phải trả (VNĐ)

    [BsonElement("totalImportAmount")]
    public decimal TotalImportAmount { get; set; } = 0; // Tổng giá trị hàng đã nhập lũy kế

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true; // Trạng thái hợp tác

    [BsonElement("note")]
    public string? Note { get; set; } // Ghi chú hợp tác
}
