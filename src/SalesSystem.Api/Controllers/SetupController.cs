using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SetupController : ControllerBase
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IMongoDatabase _db;

    public SetupController(ITenantRepository tenantRepo, IUserRepository userRepo, IMongoDatabase db)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Setup()
    {
        // Create or get default tenant
        var tenant = await _tenantRepo.GetBySubdomainAsync("demo");
        bool tenantCreated = false;

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = "Empresa Demo",
                Subdomain = "demo",
                IsActive = true,
                Modules = ["produtos", "clientes", "estoque", "pedidos", "financeiro", "insumos"]
            };
            tenant = await _tenantRepo.InsertAsync(tenant);
            tenantCreated = true;
        }

        // Create or get default admin user
        var user = await _userRepo.GetByEmailAsync("admin@admin.com", tenant.Id);
        bool userCreated = false;

        if (user is null)
        {
            user = new User
            {
                Name = "Administrador",
                Email = "admin@admin.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                Role = UserRole.Admin,
                Permissions = Permission.DefaultFor(UserRole.Admin),
                TenantId = tenant.Id,
                IsActive = true
            };
            user = await _userRepo.InsertAsync(user);
            userCreated = true;
        }

        return Ok(new
        {
            message = tenantCreated || userCreated ? "Setup realizado com sucesso." : "Ja configurado.",
            tenant = new
            {
                tenant.Id,
                tenant.Name,
                tenant.Subdomain,
                tenant.IsActive
            },
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                Role = user.Role.ToString(),
                user.Permissions,
                user.TenantId
            }
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        var collections = new[] { "orders", "products", "recipes", "insumos", "insumo_purchases", "financials", "customers", "stock_balances", "stock_moves" };
        var deleted = new Dictionary<string, long>();

        foreach (var name in collections)
        {
            var col = _db.GetCollection<MongoDB.Bson.BsonDocument>(name);
            var result = await col.DeleteManyAsync(MongoDB.Driver.FilterDefinition<MongoDB.Bson.BsonDocument>.Empty);
            deleted[name] = result.DeletedCount;
        }

        return Ok(new { message = "Todos os registros foram apagados (tenant e usuario mantidos).", deleted });
    }
}
