namespace TechZone.Application.DTOs.Cart;

public class CartDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public int TotalItems => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
    public decimal EstimatedShippingFee => Subtotal >= 500000m || Subtotal == 0 ? 0m : 30000m;
    public decimal EstimatedTotal => Subtotal + EstimatedShippingFee;
}

public class CartItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => Price * Quantity;
    public int WarrantyMonths { get; set; } = 12;
}

public class AddToCartDto
{
    public string ProductId { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemQuantityDto
{
    public string VariantSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
