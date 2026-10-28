namespace TechZone.Domain.Enums;

public enum PaymentMethod
{
    COD = 0,          // Thanh toán khi nhận hàng
    PayOS = 1,        // Cổng VietQR qua PayOS
    VnPay = 2,        // Cổng VNPAY-QR (Ưu tiên nhất tại Việt Nam)
    MoMo = 3,         // Ví điện tử MoMo
    ZaloPay = 4,      // Ví điện tử ZaloPay
    BankTransfer = 5  // Chuyển khoản ngân hàng trực tiếp
}
