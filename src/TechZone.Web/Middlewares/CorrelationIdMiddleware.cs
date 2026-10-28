using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace TechZone.Web.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Retrieve or generate Correlation ID
        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId) && !string.IsNullOrWhiteSpace(existingId))
        {
            correlationId = existingId.ToString();
        }
        else
        {
            correlationId = $"tz-corr-{Guid.NewGuid().ToString("N")[..12]}";
        }

        // 2. Store in HttpContext.Items for application-wide access
        context.Items["CorrelationId"] = correlationId;

        // 3. Attach to Response headers so client/caller can trace errors
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // 4. Push into Serilog LogContext for all downstream log entries in this request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
