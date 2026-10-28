using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechZone.Application.DTOs.Auth;
using TechZone.Application.Services;

namespace TechZone.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ICartService _cartService;

    public AuthController(IAuthService authService, ICartService cartService)
    {
        _authService = authService;
        _cartService = cartService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;

        var result = await _authService.LoginAsync(dto);
        if (!result.Succeeded || result.Data == null)
        {
            ModelState.AddModelError("", result.Message);
            return View(dto);
        }

        // Create authentication claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Data.UserId),
            new(ClaimTypes.Name, result.Data.FullName),
            new(ClaimTypes.Email, result.Data.Email),
            new(ClaimTypes.Role, result.Data.Role.ToString()),
            new("SecurityStamp", result.Data.SecurityStamp)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = dto.RememberMe,
            ExpiresUtc = dto.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        // Merge Guest Cart into User Cart (BRD Task 20 requirement)
        var guestSessionId = HttpContext.Session.GetString("GuestSessionId");
        if (!string.IsNullOrEmpty(guestSessionId))
        {
            await _cartService.MergeCartAsync(guestSessionId, result.Data.UserId);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (result.Data.Role == Domain.Enums.UserRole.Technician)
        {
            return Redirect("/Admin/Technician");
        }

        if (result.Data.Role == Domain.Enums.UserRole.WarehouseStaff)
        {
            return Redirect("/Admin/Warehouse");
        }

        if (result.Data.Role == Domain.Enums.UserRole.SalesStaff)
        {
            return Redirect("/Admin/Sales");
        }

        if (result.Data.Role == Domain.Enums.UserRole.SuperAdmin || 
            result.Data.Role == Domain.Enums.UserRole.Accountant)
        {
            return Redirect("/Admin/Dashboard");
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new RegisterDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.Succeeded || result.Data == null)
        {
            ModelState.AddModelError("", result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Hãy đăng nhập để bắt đầu trải nghiệm.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }

        var profile = await _authService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
