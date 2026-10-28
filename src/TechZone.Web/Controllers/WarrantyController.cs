using Microsoft.AspNetCore.Mvc;
using TechZone.Application.DTOs.Warranty;
using TechZone.Application.Services;

namespace TechZone.Web.Controllers;

public class WarrantyController : Controller
{
    private readonly IWarrantyService _warrantyService;

    public WarrantyController(IWarrantyService warrantyService)
    {
        _warrantyService = warrantyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return View();
        }

        var result = await _warrantyService.LookupAsync(serialNumber);
        if (!result.Succeeded || result.Data == null)
        {
            ViewBag.ErrorMessage = result.Message;
            return View();
        }

        return View(result.Data);
    }
}
