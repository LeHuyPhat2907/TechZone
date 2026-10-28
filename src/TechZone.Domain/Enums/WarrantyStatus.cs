namespace TechZone.Domain.Enums;

public enum WarrantyStatus
{
    Active = 0,         // Còn hiệu lực bảo hành
    ExpiringSoon = 1,   // Sắp hết hạn (dưới 30 ngày)
    Expired = 2,        // Đã hết hạn bảo hành
    Voided = 3          // Bị từ chối bảo hành (vào nước, rơi vỡ, can thiệp trái phép)
}
