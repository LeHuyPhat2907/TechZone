using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Catalog;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;

namespace TechZone.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly ICacheService _cache;
    private const string CacheKeyAll = "categories:all";

    public CategoryService(IRepository<Category> categoryRepo, ICacheService cache)
    {
        _categoryRepo = categoryRepo;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<CategoryDto>>(CacheKeyAll, cancellationToken);
        if (cached != null) return cached;

        var categories = await _categoryRepo.FindAsync(c => c.IsActive, cancellationToken);
        var dtos = categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IconUrl = c.IconUrl,
                ImageUrl = c.ImageUrl,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.ProductCount
            }).ToList();

        await _cache.SetAsync(CacheKeyAll, dtos, TimeSpan.FromMinutes(60), cancellationToken);
        return dtos;
    }

    public async Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cat = await _categoryRepo.FindOneAsync(c => c.Slug == slug && c.IsActive, cancellationToken);
        if (cat == null) return null;

        return new CategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Slug = cat.Slug,
            Description = cat.Description,
            IconUrl = cat.IconUrl,
            ImageUrl = cat.ImageUrl,
            DisplayOrder = cat.DisplayOrder,
            ProductCount = cat.ProductCount
        };
    }
}
