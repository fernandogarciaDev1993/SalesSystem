using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService) => _tenantService = tenantService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
        => Ok(await _tenantService.GetAllAsync());

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    [Authorize(Policy = "GlobalAdmin")]
    public async Task<IActionResult> Create([FromBody] Tenant tenant)
    {
        var created = await _tenantService.CreateAsync(tenant);
        return Created($"/api/tenants/{created.Id}", created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "GlobalAdmin")]
    public async Task<IActionResult> Update(string id, [FromBody] Tenant tenant)
    {
        tenant.Id = id;
        await _tenantService.UpdateAsync(tenant);
        return Ok(tenant);
    }
}
