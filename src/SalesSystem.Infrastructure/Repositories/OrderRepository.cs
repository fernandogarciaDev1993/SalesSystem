using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class OrderRepository : MongoRepository<Order>, IOrderRepository
{
    public OrderRepository(IMongoDatabase db) : base(db, "orders") { }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(o => o.OrderNumber, orderNumber))).FirstOrDefaultAsync();

    public async Task<long> CountByStatusAsync(OrderStatus status, string tenantId)
        => await _col.CountDocumentsAsync(F.And(TenantFilter(tenantId), F.Eq(o => o.Status, status)));
}
