namespace TechZone.Domain.Enums;

public enum InventoryStatus
{
    InStock = 0,            // Trong kho, sẵn sàng xuất bán
    Reserved = 1,           // Đã giữ chỗ đơn hàng (chưa xuất)
    AssignedToOrder = 2,    // Đã quét Serial/IMEI gắn vào vận đơn
    Shipped = 3,            // Đang trên đường vận chuyển
    Delivered = 4,          // Đã giao cho khách hàng
    Defective = 5,          // Hàng lỗi nhà sản xuất (DOA)
    Returned = 6,           // Khách trả hàng (về kho thanh lý / like new)
    InRepair = 7,           // Đang trong trung tâm bảo hành sửa chữa
    InTransit = 8           // Đang điều chuyển giữa các chi nhánh kho
}
