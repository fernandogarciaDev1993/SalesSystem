using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class StockBalanceRepository : MongoRepository<StockBalance>, IStockBalanceRepository
{
    public StockBalanceRepository(IMongoDatabase db) : base(db, "stock_balances") { }

    public async Task<StockBalance?> GetByProductAsync(string productId, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(s => s.ProductId, productId))).FirstOrDefaultAsync();
}

public class StockMoveRepository : MongoRepository<StockMove>, IStockMoveRepository
{
    public StockMoveRepository(IMongoDatabase db) : base(db, "stock_moves") { }

    public async Task<List<StockMove>> GetByProductAsync(string productId, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(m => m.ProductId, productId)))
            .SortByDescending(m => m.CreatedAt).ToListAsync();
}
