using TechZone.Application.DTOs.Cart;
using TechZone.Application.DTOs.Order;

namespace TechZone.Web.Models;

public class CheckoutViewModel
{
    public CartDto Cart { get; set; } = new();
    public CheckoutDto Checkout { get; set; } = new();
}
