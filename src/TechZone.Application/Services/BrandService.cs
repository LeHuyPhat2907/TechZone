using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Catalog;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;

namespace TechZone.Application.Services;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandDto>> GetFeaturedBrandsAsync(CancellationToken cancellationToken = default);
    Task<BrandDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public class BrandService : IBrandService
{
    private readonly IRepository<Brand> _brandRepo;
    private readonly ICacheService _cache;
    private const string CacheKeyAll = "brands:all";

    public BrandService(IRepository<Brand> brandRepo, ICacheService cache)
    {
        _brandRepo = brandRepo;
        _cache = cache;
    }

    public async Task<IReadOnlyList<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<BrandDto>>(CacheKeyAll, cancellationToken);
        if (cached != null) return cached;

        var brands = await _brandRepo.FindAsync(b => b.IsActive, cancellationToken);
        var dtos = brands
            .OrderBy(b => b.DisplayOrder)
            .Select(MapToDto)
            .ToList();

        await _cache.SetAsync(CacheKeyAll, dtos, TimeSpan.FromMinutes(60), cancellationToken);
        return dtos;
    }

    public async Task<IReadOnlyList<BrandDto>> GetFeaturedBrandsAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllBrandsAsync(cancellationToken);
        return all.Where(b => b.IsFeatured).ToList();
    }

    public async Task<BrandDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepo.FindOneAsync(b => b.Slug == slug && b.IsActive, cancellationToken);
        return brand == null ? null : MapToDto(brand);
    }

    private static BrandDto MapToDto(Brand b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Slug = b.Slug,
        LogoUrl = b.LogoUrl,
        CountryOfOrigin = b.CountryOfOrigin,
        Description = b.Description,
        IsFeatured = b.IsFeatured
    };
}
