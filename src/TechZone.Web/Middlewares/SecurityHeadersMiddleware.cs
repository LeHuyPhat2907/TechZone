using Microsoft.AspNetCore.Http;

namespace TechZone.Web.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // 1. Prevent MIME-sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // 2. Prevent Clickjacking (framing)
            headers["X-Frame-Options"] = "SAMEORIGIN";

            // 3. Legacy XSS filter for older browsers
            headers["X-XSS-Protection"] = "1; mode=block";

            // 4. Strict Referrer Policy
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // 5. Restrict sensitive browser features/permissions
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // 6. Content Security Policy (CSP)
            headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                "font-src 'self' data: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com; " +
                "img-src 'self' data: https: blob:; " +
                "connect-src 'self' https: wss: ws:; " +
                "frame-ancestors 'self';";

            // 7. Strip fingerprinting headers
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
