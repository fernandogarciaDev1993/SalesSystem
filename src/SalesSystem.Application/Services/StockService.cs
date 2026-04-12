using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IStockService
{
    Task<StockBalance?> GetBalanceByProductAsync(string productId, string tenantId);
    Task<List<StockBalance>> GetAllBalancesAsync(string tenantId);
    Task<List<StockMove>> GetMovesAsync(string productId, string tenantId);
    Task<ServiceResult<StockMove>> AddMoveAsync(StockMove move);
}

public class StockBalanceDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal MinBalance { get; set; }
    public decimal AverageCost { get; set; }
}

public class StockMoveDto
{
    public string ProductId { get; set; } = string.Empty;
    public StockMoveType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }
}

public class StockService : IStockService
{
    private readonly IStockBalanceRepository _balanceRepo;
    private readonly IStockMoveRepository _moveRepo;

    public StockService(IStockBalanceRepository balanceRepo, IStockMoveRepository moveRepo)
    {
        _balanceRepo = balanceRepo;
        _moveRepo = moveRepo;
    }

    public async Task<StockBalance?> GetBalanceByProductAsync(string productId, string tenantId)
        => await _balanceRepo.GetByProductAsync(productId, tenantId);

    public async Task<List<StockBalance>> GetAllBalancesAsync(string tenantId)
        => await _balanceRepo.GetAllAsync(tenantId);

    public async Task<List<StockMove>> GetMovesAsync(string productId, string tenantId)
        => await _moveRepo.GetByProductAsync(productId, tenantId);

    public async Task<ServiceResult<StockMove>> AddMoveAsync(StockMove move)
    {
        if (move.Quantity <= 0)
            return ServiceResult<StockMove>.Fail("Quantity must be greater than zero.");

        var existing = await _balanceRepo.GetByProductAsync(move.ProductId, move.TenantId);
        var isNewBalance = existing is null;

        var balance = existing ?? new StockBalance
        {
            TenantId = move.TenantId,
            ProductId = move.ProductId,
            ProductName = move.ProductName,
            ProductSku = move.ProductSku,
            CurrentBalance = 0,
            ReservedBalance = 0,
            AvailableBalance = 0,
            AverageCost = 0
        };

        move.PreviousBalance = balance.CurrentBalance;

        switch (move.Type)
        {
            case StockMoveType.Entrada:
            case StockMoveType.Devolucao:
                // Recalculate average cost on entry
                var totalCostBefore = balance.CurrentBalance * balance.AverageCost;
                var totalCostIncoming = move.Quantity * move.UnitCost;
                var newTotal = balance.CurrentBalance + move.Quantity;

                balance.AverageCost = newTotal > 0
                    ? (totalCostBefore + totalCostIncoming) / newTotal
                    : 0;

                balance.CurrentBalance += move.Quantity;
                break;

            case StockMoveType.Saida:
                if (balance.CurrentBalance < move.Quantity)
                    return ServiceResult<StockMove>.Fail("Insufficient stock balance.");

                balance.CurrentBalance -= move.Quantity;
                break;

            case StockMoveType.Ajuste:
                balance.CurrentBalance += move.Quantity;
                break;

            case StockMoveType.Reserva:
                if (balance.AvailableBalance < move.Quantity)
                    return ServiceResult<StockMove>.Fail("Insufficient available stock for reservation.");

                balance.ReservedBalance += move.Quantity;
                break;

            case StockMoveType.Liberacao:
                if (balance.ReservedBalance < move.Quantity)
                    return ServiceResult<StockMove>.Fail("Cannot release more than reserved amount.");

                balance.ReservedBalance -= move.Quantity;
                break;
        }

        balance.AvailableBalance = balance.CurrentBalance - balance.ReservedBalance;
        move.NewBalance = balance.CurrentBalance;
        move.TotalCost = move.Quantity * move.UnitCost;

        if (isNewBalance)
            await _balanceRepo.InsertAsync(balance);
        else
            await _balanceRepo.UpdateAsync(balance);

        var createdMove = await _moveRepo.InsertAsync(move);
        return ServiceResult<StockMove>.Ok(createdMove);
    }
}
