using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechZone.Application.DTOs.Order;
using TechZone.Application.Services;

namespace TechZone.Web.Controllers;

public class OrderController : Controller
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? orderCode, string? phone)
    {
        if (string.IsNullOrWhiteSpace(orderCode) && string.IsNullOrWhiteSpace(phone))
        {
            return View();
        }

        var result = await _orderService.TrackOrderAsync(orderCode, phone);
        if (!result.Succeeded || result.Data == null)
        {
            ViewBag.ErrorMessage = result.Message;
            return View();
        }

        return View(result.Data);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var orders = await _orderService.GetOrdersByUserAsync(userId);
        return View(orders);
    }
}
