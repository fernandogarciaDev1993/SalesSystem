using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class ProductRepository : MongoRepository<Product>, IProductRepository
{
    public ProductRepository(IMongoDatabase db) : base(db, "products") { }

    public async Task<Product?> GetBySkuAsync(string sku, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(p => p.Sku, sku))).FirstOrDefaultAsync();

    public async Task<List<Product>> GetByRecipeInsumoAsync(string insumoId, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.ElemMatch(p => p.Recipe, r => r.InsumoId == insumoId))).ToListAsync();
}
