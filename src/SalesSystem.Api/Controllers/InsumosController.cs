using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsumosController : ControllerBase
{
    private readonly IInsumoService _insumoService;
    private readonly IRecipeService _recipeService;

    public InsumosController(IInsumoService insumoService, IRecipeService recipeService)
    {
        _insumoService = insumoService;
        _recipeService = recipeService;
    }

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _insumoService.GetAllAsync(TenantId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var insumo = await _insumoService.GetByIdAsync(id, TenantId);
        return insumo is null ? NotFound() : Ok(insumo);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInsumoRequest request)
    {
        var result = await _insumoService.CreateAsync(request, TenantId);
        return result.Success ? Created($"/api/insumos/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateInsumoRequest request)
    {
        var result = await _insumoService.UpdateAsync(id, request, TenantId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _insumoService.DeleteAsync(id, TenantId);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/purchases")]
    public async Task<IActionResult> RegisterPurchase(string id, [FromBody] CreatePurchaseRequest request)
    {
        request.InsumoId = id;
        var result = await _insumoService.RegisterPurchaseAsync(request, TenantId);
        return result.Success ? Created("", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id}/purchases")]
    public async Task<IActionResult> GetPurchases(string id)
        => Ok(await _insumoService.GetPurchasesAsync(id, TenantId));

    [HttpDelete("{id}/purchases/{purchaseId}")]
    public async Task<IActionResult> DeletePurchase(string id, string purchaseId)
    {
        var result = await _insumoService.DeletePurchaseAsync(id, purchaseId, TenantId);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }
}
