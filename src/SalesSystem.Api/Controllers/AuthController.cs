using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantService _tenantService;

    public AuthController(IAuthService authService, ITenantService tenantService)
    {
        _authService = authService;
        _tenantService = tenantService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Resolve tenant subdomain to actual tenant ID
        var tenant = await _tenantService.GetBySubdomainAsync(request.TenantId);
        if (tenant is null)
            return Unauthorized(new { error = "Tenant not found." });
        if (!tenant.IsActive)
            return Unauthorized(new { error = "Tenant is inactive." });

        request.TenantId = tenant.Id;
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        result.Data!.TenantId = tenant.Id;
        result.Data!.TenantSubdomain = tenant.Subdomain;
        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return result.Success ? Ok(result.Data) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        await _authService.RevokeTokenAsync(userId, request.RefreshToken);
        return NoContent();
    }
}

public class RefreshTokenRequest { public string RefreshToken { get; set; } = string.Empty; }
public class RevokeTokenRequest { public string RefreshToken { get; set; } = string.Empty; }

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _userService.GetAllAsync(TenantId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id, TenantId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        user.TenantId = TenantId;
        var result = await _userService.CreateAsync(user);
        return result.Success ? Created($"/api/users/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] User user)
    {
        user.Id = id;
        user.TenantId = TenantId;
        var result = await _userService.UpdateAsync(user);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _userService.DeleteAsync(id, TenantId);
        return deleted ? NoContent() : NotFound();
    }
}
