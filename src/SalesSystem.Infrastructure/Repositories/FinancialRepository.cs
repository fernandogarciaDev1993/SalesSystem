using MongoDB.Driver;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Repositories;

public class FinancialRepository : MongoRepository<FinancialEntry>, IFinancialRepository
{
    public FinancialRepository(IMongoDatabase db) : base(db, "financials") { }

    public async Task<List<FinancialEntry>> GetByPeriodAsync(string tenantId, DateTime from, DateTime to)
        => await _col.Find(F.And(
            TenantFilter(tenantId),
            F.Gte(e => e.DueDate, from),
            F.Lte(e => e.DueDate, to)
        )).SortBy(e => e.DueDate).ToListAsync();

    public async Task<List<FinancialEntry>> GetPendingAsync(string tenantId)
        => await _col.Find(F.And(
            TenantFilter(tenantId),
            F.Eq(e => e.Status, FinancialStatus.Pendente)
        )).ToListAsync();

    public async Task<FinancialEntry?> GetByReferenceAsync(string referenceId, string referenceType, string tenantId)
        => await _col.Find(F.And(
            TenantFilter(tenantId),
            F.Eq(e => e.ReferenceId, referenceId),
            F.Eq(e => e.ReferenceType, referenceType)
        )).FirstOrDefaultAsync();
}
