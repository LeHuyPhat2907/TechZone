using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Order;

namespace TechZone.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;

        var enabled = _configuration.GetValue<bool>("EmailSettings:Enabled", false);
        var host = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue<int>("EmailSettings:Port", 587);
        var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl", true);
        var userName = _configuration["EmailSettings:UserName"] ?? "noreply@techzone.vn";
        var password = _configuration["EmailSettings:Password"] ?? string.Empty;
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@techzone.vn";
        var fromName = _configuration["EmailSettings:FromName"] ?? "TechZone Việt Nam";

        if (!enabled || string.IsNullOrWhiteSpace(password))
        {
            // Simulation Mode (Safe for Development, CI/CD, and Server without configured credentials)
            _logger.LogInformation(
                "[EMAIL SIMULATED - DEV MODE]\n" +
                "====================================================\n" +
                "To: {ToEmail}\n" +
                "Subject: {Subject}\n" +
                "Provider: SMTP ({Host}:{Port})\n" +
                "====================================================",
                toEmail, subject, host, port);
            return true;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000 // 10 seconds
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName, Encoding.UTF8),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(toEmail));

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}. Host: {Host}:{Port}", toEmail, host, port);
            return false;
        }
    }

    public async Task<bool> SendOrderConfirmationEmailAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerEmail)) return false;

        var subject = $"[TechZone] Xác nhận đặt hàng thành công #{order.OrderCode}";
        var itemsHtml = new StringBuilder();

        foreach (var item in order.Items)
        {
            itemsHtml.Append($@"
                <tr style='border-bottom: 1px solid #e2e8f0;'>
                    <td style='padding: 12px 0;'>
                        <strong>{item.ProductName}</strong><br/>
                        <span style='font-size: 12px; color: #64748b;'>Phân loại: {item.VariantSku}</span>
                    </td>
                    <td style='padding: 12px 0; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 12px 0; text-align: right;'>{item.UnitPrice:N0}₫</td>
                    <td style='padding: 12px 0; text-align: right; font-weight: bold; color: #0284c7;'>{item.TotalPrice:N0}₫</td>
                </tr>");
        }

        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
            <div style='background: #0f172a; padding: 24px; text-align: center; color: #ffffff;'>
                <h2 style='margin: 0; font-size: 24px; letter-spacing: 1px;'>TECH<span style='color: #0284c7;'>ZONE</span></h2>
                <p style='margin: 5px 0 0 0; font-size: 13px; color: #94a3b8;'>Hệ thống bán lẻ thiết bị công nghệ chính hãng</p>
            </div>
            <div style='padding: 30px;'>
                <h3 style='color: #10b981; margin-top: 0;'>🎉 Cảm ơn bạn đã đặt hàng tại TechZone!</h3>
                <p>Xin chào <strong>{order.CustomerName}</strong>,</p>
                <p>Đơn hàng của bạn với mã <strong style='color: #0284c7;'>#{order.OrderCode}</strong> đã được tiếp nhận và đang được bộ phận vận hành chuẩn bị đóng gói.</p>
                
                <div style='background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                    <h4 style='margin: 0 0 10px 0; color: #0f172a;'>Thông tin giao hàng:</h4>
                    <p style='margin: 4px 0; font-size: 14px;'><strong>Người nhận:</strong> {order.CustomerName} ({order.CustomerPhone})</p>
                    <p style='margin: 4px 0; font-size: 14px;'><strong>Địa chỉ:</strong> {order.FullAddress}</p>
                    <p style='margin: 4px 0; font-size: 14px;'><strong>Hình thức thanh toán:</strong> {order.PaymentMethod}</p>
                </div>

                <table style='width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 14px;'>
                    <thead>
                        <tr style='border-bottom: 2px solid #0f172a; text-align: left; color: #64748b;'>
                            <th style='padding-bottom: 8px;'>Sản phẩm</th>
                            <th style='padding-bottom: 8px; text-align: center;'>SL</th>
                            <th style='padding-bottom: 8px; text-align: right;'>Đơn giá</th>
                            <th style='padding-bottom: 8px; text-align: right;'>Thành tiền</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsHtml}
                    </tbody>
                </table>

                <div style='margin-top: 20px; text-align: right; font-size: 14px;'>
                    <p style='margin: 4px 0;'>Tạm tính: <strong>{order.Subtotal:N0}₫</strong></p>
                    <p style='margin: 4px 0;'>Phí vận chuyển: <strong>{(order.ShippingFee > 0 ? order.ShippingFee.ToString("N0") + "₫" : "Miễn phí")}</strong></p>
                    {((order.OrderDiscountAmount + order.ShippingDiscountAmount) > 0 ? $"<p style='margin: 4px 0; color: #ef4444;'>Giảm giá voucher: <strong>-{(order.OrderDiscountAmount + order.ShippingDiscountAmount):N0}₫</strong></p>" : "")}
                    <h3 style='margin: 10px 0 0 0; color: #ef4444; font-size: 20px;'>Tổng thanh toán: {order.TotalAmount:N0}₫</h3>
                </div>

                <div style='margin-top: 30px; text-align: center;'>
                    <a href='https://techzone.vn/Order/Track?orderCode={order.OrderCode}&phone={order.CustomerPhone}' 
                       style='display: inline-block; background: #0284c7; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 50px; font-weight: bold;'>
                        Tra Cứu Chi Tiết Đơn Hàng
                    </a>
                </div>
            </div>
            <div style='background: #f1f5f9; padding: 16px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                Hotline hỗ trợ: <strong>1900 8888</strong> | Email: support@techzone.vn<br/>
                © 2026 TechZone. All rights reserved.
            </div>
        </div>";

        return await SendEmailAsync(order.CustomerEmail, subject, html, cancellationToken);
    }

    public async Task<bool> SendOrderStatusUpdateEmailAsync(OrderDto order, string previousStatus, string newStatus, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerEmail)) return false;

        var subject = $"[TechZone] Cập nhật trạng thái đơn hàng #{order.OrderCode} ➔ {newStatus}";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
            <div style='background: #0f172a; padding: 20px; text-align: center; color: #ffffff;'>
                <h3 style='margin: 0; font-size: 20px;'>TECH<span style='color: #0284c7;'>ZONE</span></h3>
            </div>
            <div style='padding: 24px;'>
                <h3 style='color: #0284c7; margin-top: 0;'>Trạng thái đơn hàng của bạn đã thay đổi!</h3>
                <p>Kính gửi <strong>{order.CustomerName}</strong>,</p>
                <p>Đơn hàng <strong style='color: #0284c7;'>#{order.OrderCode}</strong> của bạn đã được chuyển từ trạng thái <em>'{previousStatus}'</em> sang: <strong style='color: #10b981; font-size: 16px;'>'{newStatus}'</strong>.</p>
                
                {(!string.IsNullOrEmpty(order.CarrierName) ? $@"
                <div style='background: #f8fafc; border: 1px solid #e2e8f0; padding: 12px; border-radius: 6px; margin: 16px 0;'>
                    <p style='margin: 0;'><strong>Đơn vị vận chuyển:</strong> {order.CarrierName}</p>
                    <p style='margin: 4px 0 0 0;'><strong>Mã vận đơn đối soát:</strong> <span style='font-family: monospace; font-weight: bold;'>{order.TrackingNumber}</span></p>
                </div>" : "")}

                <div style='text-align: center; margin-top: 24px;'>
                    <a href='https://techzone.vn/Order/Track?orderCode={order.OrderCode}&phone={order.CustomerPhone}' 
                       style='background: #0284c7; color: white; text-decoration: none; padding: 10px 24px; border-radius: 6px; font-weight: bold; display: inline-block;'>
                        Xem Hành Trình Đơn Hàng
                    </a>
                </div>
            </div>
        </div>";

        return await SendEmailAsync(order.CustomerEmail, subject, html, cancellationToken);
    }

    public async Task<bool> SendVatInvoiceEmailAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        var targetEmail = !string.IsNullOrEmpty(order.InvoiceEmail) ? order.InvoiceEmail : order.CustomerEmail;
        if (string.IsNullOrWhiteSpace(targetEmail)) return false;

        var subject = $"[TechZone] Hóa đơn điện tử VAT cho đơn hàng #{order.OrderCode}";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px;'>
            <h3 style='color: #0284c7; margin-top: 0;'>Phát hành Hóa đơn điện tử VAT thành công</h3>
            <p>Kính gửi Quý công ty <strong>{order.CompanyName}</strong>,</p>
            <p>Hóa đơn điện tử GTGT cho đơn hàng <strong>#{order.OrderCode}</strong> đã được xuất thành công.</p>
            <table style='width: 100%; border: 1px solid #e2e8f0; border-collapse: collapse; margin: 16px 0; font-size: 14px;'>
                <tr><td style='padding: 8px; border: 1px solid #e2e8f0; background: #f8fafc;'>Tên đơn vị:</td><td style='padding: 8px; border: 1px solid #e2e8f0;'><strong>{order.CompanyName}</strong></td></tr>
                <tr><td style='padding: 8px; border: 1px solid #e2e8f0; background: #f8fafc;'>Mã số thuế:</td><td style='padding: 8px; border: 1px solid #e2e8f0;'><strong>{order.TaxId}</strong></td></tr>
                <tr><td style='padding: 8px; border: 1px solid #e2e8f0; background: #f8fafc;'>Tổng tiền thanh toán:</td><td style='padding: 8px; border: 1px solid #e2e8f0; color: #ef4444; font-weight: bold;'>{order.TotalAmount:N0}₫</td></tr>
            </table>
            <p style='font-size: 13px; color: #64748b;'>Hóa đơn điện tử có giá trị pháp lý theo quy định của Tổng cục Thuế và Nghị định 123/2020/NĐ-CP.</p>
        </div>";

        return await SendEmailAsync(targetEmail, subject, html, cancellationToken);
    }
}
