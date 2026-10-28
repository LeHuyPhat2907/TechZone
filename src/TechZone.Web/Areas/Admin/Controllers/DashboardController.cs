using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Services;

namespace TechZone.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly IOrderService _orderService;

    public DashboardController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var metrics = await _orderService.GetDashboardMetricsAsync();
        return View(metrics);
    }
}
