using TechZone.Application.Common.Interfaces;
using TechZone.Application.Common.Models;
using TechZone.Application.DTOs.Auth;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;
using TechZone.Domain.Interfaces;

namespace TechZone.Application.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHtmlSanitizerService _sanitizer;

    public AuthService(
        IRepository<User> userRepo, 
        IPasswordHasher passwordHasher,
        IHtmlSanitizerService sanitizer)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _sanitizer = sanitizer;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<AuthResponseDto>.Failure("Email và mật khẩu không được để trống.");
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return Result<AuthResponseDto>.Failure("Mật khẩu xác nhận không khớp.");
        }

        // Strong password policy: Min 8 chars, 1 uppercase, 1 lowercase, 1 digit
        if (dto.Password.Length < 8 ||
            !dto.Password.Any(char.IsUpper) ||
            !dto.Password.Any(char.IsLower) ||
            !dto.Password.Any(char.IsDigit))
        {
            return Result<AuthResponseDto>.Failure("Mật khẩu phải từ 8 ký tự trở lên, gồm ít nhất một chữ hoa, một chữ thường và một chữ số.");
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var exists = await _userRepo.ExistsAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return Result<AuthResponseDto>.Failure("Email này đã được sử dụng trên TechZone.");
        }

        var user = new User
        {
            FullName = _sanitizer.Sanitize(dto.FullName),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Role = UserRole.Customer,
            IsActive = true,
            IsEmailVerified = true, // Auto verify for demo/student showcase
            RewardPoints = 100 // Welcome gift points
        };

        await _userRepo.AddAsync(user, cancellationToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            SecurityStamp = user.SecurityStamp
        }, "Đăng ký tài khoản thành công!");
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<AuthResponseDto>.Failure("Email và mật khẩu không được để trống.");
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.FindOneAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return Result<AuthResponseDto>.Failure("Email hoặc mật khẩu không chính xác.");
        }

        // Check Account Lockout (OWASP A07 - Brute force protection)
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var minutesLeft = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
            return Result<AuthResponseDto>.Failure($"Tài khoản đã bị tạm khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau {minutesLeft} phút.");
        }

        bool isValidPassword = _passwordHasher.VerifyPassword(dto.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _userRepo.UpdateAsync(user, cancellationToken);
                return Result<AuthResponseDto>.Failure("Tài khoản đã bị tạm khóa 15 phút do nhập sai mật khẩu quá 5 lần liên tiếp.");
            }

            await _userRepo.UpdateAsync(user, cancellationToken);
            int attemptsRemaining = 5 - user.AccessFailedCount;
            return Result<AuthResponseDto>.Failure($"Email hoặc mật khẩu không chính xác. Bạn còn {attemptsRemaining} lần thử.");
        }

        // Reset lockout counters on successful authentication
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            SecurityStamp = user.SecurityStamp
        }, "Đăng nhập thành công!");
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            RewardPoints = user.RewardPoints,
            Addresses = user.Addresses.Select(a => new UserAddressDto
            {
                Id = a.Id,
                ReceiverName = a.ReceiverName,
                PhoneNumber = a.PhoneNumber,
                Province = a.Province,
                District = a.District,
                Ward = a.Ward,
                DetailAddress = a.DetailAddress,
                IsDefault = a.IsDefault
            }).ToList()
        };
    }
}
