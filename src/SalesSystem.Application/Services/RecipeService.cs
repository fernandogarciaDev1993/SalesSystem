using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IRecipeService
{
    Task<Recipe?> GetByIdAsync(string id, string tenantId);
    Task<List<Recipe>> GetAllAsync(string tenantId);
    Task<ServiceResult<Recipe>> CreateAsync(CreateRecipeRequest request, string tenantId);
    Task<ServiceResult<Recipe>> UpdateAsync(string id, UpdateRecipeRequest request, string tenantId);
    Task<bool> DeleteAsync(string id, string tenantId);
    Task<ServiceResult<Recipe>> RecalculateCostAsync(string recipeId, string tenantId);
    Task<int> RecalculateByInsumoAsync(string insumoId, string tenantId);
    Task<int> RecalculateByRecipeAsync(string recipeId, string tenantId);
    Task<int> RecalculateAllAsync(string tenantId);
    Task<ServiceResult<Recipe>> DuplicateAsync(string recipeId, string tenantId);
    Task<List<IngredientOption>> GetIngredientOptionsAsync(string tenantId);
}

public class RecipeItemRequest
{
    public string InsumoId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public bool IsRecipe { get; set; }
}

public class RecipeStepRequest
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<RecipeItemRequest> Ingredients { get; set; } = [];
    public List<RecipeStepRequest> Steps { get; set; } = [];
    public int YieldQuantity { get; set; } = 1;
    public bool IsInsumo { get; set; }
    public string? OutputInsumoId { get; set; }
}

public class UpdateRecipeRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<RecipeItemRequest>? Ingredients { get; set; }
    public List<RecipeStepRequest>? Steps { get; set; }
    public int? YieldQuantity { get; set; }
    public bool? IsInsumo { get; set; }
    public string? OutputInsumoId { get; set; }
    public bool? IsActive { get; set; }
}

public class IngredientOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public decimal CostPerBaseUnit { get; set; }
    public decimal CostPerUserUnit { get; set; }
    public decimal AverageCost { get; set; }
    public string Type { get; set; } = "insumo";
}

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepo;
    private readonly IInsumoRepository _insumoRepo;

    public RecipeService(IRecipeRepository recipeRepo, IInsumoRepository insumoRepo)
    {
        _recipeRepo = recipeRepo;
        _insumoRepo = insumoRepo;
    }

    public async Task<Recipe?> GetByIdAsync(string id, string tenantId)
        => await _recipeRepo.GetByIdAsync(id, tenantId);

    public async Task<List<Recipe>> GetAllAsync(string tenantId)
        => await _recipeRepo.GetAllAsync(tenantId);

    public async Task<ServiceResult<Recipe>> CreateAsync(CreateRecipeRequest request, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<Recipe>.Fail("Nome da receita e obrigatorio.");

        if (request.YieldQuantity <= 0)
            request.YieldQuantity = 1;

        var all = await _recipeRepo.GetAllAsync(tenantId);
        var maxCode = all.Select(r => { var num = r.Code?.Replace("REC-",""); return int.TryParse(num, out var n) ? n : 0; }).DefaultIfEmpty(0).Max();

        // Auto-create insumo when IsInsumo is checked and no OutputInsumoId provided
        if (request.IsInsumo && string.IsNullOrEmpty(request.OutputInsumoId))
        {
            var autoInsumo = new Insumo
            {
                TenantId = tenantId,
                Code = $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Name = $"{request.Name} (producao)",
                Unit = InsumoUnit.UN,
                BaseUnit = "un",
                IsActive = true
            };
            var createdInsumo = await _insumoRepo.InsertAsync(autoInsumo);
            request.OutputInsumoId = createdInsumo.Id;
        }

        var recipe = new Recipe
        {
            TenantId = tenantId,
            Code = $"REC-{(maxCode + 1):D5}",
            Name = request.Name,
            Description = request.Description,
            Category = request.Category ?? string.Empty,
            YieldQuantity = request.YieldQuantity,
            IsInsumo = request.IsInsumo,
            OutputInsumoId = request.IsInsumo ? request.OutputInsumoId : null,
            Steps = request.Steps.Select(s => new RecipeStep { Order = s.Order, Description = s.Description }).ToList(),
            IsActive = true
        };

        var ingredientsResult = await BuildIngredients(request.Ingredients, tenantId);
        if (!ingredientsResult.Success)
            return ServiceResult<Recipe>.Fail(ingredientsResult.Error!);

        recipe.Ingredients = ingredientsResult.Data!;
        recipe.CalculatedCost = recipe.Ingredients.Sum(i => i.TotalCost);
        recipe.CostPerUnit = recipe.YieldQuantity > 0
            ? recipe.CalculatedCost / recipe.YieldQuantity
            : recipe.CalculatedCost;

        var created = await _recipeRepo.InsertAsync(recipe);
        return ServiceResult<Recipe>.Ok(created);
    }

    public async Task<ServiceResult<Recipe>> UpdateAsync(string id, UpdateRecipeRequest request, string tenantId)
    {
        var recipe = await _recipeRepo.GetByIdAsync(id, tenantId);
        if (recipe is null)
            return ServiceResult<Recipe>.Fail("Receita nao encontrada.");

        if (request.Name is not null) recipe.Name = request.Name;
        if (request.Description is not null) recipe.Description = request.Description;
        if (request.Category is not null) recipe.Category = request.Category;
        if (request.YieldQuantity.HasValue) recipe.YieldQuantity = request.YieldQuantity.Value > 0 ? request.YieldQuantity.Value : 1;
        if (request.IsInsumo.HasValue) recipe.IsInsumo = request.IsInsumo.Value;
        if (request.OutputInsumoId is not null)
            recipe.OutputInsumoId = string.IsNullOrEmpty(request.OutputInsumoId) ? null : request.OutputInsumoId;
        // Auto-create insumo when IsInsumo is checked and no OutputInsumoId set
        if (recipe.IsInsumo && string.IsNullOrEmpty(recipe.OutputInsumoId))
        {
            var autoInsumo = new Insumo
            {
                TenantId = tenantId,
                Code = $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Name = $"{recipe.Name} (producao)",
                Unit = InsumoUnit.UN,
                BaseUnit = "un",
                IsActive = true
            };
            var createdInsumo = await _insumoRepo.InsertAsync(autoInsumo);
            recipe.OutputInsumoId = createdInsumo.Id;
        }
        if (!recipe.IsInsumo) recipe.OutputInsumoId = null;
        if (request.IsActive.HasValue) recipe.IsActive = request.IsActive.Value;
        if (request.Steps is not null)
            recipe.Steps = request.Steps.Select(s => new RecipeStep { Order = s.Order, Description = s.Description }).ToList();

        if (request.Ingredients is not null)
        {
            var ingredientsResult = await BuildIngredients(request.Ingredients, tenantId);
            if (!ingredientsResult.Success)
                return ServiceResult<Recipe>.Fail(ingredientsResult.Error!);

            recipe.Ingredients = ingredientsResult.Data!;
        }

        recipe.CalculatedCost = recipe.Ingredients.Sum(i => i.TotalCost);
        recipe.CostPerUnit = recipe.YieldQuantity > 0
            ? recipe.CalculatedCost / recipe.YieldQuantity
            : recipe.CalculatedCost;

        await _recipeRepo.UpdateAsync(recipe);

        // Cascade: recalculate recipes that use this recipe as ingredient
        if (recipe.IsInsumo)
        {
            await RecalculateByRecipeAsync(recipe.Id, tenantId);
        }

        return ServiceResult<Recipe>.Ok(recipe);
    }

    public async Task<bool> DeleteAsync(string id, string tenantId)
        => await _recipeRepo.DeleteAsync(id, tenantId);

    public async Task<ServiceResult<Recipe>> RecalculateCostAsync(string recipeId, string tenantId)
    {
        var recipe = await _recipeRepo.GetByIdAsync(recipeId, tenantId);
        if (recipe is null)
            return ServiceResult<Recipe>.Fail("Receita nao encontrada.");

        await RecalculateIngredientCosts(recipe, tenantId);
        await _recipeRepo.UpdateAsync(recipe);
        return ServiceResult<Recipe>.Ok(recipe);
    }

    public async Task<int> RecalculateByInsumoAsync(string insumoId, string tenantId)
    {
        var recipes = await _recipeRepo.GetByIngredientInsumoAsync(insumoId, tenantId);
        var count = 0;
        foreach (var recipe in recipes)
        {
            await RecalculateIngredientCosts(recipe, tenantId);
            await _recipeRepo.UpdateAsync(recipe);
            count++;
        }
        return count;
    }

    public async Task<int> RecalculateByRecipeAsync(string recipeId, string tenantId)
    {
        // Find all recipes that use this recipe as an ingredient
        var dependents = await _recipeRepo.GetByIngredientRecipeAsync(recipeId, tenantId);
        var count = 0;
        foreach (var recipe in dependents)
        {
            await RecalculateIngredientCosts(recipe, tenantId);
            await _recipeRepo.UpdateAsync(recipe);
            count++;
        }
        return count;
    }

    public async Task<ServiceResult<Recipe>> DuplicateAsync(string recipeId, string tenantId)
    {
        var source = await _recipeRepo.GetByIdAsync(recipeId, tenantId);
        if (source is null)
            return ServiceResult<Recipe>.Fail("Receita nao encontrada.");

        // Generate new code
        var all = await _recipeRepo.GetAllAsync(tenantId);
        var maxCode = all
            .Select(r => { var num = r.Code?.Replace("REC-", ""); return int.TryParse(num, out var n) ? n : 0; })
            .DefaultIfEmpty(0)
            .Max();

        var duplicate = new Recipe
        {
            TenantId = tenantId,
            Code = $"REC-{(maxCode + 1):D5}",
            Name = $"{source.Name} (copia)",
            Description = source.Description,
            Category = source.Category,
            Ingredients = source.Ingredients.Select(i => new RecipeIngredient
            {
                InsumoId = i.InsumoId,
                InsumoName = i.InsumoName,
                InsumoCode = i.InsumoCode,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitCost = i.UnitCost,
                TotalCost = i.TotalCost,
                IsRecipe = i.IsRecipe
            }).ToList(),
            Steps = source.Steps.Select(s => new RecipeStep
            {
                Order = s.Order,
                Description = s.Description
            }).ToList(),
            CalculatedCost = source.CalculatedCost,
            YieldQuantity = source.YieldQuantity,
            CostPerUnit = source.CostPerUnit,
            IsInsumo = false, // duplicate starts as non-insumo
            IsActive = true
        };

        var created = await _recipeRepo.InsertAsync(duplicate);
        return ServiceResult<Recipe>.Ok(created);
    }

    public async Task<int> RecalculateAllAsync(string tenantId)
    {
        var allRecipes = await _recipeRepo.GetAllAsync(tenantId);
        var activeRecipes = allRecipes.Where(r => r.IsActive && r.Ingredients.Count > 0).ToList();
        if (activeRecipes.Count == 0) return 0;

        var count = 0;
        foreach (var recipe in activeRecipes)
        {
            await RecalculateIngredientCosts(recipe, tenantId);
            await _recipeRepo.UpdateAsync(recipe);
            count++;
        }
        return count;
    }

    public async Task<List<IngredientOption>> GetIngredientOptionsAsync(string tenantId)
    {
        var options = new List<IngredientOption>();

        // Add regular insumos (exclude auto-created production insumos)
        var insumos = await _insumoRepo.GetAllAsync(tenantId);
        foreach (var insumo in insumos.Where(i => i.IsActive && !i.Code.StartsWith("AUTO-")))
        {
            var factor = InsumoUnitHelper.Factor(insumo.Unit);
            options.Add(new IngredientOption
            {
                Id = insumo.Id,
                Name = insumo.Name,
                Code = insumo.Code,
                Unit = insumo.Unit.ToString(),
                BaseUnit = InsumoUnitHelper.BaseUnitName(insumo.Unit),
                CostPerBaseUnit = insumo.AverageCost,
                CostPerUserUnit = insumo.AverageCost * factor,
                AverageCost = insumo.AverageCost,
                Type = "insumo"
            });
        }

        // Add recipes marked as insumo (these are sub-recipes, user selects these)
        var recipeInsumos = await _recipeRepo.GetInsumosAsync(tenantId);
        foreach (var recipe in recipeInsumos)
        {
            options.Add(new IngredientOption
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Code = $"R{recipe.Id[^4..]}",
                Unit = "UN",
                BaseUnit = "un",
                CostPerBaseUnit = recipe.CostPerUnit,
                CostPerUserUnit = recipe.CostPerUnit,
                AverageCost = recipe.CostPerUnit,
                Type = "recipe"
            });
        }

        return options;
    }

    private async Task<ServiceResult<List<RecipeIngredient>>> BuildIngredients(List<RecipeItemRequest> items, string tenantId)
    {
        var ingredients = new List<RecipeIngredient>();

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return ServiceResult<List<RecipeIngredient>>.Fail("Todas as quantidades devem ser maiores que zero.");

            if (item.IsRecipe)
            {
                var sourceRecipe = await _recipeRepo.GetByIdAsync(item.InsumoId, tenantId);
                if (sourceRecipe is null)
                    return ServiceResult<List<RecipeIngredient>>.Fail($"Receita '{item.InsumoId}' nao encontrada.");

                ingredients.Add(new RecipeIngredient
                {
                    InsumoId = sourceRecipe.Id,
                    InsumoName = sourceRecipe.Name,
                    InsumoCode = $"R{sourceRecipe.Id[^4..]}",
                    Quantity = item.Quantity,
                    Unit = "UN",
                    UnitCost = sourceRecipe.CostPerUnit,
                    TotalCost = item.Quantity * sourceRecipe.CostPerUnit,
                    IsRecipe = true
                });
            }
            else
            {
                var insumo = await _insumoRepo.GetByIdAsync(item.InsumoId, tenantId);
                if (insumo is null)
                    return ServiceResult<List<RecipeIngredient>>.Fail($"Insumo '{item.InsumoId}' nao encontrado.");

                // Quantity is in base unit (g, ml, un). Cost is per base unit.
                ingredients.Add(new RecipeIngredient
                {
                    InsumoId = insumo.Id,
                    InsumoName = insumo.Name,
                    InsumoCode = insumo.Code,
                    Quantity = item.Quantity,
                    Unit = InsumoUnitHelper.BaseUnitName(insumo.Unit),
                    UnitCost = insumo.AverageCost,
                    TotalCost = item.Quantity * insumo.AverageCost,
                    IsRecipe = false
                });
            }
        }

        return ServiceResult<List<RecipeIngredient>>.Ok(ingredients);
    }

    private async Task RecalculateIngredientCosts(Recipe recipe, string tenantId)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.IsRecipe)
            {
                var sourceRecipe = await _recipeRepo.GetByIdAsync(ingredient.InsumoId, tenantId);
                if (sourceRecipe is not null)
                {
                    ingredient.UnitCost = sourceRecipe.CostPerUnit;
                    ingredient.TotalCost = ingredient.Quantity * sourceRecipe.CostPerUnit;
                    ingredient.InsumoName = sourceRecipe.Name;
                }
            }
            else
            {
                var insumo = await _insumoRepo.GetByIdAsync(ingredient.InsumoId, tenantId);
                if (insumo is not null)
                {
                    ingredient.UnitCost = insumo.AverageCost; // cost per base unit (g, ml, un)
                    ingredient.TotalCost = ingredient.Quantity * insumo.AverageCost;
                    ingredient.InsumoName = insumo.Name;
                    ingredient.Unit = InsumoUnitHelper.BaseUnitName(insumo.Unit);
                }
            }
        }

        recipe.CalculatedCost = recipe.Ingredients.Sum(i => i.TotalCost);
        recipe.CostPerUnit = recipe.YieldQuantity > 0
            ? recipe.CalculatedCost / recipe.YieldQuantity
            : recipe.CalculatedCost;
    }
}
