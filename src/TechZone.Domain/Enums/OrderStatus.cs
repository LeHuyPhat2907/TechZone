namespace TechZone.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,            // Chờ xử lý / Chờ thanh toán
    PaymentConfirmed = 1,   // Đã thanh toán trực tuyến
    Confirmed = 2,          // Đã xác nhận đơn hàng
    Preparing = 3,          // Đang chuẩn bị hàng (Scan Serial/IMEI)
    Shipping = 4,           // Đang giao hàng (đã bàn giao bưu tá)
    Delivered = 5,          // Giao thành công (kích hoạt bảo hành điện tử)
    Cancelled = 6,          // Đã hủy đơn
    Returned = 7,           // Đã trả hàng / hoàn tiền
    DeliveryFailed = 8      // Giao hàng không thành công
}
