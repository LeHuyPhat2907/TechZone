using Microsoft.AspNetCore.Mvc;
using TechZone.Application.Common.Interfaces;
using TechZone.Application.DTOs.Auth;
using TechZone.Application.Services;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;

namespace TechZone.Web.Controllers;

[ApiController]
[Route("api/v1/auth")]
[IgnoreAntiforgeryToken] // REST APIs use Bearer token authorization instead of CSRF cookie
public class ApiAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtService;
    private readonly IRepository<User> _userRepo;

    public ApiAuthController(
        IAuthService authService,
        IJwtTokenService jwtService,
        IRepository<User> userRepo)
    {
        _authService = authService;
        _jwtService = jwtService;
        _userRepo = userRepo;
    }

    /// <summary>
    /// Generates a signed JWT Access Token for Mobile App or External API Clients.
    /// Demonstrates the Task 12 JWT Authentication architecture.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { succeeded = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.LoginAsync(dto);
        if (!result.Succeeded || result.Data == null)
        {
            return Unauthorized(new { succeeded = false, message = result.Message });
        }

        var user = await _userRepo.GetByIdAsync(result.Data.UserId);
        if (user == null)
        {
            return Unauthorized(new { succeeded = false, message = "Không tìm thấy thông tin tài khoản." });
        }

        var token = _jwtService.GenerateAccessToken(user, expirationMinutes: 60);

        return Ok(new
        {
            succeeded = true,
            accessToken = token,
            tokenType = "Bearer",
            expiresInSeconds = 3600,
            user = new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role.ToString()
            }
        });
    }
}
