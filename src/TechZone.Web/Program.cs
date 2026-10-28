using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using Serilog.Events;
using TechZone.Application;
using TechZone.Infrastructure;
using TechZone.Infrastructure.Data;
using TechZone.Infrastructure.Hubs;
using TechZone.Web.Middlewares;

// Configure Serilog Structured Logging (Task 17)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/techzone-app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Forwarded Headers for Proxmox, Nginx and Cloudflare Tunnel support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
#pragma warning disable ASPDEPR005
    options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
    options.KnownProxies.Clear();
});

// Built-in Health Checks (Task 17)
builder.Services.AddHealthChecks();

// 1. Add services with Global Anti-CSRF Protection (OWASP A08)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();

// Response Compression (Brotli & Gzip - Task 15 SEO & Performance)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/json",
        "application/xml",
        "text/plain",
        "text/css",
        "application/javascript"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// 2. Real-Time Communication Hub (SignalR - Task 14)
builder.Services.AddSignalR();

// 3. Anti-CSRF Token Configuration
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.HeaderName = "X-CSRF-TOKEN";
});

// 4. Infrastructure and Application layer DI
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// 4. ASP.NET Core Rate Limiting (.NET 10 - OWASP A04 & A07 Protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy for sensitive authentication endpoints (Max 5 attempts/minute per IP)
    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_auth_client",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Policy for general global endpoints (Max 120 requests/minute per IP)
    options.AddPolicy("GlobalPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
});

// 5. Strict HSTS Configuration (OWASP A02 & Transport Security)
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// 6. Session Configuration for Guest shopping carts (Hardened Cookie)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = "TechZone.Session";
});

// 7. Cookie Authentication (Hardened Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "TechZone.Auth";
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userPrincipal = context.Principal;
                var userId = userPrincipal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var stampClaim = userPrincipal?.FindFirst("SecurityStamp")?.Value;

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(stampClaim))
                {
                    var userRepo = context.HttpContext.RequestServices.GetService<TechZone.Domain.Interfaces.IRepository<TechZone.Domain.Entities.User>>();
                    if (userRepo != null)
                    {
                        var user = await userRepo.GetByIdAsync(userId);
                        if (user == null || !user.IsActive || user.SecurityStamp != stampClaim)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("SuperAdmin", "SalesStaff", "WarehouseStaff", "Technician"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
});

var app = builder.Build();

// Run MongoDB Index Creation & Seed Initial Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<IMongoDbContext>();
        await dbContext.CreateIndexesAsync();

        var seeder = services.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseCorrelationId();
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // 1-year aggressive static cache for CSS, JS, SVG, Fonts, Images with immutable flag
        const int durationInSeconds = 60 * 60 * 24 * 365; // 365 days
        ctx.Context.Response.Headers.CacheControl = $"public,max-age={durationInSeconds},immutable";
    }
});

app.UseRouting();
app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Health Checks Monitoring Endpoint (Task 17)
app.MapHealthChecks("/health");

// SignalR Real-Time Notification Endpoint (Task 14)
app.MapHub<NotificationHub>("/hub/notifications");

app.MapAreaControllerRoute(
    name: "admin_area",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Starting TechZone E-Commerce Web Application (.NET 10)");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TechZone Web Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
