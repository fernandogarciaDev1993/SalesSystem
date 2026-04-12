using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class CustomerRepository : MongoRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(IMongoDatabase db) : base(db, "customers") { }

    public async Task<Customer?> GetByDocumentAsync(string document, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(c => c.Document, document))).FirstOrDefaultAsync();
}
