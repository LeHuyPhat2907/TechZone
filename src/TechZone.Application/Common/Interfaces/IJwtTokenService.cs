using System.Security.Claims;
using TechZone.Domain.Entities;

namespace TechZone.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, int expirationMinutes = 60);
    ClaimsPrincipal? ValidateToken(string token);
}
