using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class InsumoRepository : MongoRepository<Insumo>, IInsumoRepository
{
    public InsumoRepository(IMongoDatabase db) : base(db, "insumos") { }

    public async Task<Insumo?> GetByCodeAsync(string code, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(i => i.Code, code))).FirstOrDefaultAsync();
}

public class InsumoPurchaseRepository : MongoRepository<InsumoPurchase>, IInsumoPurchaseRepository
{
    public InsumoPurchaseRepository(IMongoDatabase db) : base(db, "insumo_purchases") { }

    public async Task<List<InsumoPurchase>> GetByInsumoAsync(string insumoId, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(p => p.InsumoId, insumoId)))
            .SortByDescending(p => p.CreatedAt).ToListAsync();
}
