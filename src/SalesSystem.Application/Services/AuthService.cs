using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string userId, string refreshToken);
}

public interface IUserService
{
    Task<User?> GetByIdAsync(string id, string tenantId);
    Task<User?> GetByEmailAsync(string email, string tenantId);
    Task<List<User>> GetAllAsync(string tenantId);
    Task<ServiceResult<User>> CreateAsync(User user);
    Task<ServiceResult<User>> UpdateAsync(User user);
    Task<bool> DeleteAsync(string id, string tenantId);
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
    public string TenantId { get; set; } = string.Empty;
    public string TenantSubdomain { get; set; } = string.Empty;
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email, request.TenantId);
        if (user is null)
            return ServiceResult<AuthResponse>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ServiceResult<AuthResponse>.Fail("User account is disabled.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ServiceResult<AuthResponse>.Fail("Invalid email or password.");

        user.LastLogin = DateTime.UtcNow;

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        // Remove expired/revoked refresh tokens
        user.RefreshTokens.RemoveAll(rt => !rt.IsActive);

        await _userRepo.UpdateAsync(user);

        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

        return ServiceResult<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString(),
            Permissions = user.Permissions
        });
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        // Search all users for matching active refresh token
        // In practice, you'd want an index or a more efficient lookup
        var allUsers = await _userRepo.GetAllAsync(string.Empty);
        var user = allUsers.FirstOrDefault(u =>
            u.RefreshTokens.Any(rt => rt.Token == refreshToken && rt.IsActive));

        if (user is null)
            return ServiceResult<AuthResponse>.Fail("Invalid or expired refresh token.");

        var existingToken = user.RefreshTokens.First(rt => rt.Token == refreshToken);
        existingToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = GenerateRefreshToken();
        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        user.RefreshTokens.RemoveAll(rt => !rt.IsActive && rt.Token != refreshToken);

        await _userRepo.UpdateAsync(user);

        var token = GenerateJwtToken(user);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

        return ServiceResult<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            RefreshToken = newRefreshToken,
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString(),
            Permissions = user.Permissions
        });
    }

    public async Task RevokeTokenAsync(string userId, string refreshToken)
    {
        var user = await _userRepo.GetByIdAsync(userId, string.Empty);
        if (user is null) return;

        var token = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
        if (token is null) return;

        token.RevokedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured.")));

        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("tenantId", user.TenantId)
        };

        foreach (var permission in user.Permissions)
            claims.Add(new Claim("permission", permission));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(securityToken);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}

public class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<User?> GetByIdAsync(string id, string tenantId)
        => await _repo.GetByIdAsync(id, tenantId);

    public async Task<User?> GetByEmailAsync(string email, string tenantId)
        => await _repo.GetByEmailAsync(email, tenantId);

    public async Task<List<User>> GetAllAsync(string tenantId)
        => await _repo.GetAllAsync(tenantId);

    public async Task<ServiceResult<User>> CreateAsync(User user)
    {
        var existing = await _repo.GetByEmailAsync(user.Email, user.TenantId);
        if (existing is not null)
            return ServiceResult<User>.Fail("A user with this email already exists.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

        if (user.Permissions.Count == 0)
            user.Permissions = Permission.DefaultFor(user.Role);

        var created = await _repo.InsertAsync(user);
        return ServiceResult<User>.Ok(created);
    }

    public async Task<ServiceResult<User>> UpdateAsync(User user)
    {
        var existing = await _repo.GetByIdAsync(user.Id, user.TenantId);
        if (existing is null)
            return ServiceResult<User>.Fail("User not found.");

        // If email changed, check for duplicates
        if (existing.Email != user.Email)
        {
            var duplicate = await _repo.GetByEmailAsync(user.Email, user.TenantId);
            if (duplicate is not null)
                return ServiceResult<User>.Fail("A user with this email already exists.");
        }

        await _repo.UpdateAsync(user);
        return ServiceResult<User>.Ok(user);
    }

    public async Task<bool> DeleteAsync(string id, string tenantId)
        => await _repo.DeleteAsync(id, tenantId);
}
