using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(IMongoDatabase db) : base(db, "users") { }

    public async Task<User?> GetByEmailAsync(string email, string tenantId)
        => await _col.Find(F.And(TenantFilter(tenantId), F.Eq(u => u.Email, email))).FirstOrDefaultAsync();
}
