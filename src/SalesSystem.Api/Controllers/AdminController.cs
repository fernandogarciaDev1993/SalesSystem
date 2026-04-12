using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "GlobalAdmin")]
public class AdminController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public AdminController(ITenantService tenantService, IUserService userService)
    {
        _tenantService = tenantService;
        _userService = userService;
    }

    // GET /api/admin/tenants
    [HttpGet("tenants")]
    public async Task<IActionResult> ListTenants()
        => Ok(await _tenantService.GetAllAsync());

    // GET /api/admin/tenants/{id}
    [HttpGet("tenants/{id}")]
    public async Task<IActionResult> GetTenant(string id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    // POST /api/admin/tenants
    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] Tenant tenant)
    {
        var created = await _tenantService.CreateAsync(tenant);
        return Created($"/api/admin/tenants/{created.Id}", created);
    }

    // PUT /api/admin/tenants/{id}
    [HttpPut("tenants/{id}")]
    public async Task<IActionResult> UpdateTenant(string id, [FromBody] Tenant tenant)
    {
        tenant.Id = id;
        await _tenantService.UpdateAsync(tenant);
        return Ok(tenant);
    }

    // PUT /api/admin/tenants/{id}/ui
    [HttpPut("tenants/{id}/ui")]
    public async Task<IActionResult> UpdateTenantUi(string id, [FromBody] TenantUiConfig config)
    {
        var savedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        await _tenantService.UpdateUiConfigAsync(id, config, savedBy);
        return Ok(new { message = "UI config updated." });
    }

    // GET /api/admin/tenants/{id}/users
    [HttpGet("tenants/{id}/users")]
    public async Task<IActionResult> ListUsers(string id)
        => Ok(await _userService.GetAllAsync(id));

    // POST /api/admin/tenants/{id}/users
    [HttpPost("tenants/{id}/users")]
    public async Task<IActionResult> CreateUser(string id, [FromBody] CreateUserRequest request)
    {
        var user = new User
        {
            TenantId = id,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = request.Password, // UserService.CreateAsync will hash this
            Role = request.Role,
            Permissions = request.Permissions.Count > 0
                ? request.Permissions
                : Permission.DefaultFor(request.Role),
            IsActive = true
        };

        var result = await _userService.CreateAsync(user);
        return result.Success
            ? Created($"/api/admin/tenants/{id}/users/{result.Data!.Id}", result.Data)
            : BadRequest(new { error = result.Error });
    }

    // DELETE /api/admin/tenants/{id}/users/{userId}
    [HttpDelete("tenants/{id}/users/{userId}")]
    public async Task<IActionResult> DeleteUser(string id, string userId)
    {
        var deleted = await _userService.DeleteAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }
}

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Vendedor;
    public List<string> Permissions { get; set; } = [];
}
