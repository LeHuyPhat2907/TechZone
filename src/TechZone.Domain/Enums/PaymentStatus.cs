namespace TechZone.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,    // Đang chờ thanh toán
    Completed = 1,  // Thanh toán thành công
    Failed = 2,     // Thanh toán thất bại
    Refunded = 3    // Đã hoàn tiền
}
