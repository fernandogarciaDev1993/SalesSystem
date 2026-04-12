using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IFinancialService
{
    Task<FinancialEntry?> GetByIdAsync(string id, string tenantId);
    Task<List<FinancialEntry>> GetAllAsync(string tenantId);
    Task<ServiceResult<FinancialEntry>> CreateAsync(FinancialEntry entry);
    Task<ServiceResult<FinancialEntry>> UpdateAsync(FinancialEntry entry);
    Task<FinancialSummaryDto> GetSummaryAsync(string tenantId, DateTime from, DateTime to);
    Task<List<FinancialEntry>> GetByPeriodAsync(string tenantId, DateTime from, DateTime to);
}

public class FinancialSummaryDto
{
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public int Pendentes { get; set; }
    public int Vencidos { get; set; }
}

public class FinancialService : IFinancialService
{
    private readonly IFinancialRepository _repo;

    public FinancialService(IFinancialRepository repo)
    {
        _repo = repo;
    }

    public async Task<FinancialEntry?> GetByIdAsync(string id, string tenantId)
        => await _repo.GetByIdAsync(id, tenantId);

    public async Task<List<FinancialEntry>> GetAllAsync(string tenantId)
        => await _repo.GetAllAsync(tenantId);

    public async Task<ServiceResult<FinancialEntry>> CreateAsync(FinancialEntry entry)
    {
        if (entry.Amount <= 0)
            return ServiceResult<FinancialEntry>.Fail("Amount must be greater than zero.");

        var all = await _repo.GetAllAsync(entry.TenantId);
        var maxCode = all.Select(f => { var num = f.Code?.Replace("FIN-",""); return int.TryParse(num, out var n) ? n : 0; }).DefaultIfEmpty(0).Max();
        entry.Code = $"FIN-{(maxCode + 1):D5}";

        entry.AmountDue = entry.Amount - entry.AmountPaid;

        var created = await _repo.InsertAsync(entry);
        return ServiceResult<FinancialEntry>.Ok(created);
    }

    public async Task<ServiceResult<FinancialEntry>> UpdateAsync(FinancialEntry entry)
    {
        var existing = await _repo.GetByIdAsync(entry.Id, entry.TenantId);
        if (existing is null)
            return ServiceResult<FinancialEntry>.Fail("Financial entry not found.");

        entry.AmountDue = entry.Amount - entry.AmountPaid;

        if (entry.AmountPaid >= entry.Amount)
        {
            entry.Status = FinancialStatus.Pago;
            entry.PaymentDate ??= DateTime.UtcNow;
        }
        else if (entry.DueDate < DateTime.UtcNow && entry.Status == FinancialStatus.Pendente)
        {
            entry.Status = FinancialStatus.Vencido;
        }

        await _repo.UpdateAsync(entry);
        return ServiceResult<FinancialEntry>.Ok(entry);
    }

    public async Task<List<FinancialEntry>> GetByPeriodAsync(string tenantId, DateTime from, DateTime to)
        => await _repo.GetByPeriodAsync(tenantId, from, to);

    public async Task<FinancialSummaryDto> GetSummaryAsync(string tenantId, DateTime from, DateTime to)
    {
        var entries = await _repo.GetByPeriodAsync(tenantId, from, to);

        var summary = new FinancialSummaryDto
        {
            TotalReceitas = entries
                .Where(e => e.Type == FinancialType.Receita)
                .Sum(e => e.Amount),
            TotalDespesas = entries
                .Where(e => e.Type == FinancialType.Despesa)
                .Sum(e => e.Amount),
            Pendentes = entries.Count(e => e.Status == FinancialStatus.Pendente),
            Vencidos = entries.Count(e => e.Status == FinancialStatus.Vencido)
        };

        summary.Saldo = summary.TotalReceitas - summary.TotalDespesas;

        return summary;
    }
}
