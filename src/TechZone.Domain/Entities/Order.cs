using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

[BsonIgnoreExtraElements]
public class Order : BaseEntity, IAggregateRoot
{
    [BsonElement("orderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonIgnore]
    public bool IsGuestOrder => string.IsNullOrEmpty(UserId);

    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [BsonElement("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [BsonElement("shippingAddress")]
    public OrderShippingAddress ShippingAddress { get; set; } = new();

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new();

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("shippingFee")]
    public decimal ShippingFee { get; set; }

    [BsonElement("orderDiscountAmount")]
    public decimal OrderDiscountAmount { get; set; }

    [BsonElement("shippingDiscountAmount")]
    public decimal ShippingDiscountAmount { get; set; }

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

    [BsonElement("paymentStatus")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [BsonElement("orderStatus")]
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

    [BsonElement("appliedOrderCouponCode")]
    public string? AppliedOrderCouponCode { get; set; }

    [BsonElement("appliedShippingCouponCode")]
    public string? AppliedShippingCouponCode { get; set; }

    [BsonElement("carrierName")]
    public string CarrierName { get; set; } = "Giao Hàng Nhanh (GHN)";

    [BsonElement("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [BsonElement("estimatedDeliveryDate")]
    public DateTime? EstimatedDeliveryDate { get; set; }

    [BsonElement("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }

    [BsonElement("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    [BsonElement("cancellationReason")]
    public string? CancellationReason { get; set; }

    [BsonElement("customerNotes")]
    public string? CustomerNotes { get; set; }

    [BsonElement("adminNotes")]
    public string? AdminNotes { get; set; }

    [BsonElement("deliveryType")]
    public string DeliveryType { get; set; } = "HomeDelivery"; // "HomeDelivery" or "StorePickup"

    [BsonElement("pickupStoreAddress")]
    public string? PickupStoreAddress { get; set; }

    [BsonElement("requireVatInvoice")]
    public bool RequireVatInvoice { get; set; } = false;

    [BsonElement("companyName")]
    public string? CompanyName { get; set; }

    [BsonElement("taxId")]
    public string? TaxId { get; set; }

    [BsonElement("companyAddress")]
    public string? CompanyAddress { get; set; }

    [BsonElement("invoiceEmail")]
    public string? InvoiceEmail { get; set; }

    [BsonElement("isManualOrder")]
    public bool IsManualOrder { get; set; } = false;

    [BsonElement("sourceChannel")]
    public string SourceChannel { get; set; } = "Website"; // Website, Telesales, Fanpage, Zalo, DirectStore

    [BsonElement("refundStatus")]
    public string RefundStatus { get; set; } = "None"; // None, Requested, Approved, Rejected, Refunded

    [BsonElement("refundAmount")]
    public decimal? RefundAmount { get; set; }

    [BsonElement("refundReason")]
    public string? RefundReason { get; set; }

    [BsonElement("refundMethod")]
    public string? RefundMethod { get; set; } // BankTransfer, OriginalPaymentMethod, StoreCredit

    [BsonElement("refundApprovedBy")]
    public string? RefundApprovedBy { get; set; }

    [BsonElement("refundApprovedAt")]
    public DateTime? RefundApprovedAt { get; set; }

    [BsonElement("assignedStaffId")]
    public string? AssignedStaffId { get; set; }

    [BsonElement("assignedStaffName")]
    public string? AssignedStaffName { get; set; }

    [BsonElement("deliveryTimeSlot")]
    public string? DeliveryTimeSlot { get; set; } = "Giờ hành chính (8h-17h)";

    [BsonElement("callVerificationStatus")]
    public string CallVerificationStatus { get; set; } = "PendingCall"; // PendingCall, CallVerified, Unreachable, Rescheduled

    [BsonElement("callVerificationNote")]
    public string? CallVerificationNote { get; set; }

    [BsonElement("internalNotes")]
    public string? InternalNotes { get; set; }

    [BsonElement("returnInitiationStatus")]
    public string ReturnInitiationStatus { get; set; } = "None"; // None, Requested, SentToTechnician, Completed

    [BsonElement("returnInitiationReason")]
    public string? ReturnInitiationReason { get; set; }

    [BsonElement("returnInitiationNotes")]
    public string? ReturnInitiationNotes { get; set; }

    [BsonElement("statusHistory")]
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class OrderItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("productSlug")]
    public string ProductSlug { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("variantName")]
    public string VariantName { get; set; } = string.Empty;

    [BsonElement("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("totalPrice")]
    public decimal TotalPrice => UnitPrice * Quantity;

    [BsonElement("warrantyMonths")]
    public int WarrantyMonths { get; set; } = 12;

    [BsonElement("assignedSerialNumbers")]
    public List<string> AssignedSerialNumbers { get; set; } = new(); // Serial/IMEI gán trong lúc Scan-to-Pack
}

[BsonIgnoreExtraElements]
public class OrderShippingAddress
{
    [BsonElement("receiverName")]
    public string ReceiverName { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("province")]
    public string Province { get; set; } = string.Empty;

    [BsonElement("district")]
    public string District { get; set; } = string.Empty;

    [BsonElement("ward")]
    public string Ward { get; set; } = string.Empty;

    [BsonElement("detailAddress")]
    public string DetailAddress { get; set; } = string.Empty;

    [BsonElement("fullAddress")]
    public string FullAddress => $"{DetailAddress}, {Ward}, {District}, {Province}".Trim(',', ' ');
}

[BsonIgnoreExtraElements]
public class OrderStatusHistory
{
    [BsonElement("status")]
    public OrderStatus Status { get; set; }

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;

    [BsonElement("changedBy")]
    public string ChangedBy { get; set; } = "System";

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
