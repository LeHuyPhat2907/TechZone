using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Cart;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;

namespace TechZone.Application.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string? userId, string? guestSessionId, CancellationToken cancellationToken = default);
    Task<CartDto> AddToCartAsync(string? userId, string? guestSessionId, AddToCartDto dto, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateQuantityAsync(string? userId, string? guestSessionId, string variantSku, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(string? userId, string? guestSessionId, string variantSku, CancellationToken cancellationToken = default);
    Task ClearCartAsync(string? userId, string? guestSessionId, CancellationToken cancellationToken = default);
    Task MergeCartAsync(string guestSessionId, string userId, CancellationToken cancellationToken = default);
}

public class CartService : ICartService
{
    private readonly IRepository<Cart> _cartRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly ICacheService _cache;

    public CartService(IRepository<Cart> cartRepo, IRepository<Product> productRepo, ICacheService cache)
    {
        _cartRepo = cartRepo;
        _productRepo = productRepo;
        _cache = cache;
    }

    private string GetCacheKey(string? userId, string? guestSessionId)
    {
        return !string.IsNullOrEmpty(userId)
            ? $"cart:user:{userId}"
            : $"cart:guest:{guestSessionId ?? "unknown"}";
    }

    private async Task<Cart> GetOrCreateCartEntityAsync(string? userId, string? guestSessionId, CancellationToken ct)
    {
        Cart? cart = null;
        if (!string.IsNullOrEmpty(userId))
        {
            cart = await _cartRepo.FindOneAsync(c => c.UserId == userId, ct);
        }
        else if (!string.IsNullOrEmpty(guestSessionId))
        {
            cart = await _cartRepo.FindOneAsync(c => c.GuestSessionId == guestSessionId, ct);
        }

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                GuestSessionId = string.IsNullOrEmpty(userId) ? guestSessionId : null,
                ExpiresAt = string.IsNullOrEmpty(userId) ? DateTime.UtcNow.AddDays(7) : null
            };
            await _cartRepo.AddAsync(cart, ct);
        }

        return cart;
    }

    public async Task<CartDto> GetCartAsync(string? userId, string? guestSessionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId, guestSessionId);
        var cached = await _cache.GetAsync<CartDto>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var cart = await GetOrCreateCartEntityAsync(userId, guestSessionId, cancellationToken);
        var dto = MapToDto(cart);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), cancellationToken);
        return dto;
    }

    public async Task<CartDto> AddToCartAsync(string? userId, string? guestSessionId, AddToCartDto dto, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, guestSessionId, cancellationToken);

        // Find product
        var product = await _productRepo.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null || !product.IsActive)
        {
            return MapToDto(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.VariantSku == dto.VariantSku);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            // Determine variant info or fallback to main product
            var variant = product.Variants.FirstOrDefault(v => v.Sku == dto.VariantSku);
            var newItem = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                VariantSku = variant?.Sku ?? product.Sku,
                VariantName = variant?.VariantName ?? product.Name,
                ThumbnailUrl = variant?.ImageUrl ?? product.ThumbnailUrl,
                Price = variant?.Price ?? product.Price,
                OriginalPrice = variant?.OriginalPrice ?? product.OriginalPrice,
                Quantity = dto.Quantity,
                WarrantyMonths = product.WarrantyMonths
            };
            cart.Items.Add(newItem);
        }

        await _cartRepo.UpdateAsync(cart, cancellationToken);

        var cartDto = MapToDto(cart);
        var cacheKey = GetCacheKey(userId, guestSessionId);
        await _cache.SetAsync(cacheKey, cartDto, TimeSpan.FromMinutes(30), cancellationToken);

        return cartDto;
    }

    public async Task<CartDto> UpdateQuantityAsync(string? userId, string? guestSessionId, string variantSku, int quantity, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, guestSessionId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.VariantSku == variantSku);

        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            await _cartRepo.UpdateAsync(cart, cancellationToken);
        }

        var cartDto = MapToDto(cart);
        var cacheKey = GetCacheKey(userId, guestSessionId);
        await _cache.SetAsync(cacheKey, cartDto, TimeSpan.FromMinutes(30), cancellationToken);

        return cartDto;
    }

    public async Task<CartDto> RemoveItemAsync(string? userId, string? guestSessionId, string variantSku, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, guestSessionId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.VariantSku == variantSku);

        if (item != null)
        {
            cart.Items.Remove(item);
            await _cartRepo.UpdateAsync(cart, cancellationToken);
        }

        var cartDto = MapToDto(cart);
        var cacheKey = GetCacheKey(userId, guestSessionId);
        await _cache.SetAsync(cacheKey, cartDto, TimeSpan.FromMinutes(30), cancellationToken);

        return cartDto;
    }

    public async Task ClearCartAsync(string? userId, string? guestSessionId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartEntityAsync(userId, guestSessionId, cancellationToken);
        cart.Items.Clear();
        await _cartRepo.UpdateAsync(cart, cancellationToken);

        var cacheKey = GetCacheKey(userId, guestSessionId);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    public async Task MergeCartAsync(string guestSessionId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(guestSessionId) || string.IsNullOrEmpty(userId)) return;

        var guestCart = await _cartRepo.FindOneAsync(c => c.GuestSessionId == guestSessionId, cancellationToken);
        if (guestCart == null || guestCart.Items.Count == 0) return;

        var userCart = await GetOrCreateCartEntityAsync(userId, null, cancellationToken);

        foreach (var gItem in guestCart.Items)
        {
            var uItem = userCart.Items.FirstOrDefault(i => i.VariantSku == gItem.VariantSku);
            if (uItem != null)
            {
                uItem.Quantity += gItem.Quantity;
            }
            else
            {
                userCart.Items.Add(gItem);
            }
        }

        await _cartRepo.UpdateAsync(userCart, cancellationToken);
        await _cartRepo.DeleteAsync(guestCart.Id, softDelete: false, cancellationToken);

        await _cache.RemoveAsync(GetCacheKey(null, guestSessionId), cancellationToken);
        await _cache.SetAsync(GetCacheKey(userId, null), MapToDto(userCart), TimeSpan.FromMinutes(30), cancellationToken);
    }

    private static CartDto MapToDto(Cart c) => new()
    {
        Id = c.Id,
        UserId = c.UserId,
        GuestSessionId = c.GuestSessionId,
        Items = c.Items.Select(i => new CartItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductSlug = i.ProductSlug,
            VariantSku = i.VariantSku,
            VariantName = i.VariantName,
            ThumbnailUrl = i.ThumbnailUrl,
            Price = i.Price,
            OriginalPrice = i.OriginalPrice,
            Quantity = i.Quantity,
            WarrantyMonths = i.WarrantyMonths
        }).ToList()
    };
}
