using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class RecipeRepository : MongoRepository<Recipe>, IRecipeRepository
{
    public RecipeRepository(IMongoDatabase db) : base(db, "recipes") { }

    public async Task<List<Recipe>> GetInsumosAsync(string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(r => r.IsInsumo, true), F.Eq(r => r.IsActive, true))).ToListAsync();

    public async Task<List<Recipe>> GetByIngredientInsumoAsync(string insumoId, string tenantId)
        => await _col.Find(F.And(
            TenantFilter(tenantId),
            F.ElemMatch(r => r.Ingredients, i => i.InsumoId == insumoId && !i.IsRecipe)
        )).ToListAsync();

    public async Task<List<Recipe>> GetByIngredientRecipeAsync(string recipeId, string tenantId)
        => await _col.Find(F.And(
            TenantFilter(tenantId),
            F.ElemMatch(r => r.Ingredients, i => i.InsumoId == recipeId && i.IsRecipe)
        )).ToListAsync();
}
