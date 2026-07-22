using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Auth;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Settings;

namespace RentalVehicle.Api.Services;

public interface IAuthService
{
    Task<(bool success, string? error, RegisterResponse? data)> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<(bool success, string? error, LoginResponse? data)> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(AppDbContext db, IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<(bool success, string? error, RegisterResponse? data)> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return (false, "Họ tên là bắt buộc.", null);
        if (string.IsNullOrWhiteSpace(request.Email))
            return (false, "Email là bắt buộc.", null);
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return (false, "Password must be at least 6 characters.", null);
        if (request.Password != request.ConfirmPassword)
            return (false, "Mật khẩu xác nhận không khớp.", null);

        var normalized = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == normalized, ct))
            return (false, "Email đã được đăng ký.", null);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Role = Roles.Customer,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return (true, null, new RegisterResponse());
    }

    public async Task<(bool success, string? error, LoginResponse? data)> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Email và mật khẩu là bắt buộc.", null);

        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Email hoặc mật khẩu không đúng.", null);

        var token = CreateJwt(user);
        return (true, null, new LoginResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }
        });
    }

    private string CreateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("fullName", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
