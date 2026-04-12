using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService) => _recipeService = recipeService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _recipeService.GetAllAsync(TenantId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var recipe = await _recipeService.GetByIdAsync(id, TenantId);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeRequest request)
    {
        var result = await _recipeService.CreateAsync(request, TenantId);
        return result.Success ? Created($"/api/recipes/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRecipeRequest request)
    {
        var result = await _recipeService.UpdateAsync(id, request, TenantId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _recipeService.DeleteAsync(id, TenantId);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/recalculate")]
    public async Task<IActionResult> Recalculate(string id)
    {
        var result = await _recipeService.RecalculateCostAsync(id, TenantId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("recalculate-all")]
    public async Task<IActionResult> RecalculateAll()
    {
        var count = await _recipeService.RecalculateAllAsync(TenantId);
        return Ok(new { updatedRecipes = count });
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(string id)
    {
        var result = await _recipeService.DuplicateAsync(id, TenantId);
        return result.Success ? Created($"/api/recipes/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("ingredients")]
    public async Task<IActionResult> GetIngredientOptions()
        => Ok(await _recipeService.GetIngredientOptionsAsync(TenantId));

    [HttpPost("{id}/produce")]
    public async Task<IActionResult> Produce(string id, [FromBody] ProduceRecipeRequest request,
        [FromServices] IInsumoService insumoService,
        [FromServices] IProductService productService,
        [FromServices] IStockService stockService,
        [FromServices] Application.Interfaces.IInsumoRepository insumoRepo)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { error = "Quantidade deve ser maior que zero." });

        var recipe = await _recipeService.GetByIdAsync(id, TenantId);
        if (recipe is null)
            return NotFound(new { error = "Receita nao encontrada." });
        if (recipe.Ingredients.Count == 0)
            return BadRequest(new { error = "Receita nao possui ingredientes." });

        // VALIDATION PASS - check all ingredients have sufficient stock
        foreach (var item in recipe.Ingredients)
        {
            if (item.IsRecipe)
            {
                var subRecipe = await _recipeService.GetByIdAsync(item.InsumoId, TenantId);
                if (subRecipe is null)
                    return BadRequest(new { error = $"Receita '{item.InsumoName}' nao encontrada." });
                if (string.IsNullOrEmpty(subRecipe.OutputInsumoId))
                    return BadRequest(new { error = $"Receita '{subRecipe.Name}' precisa ser produzida primeiro. Va em Producao e produza esta receita." });

                var neededUnits = (long)(item.Quantity * request.Quantity);
                var outputInsumo = await insumoService.GetByIdAsync(subRecipe.OutputInsumoId, TenantId);
                if (outputInsumo is null)
                    return BadRequest(new { error = $"Insumo de saida da receita '{subRecipe.Name}' nao encontrado." });
                if (outputInsumo.CurrentStock < neededUnits)
                    return BadRequest(new { error = $"Estoque insuficiente de '{subRecipe.Name}'. Necessario: {neededUnits} un, Disponivel: {outputInsumo.CurrentStock} un. Produza mais unidades desta receita primeiro." });
                continue;
            }

            var insumo = await insumoService.GetByIdAsync(item.InsumoId, TenantId);
            if (insumo is null)
                return BadRequest(new { error = $"Insumo '{item.InsumoName}' nao encontrado." });

            var needed = (long)(item.Quantity * request.Quantity);
            if (insumo.CurrentStock < needed)
            {
                var factor = Domain.Entities.InsumoUnitHelper.Factor(insumo.Unit);
                var neededDisplay = factor > 1 ? $"{(decimal)needed / factor:N2} {insumo.Unit}" : $"{needed} {insumo.BaseUnit}";
                var availableDisplay = factor > 1 ? $"{(decimal)insumo.CurrentStock / factor:N2} {insumo.Unit}" : $"{insumo.CurrentStock} {insumo.BaseUnit}";
                return BadRequest(new { error = $"Estoque insuficiente de '{insumo.Name}'. Necessario: {neededDisplay}, Disponivel: {availableDisplay}" });
            }
        }

        // CONSUMPTION PASS - only runs if all validations passed
        foreach (var item in recipe.Ingredients)
        {
            if (item.IsRecipe)
            {
                var subRecipe = await _recipeService.GetByIdAsync(item.InsumoId, TenantId);
                var neededUnits = (long)(item.Quantity * request.Quantity);
                var consumeResult = await insumoService.ConsumeBaseAsync(
                    subRecipe!.OutputInsumoId!, neededUnits, TenantId,
                    $"Consumo para producao de '{recipe.Name}'");
                if (!consumeResult.Success)
                    return BadRequest(new { error = consumeResult.Error });
                continue;
            }

            var baseQty = (long)(item.Quantity * request.Quantity);
            var consumeResult2 = await insumoService.ConsumeBaseAsync(
                item.InsumoId, baseQty, TenantId,
                $"Producao de {request.Quantity} lote(s) de '{recipe.Name}'");

            if (!consumeResult2.Success)
                return BadRequest(new { error = consumeResult2.Error });
        }

        var totalUnits = request.Quantity * recipe.YieldQuantity;

        if (recipe.IsInsumo)
        {
            // Output goes to insumo stock
            // Auto-create output insumo if not set
            if (string.IsNullOrEmpty(recipe.OutputInsumoId))
            {
                var allInsumos = await insumoService.GetAllAsync(TenantId);
                var maxCode = allInsumos
                    .Select(i => int.TryParse(i.Code, out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                var autoInsumo = new Domain.Entities.Insumo
                {
                    TenantId = TenantId,
                    Code = (maxCode + 1).ToString("D5"),
                    Name = $"{recipe.Name} (producao)",
                    Unit = Domain.Entities.InsumoUnit.UN,
                    BaseUnit = "un",
                    IsActive = true
                };
                var created = await insumoRepo.InsertAsync(autoInsumo);
                recipe.OutputInsumoId = created.Id;
                await _recipeService.UpdateAsync(recipe.Id, new UpdateRecipeRequest { OutputInsumoId = created.Id }, TenantId);
            }

            var targetInsumo = await insumoRepo.GetByIdAsync(recipe.OutputInsumoId, TenantId);
            if (targetInsumo is null)
                return BadRequest(new { error = "Insumo de saida nao encontrado." });

            targetInsumo.CurrentStock += totalUnits;
            await insumoRepo.UpdateAsync(targetInsumo);

            return Ok(new
            {
                message = $"Producao realizada: {totalUnits} unidade(s) adicionadas ao insumo '{targetInsumo.Name}'",
                totalUnits,
                totalCost = recipe.CalculatedCost * request.Quantity,
                outputType = "insumo",
                outputName = targetInsumo.Name
            });
        }
        else
        {
            // Output goes to product stock - find product linked to this recipe
            var productId = request.ProductId;
            if (string.IsNullOrEmpty(productId))
            {
                // Auto-find product linked to this recipe
                var allProducts = await productService.GetAllAsync(TenantId);
                var linkedProduct = allProducts.FirstOrDefault(p => p.RecipeId == recipe.Id);
                if (linkedProduct is not null)
                    productId = linkedProduct.Id;
            }

            if (string.IsNullOrEmpty(productId))
                return BadRequest(new { error = "Nenhum produto vinculado a esta receita. Crie um produto e vincule esta receita." });

            var product = await productService.GetByIdAsync(productId, TenantId);
            if (product is null)
                return BadRequest(new { error = "Produto nao encontrado." });

            var stockMove = new Domain.Entities.StockMove
            {
                TenantId = TenantId,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Type = Domain.Entities.StockMoveType.Entrada,
                Quantity = totalUnits,
                UnitCost = recipe.CostPerUnit,
                ReferenceId = recipe.Id,
                ReferenceType = "Production",
                Note = $"Producao de {request.Quantity} lote(s) de '{recipe.Name}'",
                UserId = "system"
            };

            var result = await stockService.AddMoveAsync(stockMove);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new
            {
                message = $"Producao realizada: {totalUnits} unidade(s) de '{product.Name}' adicionadas ao estoque",
                totalUnits,
                totalCost = recipe.CalculatedCost * request.Quantity,
                outputType = "product",
                outputName = product.Name,
                stockMove = result.Data
            });
        }
    }
}

public class ProduceRecipeRequest
{
    public int Quantity { get; set; }
    public string? ProductId { get; set; }
}
