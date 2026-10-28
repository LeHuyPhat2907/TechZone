namespace TechZone.Domain.Enums;

public enum CouponScope
{
    Order = 0,      // Giảm giá trực tiếp trên giá trị đơn hàng
    Shipping = 1    // Miễn phí hoặc giảm phí vận chuyển
}

public enum DiscountType
{
    Percentage = 0, // Giảm theo % (có trần tối đa MaxDiscountAmount)
    FixedAmount = 1 // Giảm số tiền cố định (VD: 50.000đ, 200.000đ)
}
