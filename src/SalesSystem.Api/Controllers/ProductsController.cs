using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _productService.GetAllAsync(TenantId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var product = await _productService.GetByIdAsync(id, TenantId);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await _productService.CreateAsync(request, TenantId);
        return result.Success ? Created($"/api/products/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProductRequest request)
    {
        var result = await _productService.UpdateAsync(id, request, TenantId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _productService.DeleteAsync(id, TenantId);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/produce")]
    public async Task<IActionResult> Produce(string id, [FromBody] ProduceRequest request,
        [FromServices] IRecipeService recipeService,
        [FromServices] IInsumoService insumoService,
        [FromServices] IStockService stockService)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { error = "Quantidade deve ser maior que zero." });

        var product = await _productService.GetByIdAsync(id, TenantId);
        if (product is null)
            return NotFound(new { error = "Produto nao encontrado." });

        // Try to load recipe from recipeId first, fall back to inline recipe
        Domain.Entities.Recipe? recipe = null;
        if (!string.IsNullOrEmpty(product.RecipeId))
        {
            recipe = await recipeService.GetByIdAsync(product.RecipeId, TenantId);
        }

        if (recipe is not null && recipe.Ingredients.Count > 0)
        {
            // Use the Recipe entity — quantities are in base units (g, ml, un)
            foreach (var item in recipe.Ingredients)
            {
                if (item.IsRecipe) continue; // sub-recipes: for now skip

                var baseQty = (long)(item.Quantity * request.Quantity);
                var consumeResult = await insumoService.ConsumeBaseAsync(
                    item.InsumoId,
                    baseQty,
                    TenantId,
                    $"Producao de {request.Quantity}x {product.Name}");

                if (!consumeResult.Success)
                    return BadRequest(new { error = consumeResult.Error });
            }

            var unitCost = recipe.CostPerUnit;
            var stockMove = new Domain.Entities.StockMove
            {
                TenantId = TenantId,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Type = Domain.Entities.StockMoveType.Entrada,
                Quantity = request.Quantity,
                UnitCost = unitCost,
                ReferenceId = product.Id,
                ReferenceType = "Production",
                Note = $"Producao de {request.Quantity} unidade(s)",
                UserId = "system"
            };

            var result = await stockService.AddMoveAsync(stockMove);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = $"Producao realizada: {request.Quantity} unidade(s) de '{product.Name}'", stockMove = result.Data });
        }
        else if (product.HasRecipe && product.Recipe.Count > 0)
        {
            // Legacy: inline recipe on product
            foreach (var item in product.Recipe)
            {
                var consumeResult = await insumoService.ConsumeAsync(
                    item.InsumoId,
                    item.Quantity * request.Quantity,
                    TenantId,
                    $"Producao de {request.Quantity}x {product.Name}");

                if (!consumeResult.Success)
                    return BadRequest(new { error = consumeResult.Error });
            }

            var stockMove = new Domain.Entities.StockMove
            {
                TenantId = TenantId,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Type = Domain.Entities.StockMoveType.Entrada,
                Quantity = request.Quantity,
                UnitCost = product.CalculatedCost,
                ReferenceId = product.Id,
                ReferenceType = "Production",
                Note = $"Producao de {request.Quantity} unidade(s)",
                UserId = "system"
            };

            var result = await stockService.AddMoveAsync(stockMove);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = $"Producao realizada: {request.Quantity} unidade(s) de '{product.Name}'", stockMove = result.Data });
        }
        else
        {
            return BadRequest(new { error = "Produto nao possui receita cadastrada." });
        }
    }
}

public class ProduceRequest
{
    public int Quantity { get; set; }
}
