using MongoDB.Bson.Serialization.Attributes;
using TechZone.Domain.Common;
using TechZone.Domain.Enums;

namespace TechZone.Domain.Entities;

public class Payment : BaseEntity, IAggregateRoot
{
    [BsonElement("paymentCode")]
    public string PaymentCode { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("orderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [BsonElement("method")]
    public PaymentMethod Method { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [BsonElement("transactionId")]
    public string? TransactionId { get; set; }

    [BsonElement("bankCode")]
    public string? BankCode { get; set; }

    [BsonElement("paymentUrl")]
    public string? PaymentUrl { get; set; }

    [BsonElement("qrCodeUrl")]
    public string? QrCodeUrl { get; set; }

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("webhookPayload")]
    public string? WebhookPayload { get; set; }
}
