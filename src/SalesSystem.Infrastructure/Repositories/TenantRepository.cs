using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly IMongoCollection<Tenant> _col;

    public TenantRepository(IMongoDatabase db)
        => _col = db.GetCollection<Tenant>("tenants");

    public async Task<Tenant?> GetByIdAsync(string id)
        => await _col.Find(t => t.Id == id).FirstOrDefaultAsync();

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
        => await _col.Find(t => t.Subdomain == subdomain).FirstOrDefaultAsync();

    public async Task<List<Tenant>> GetAllAsync()
        => await _col.Find(_ => true).ToListAsync();

    public async Task<Tenant> InsertAsync(Tenant tenant)
    {
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _col.InsertOneAsync(tenant);
        return tenant;
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        tenant.UpdatedAt = DateTime.UtcNow;
        await _col.ReplaceOneAsync(t => t.Id == tenant.Id, tenant);
    }
}
