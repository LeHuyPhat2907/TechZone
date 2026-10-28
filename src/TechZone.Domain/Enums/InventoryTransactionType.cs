namespace TechZone.Domain.Enums;

public enum InventoryTransactionType
{
    InboundImport = 0,      // Nhập kho từ nhà cung cấp
    OutboundSale = 1,       // Xuất kho bán hàng theo đơn
    CustomerReturn = 2,     // Nhập lại do khách trả hàng / hủy đơn
    DefectiveReturn = 3,    // Xuất trả lại nhà phân phối do lỗi NSX
    StockAdjustment = 4,    // Kiểm kê điều chỉnh kho (thừa/thiếu)
    WarrantyExchange = 5    // Xuất kho máy mới để 1 đổi 1 bảo hành
}
