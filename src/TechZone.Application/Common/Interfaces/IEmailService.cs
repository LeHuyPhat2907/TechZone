using TechZone.Application.DTOs.Order;

namespace TechZone.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task<bool> SendOrderConfirmationEmailAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task<bool> SendOrderStatusUpdateEmailAsync(OrderDto order, string previousStatus, string newStatus, CancellationToken cancellationToken = default);
    Task<bool> SendVatInvoiceEmailAsync(OrderDto order, CancellationToken cancellationToken = default);
}
