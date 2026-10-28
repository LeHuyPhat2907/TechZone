using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechZone.Application.Common.Interfaces;
using TechZone.Domain.Interfaces;
using TechZone.Infrastructure.Configuration;
using TechZone.Infrastructure.Data;
using TechZone.Infrastructure.Repositories;
using TechZone.Infrastructure.Services;

namespace TechZone.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Options Configuration
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));

        // 2. MongoDB Context
        services.AddSingleton<IMongoDbContext, MongoDbContext>();

        // 3. Unit of Work & Generic Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // 4. Redis Cache Service with resilient memory fallback
        services.AddSingleton<ICacheService, RedisCacheService>();

        // 5. Security: Password Hashing, JWT Token & HTML Sanitizer
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

        // 6. Notification & Email Communication Services (Task 14)
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAppNotificationService, AppNotificationService>();

        // 7. Storage & Image Upload Service (Task 16)
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.AddScoped<IStorageService, LocalStorageService>();

        // 8. Database Seeder
        services.AddScoped<DbSeeder>();

        return services;
    }
}
