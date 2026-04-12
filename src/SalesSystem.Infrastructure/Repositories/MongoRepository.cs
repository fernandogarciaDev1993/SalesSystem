using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public abstract class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _col;
    protected FilterDefinitionBuilder<T> F => Builders<T>.Filter;
    protected UpdateDefinitionBuilder<T> U => Builders<T>.Update;

    protected MongoRepository(IMongoDatabase db, string collectionName)
        => _col = db.GetCollection<T>(collectionName);

    protected FilterDefinition<T> TenantFilter(string tenantId)
        => F.Eq(e => e.TenantId, tenantId);

    public async Task<T?> GetByIdAsync(string id, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(e => e.Id, id))).FirstOrDefaultAsync();

    public async Task<List<T>> GetAllAsync(string tenantId)
        => await _col.Find(TenantFilter(tenantId)).ToListAsync();

    public async Task<List<T>> GetAllAsync(string tenantId, FilterDefinition<T>? filter)
    {
        var combined = filter is null ? TenantFilter(tenantId) : F.And(TenantFilter(tenantId), filter);
        return await _col.Find(combined).ToListAsync();
    }

    public async Task<T> InsertAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _col.InsertOneAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await _col.ReplaceOneAsync(F.And(TenantFilter(entity.TenantId), F.Eq(e => e.Id, entity.Id)), entity);
    }

    public async Task<bool> DeleteAsync(string id, string tenantId)
    {
        var result = await _col.DeleteOneAsync(F.And(TenantFilter(tenantId), F.Eq(e => e.Id, id)));
        return result.DeletedCount > 0;
    }
}
