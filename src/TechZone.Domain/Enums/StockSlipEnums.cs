namespace TechZone.Domain.Enums;

public enum StockSlipType
{
    Import = 0, // Phiếu nhập kho từ nhà phân phối (GRN)
    Export = 1  // Phiếu xuất kho (GDN: Xuất trả NCC, Xuất thanh lý, Hủy hàng)
}

public enum StockSlipStatus
{
    Draft = 0,     // Phiếu nháp chờ duyệt
    Approved = 1,  // Đã duyệt & Cập nhật tồn kho
    Cancelled = 2  // Đã hủy phiếu
}

public enum StockExportReason
{
    ReturnToSupplier = 0, // Xuất trả nhà cung cấp do lỗi NSX
    Liquidation = 1,      // Xuất thanh lý hàng cũ / hàng trưng bày
    Disposal = 2,         // Xuất hủy hàng hư hỏng nặng
    Transfer = 3          // Xuất điều chuyển chi nhánh kho
}
