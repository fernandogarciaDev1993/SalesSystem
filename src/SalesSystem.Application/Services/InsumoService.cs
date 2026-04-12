using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IInsumoService
{
    Task<Insumo?> GetByIdAsync(string id, string tenantId);
    Task<List<Insumo>> GetAllAsync(string tenantId);
    Task<ServiceResult<Insumo>> CreateAsync(CreateInsumoRequest request, string tenantId);
    Task<ServiceResult<Insumo>> UpdateAsync(string id, UpdateInsumoRequest request, string tenantId);
    Task<bool> DeleteAsync(string id, string tenantId);
    Task<ServiceResult<InsumoPurchase>> RegisterPurchaseAsync(CreatePurchaseRequest request, string tenantId);
    Task<ServiceResult<bool>> DeletePurchaseAsync(string insumoId, string purchaseId, string tenantId);
    Task<List<InsumoPurchase>> GetPurchasesAsync(string insumoId, string tenantId);
    Task<ServiceResult<Insumo>> ConsumeAsync(string insumoId, decimal quantityInUserUnit, string tenantId, string note);
    Task<ServiceResult<Insumo>> ConsumeBaseAsync(string insumoId, long baseQuantity, string tenantId, string note);
}

/// <summary>
/// Quantidade e custo na unidade do usuario (ex: 5 KG a R$3.00/KG).
/// O servico converte para unidade-base internamente (ex: 5000 g a R$0.003/g).
/// </summary>
public class CreateInsumoRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InsumoUnit Unit { get; set; } = InsumoUnit.KG;
    public decimal MinStock { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal InitialUnitCost { get; set; }
    public int InitialPackageSize { get; set; }
    public bool IsSellable { get; set; }
    public decimal SalePrice { get; set; }
}

public class UpdateInsumoRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? MinStock { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Quantity/UnitCost na unidade do usuario (KG, LT, UN...).
/// Se PackageSize > 0, indica compra por embalagem:
///   Quantity = qtde de embalagens, PackageSize = unidades por embalagem, UnitCost = custo por embalagem.
///   Ex: 1 cartela de ovos (20 un) por R$12 → Quantity=1, PackageSize=20, UnitCost=12.
/// </summary>
public class CreatePurchaseRequest
{
    public string InsumoId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public int PackageSize { get; set; }
    public string? Note { get; set; }
}

public class InsumoService : IInsumoService
{
    private readonly IInsumoRepository _repo;
    private readonly IInsumoPurchaseRepository _purchaseRepo;
    private readonly IRecipeService _recipeService;
    private readonly IProductRepository _productRepo;
    private readonly IFinancialRepository _financialRepo;
    private readonly IStockBalanceRepository _stockBalanceRepo;
    private readonly IStockMoveRepository _stockMoveRepo;

    public InsumoService(IInsumoRepository repo, IInsumoPurchaseRepository purchaseRepo, IRecipeService recipeService, IProductRepository productRepo, IFinancialRepository financialRepo, IStockBalanceRepository stockBalanceRepo, IStockMoveRepository stockMoveRepo)
    {
        _repo = repo;
        _purchaseRepo = purchaseRepo;
        _recipeService = recipeService;
        _productRepo = productRepo;
        _financialRepo = financialRepo;
        _stockBalanceRepo = stockBalanceRepo;
        _stockMoveRepo = stockMoveRepo;
    }

    public async Task<Insumo?> GetByIdAsync(string id, string tenantId)
        => await _repo.GetByIdAsync(id, tenantId);

    public async Task<List<Insumo>> GetAllAsync(string tenantId)
        => await _repo.GetAllAsync(tenantId);

    public async Task<ServiceResult<Insumo>> CreateAsync(CreateInsumoRequest request, string tenantId)
    {
        // Auto-generate numeric code based on last record
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            var all = await _repo.GetAllAsync(tenantId);
            var maxCode = all
                .Select(i => int.TryParse(i.Code, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            request.Code = (maxCode + 1).ToString("D5");
        }

        var existing = await _repo.GetByCodeAsync(request.Code, tenantId);
        if (existing is not null)
            return ServiceResult<Insumo>.Fail($"Ja existe um insumo com codigo '{request.Code}'.");

        var hasInitialPurchase = request.InitialQuantity > 0 && request.InitialUnitCost > 0 && request.InitialPackageSize > 0;

        // Sempre: totalUnits = embalagens × conteudo_por_embalagem
        // costPerUnit = preco_embalagem / conteudo_por_embalagem
        // O conteudo já está na menor unidade (g para KG, ml para LT, un para UN)
        var totalBaseUnits = hasInitialPurchase ? (long)(request.InitialQuantity * request.InitialPackageSize) : 0;
        var costPerBaseUnit = hasInitialPurchase ? request.InitialUnitCost / request.InitialPackageSize : 0;

        var insumo = new Insumo
        {
            TenantId = tenantId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Unit = request.Unit,
            BaseUnit = InsumoUnitHelper.BaseUnitName(request.Unit),
            MinStock = InsumoUnitHelper.ToBase(request.MinStock, request.Unit),
            CurrentStock = totalBaseUnits,
            AverageCost = costPerBaseUnit,
            LastCost = costPerBaseUnit,
            IsSellable = request.IsSellable,
            SalePrice = request.SalePrice,
            IsActive = true
        };

        var created = await _repo.InsertAsync(insumo);

        if (hasInitialPurchase)
        {
            var purchase = new InsumoPurchase
            {
                TenantId = tenantId,
                InsumoId = created.Id,
                InsumoName = created.Name,
                InsumoCode = created.Code,
                Quantity = totalBaseUnits,
                RemainingStock = totalBaseUnits,
                UnitCost = costPerBaseUnit,
                TotalCost = request.InitialQuantity * request.InitialUnitCost,
                PreviousStock = 0,
                NewStock = totalBaseUnits,
                PreviousAvgCost = 0,
                NewAvgCost = costPerBaseUnit,
                InputUnit = request.Unit.ToString(),
                InputQuantity = request.InitialQuantity,
                InputUnitCost = request.InitialUnitCost,
                Note = $"Estoque inicial ({request.InitialQuantity:N0}x embalagem de {request.InitialPackageSize})"
            };
            await _purchaseRepo.InsertAsync(purchase);

            // Create financial entry for the initial purchase
            var totalCost = request.InitialQuantity * request.InitialUnitCost;
            var financialEntry = new FinancialEntry
            {
                TenantId = tenantId,
                Type = FinancialType.Despesa,
                Category = FinancialCategory.Compra,
                Description = $"Compra de insumo: {created.Name}",
                Amount = totalCost,
                DueDate = DateTime.UtcNow,
                PaymentDate = DateTime.UtcNow,
                AmountPaid = totalCost,
                AmountDue = 0,
                Status = FinancialStatus.Pago,
                ReferenceId = purchase.Id,
                ReferenceType = "InsumoPurchase"
            };
            await _financialRepo.InsertAsync(financialEntry);
        }

        // Auto-create product when insumo is sellable
        if (request.IsSellable && request.SalePrice > 0)
        {
            var factor = InsumoUnitHelper.Factor(request.Unit);
            var costPerUserUnit = costPerBaseUnit * factor;

            var product = new Product
            {
                TenantId = tenantId,
                Name = created.Name,
                Description = created.Description,
                Unit = ProductUnit.UN,
                CostPrice = costPerUserUnit,
                SalePrice = request.SalePrice,
                MinSalePrice = costPerUserUnit,
                InsumoId = created.Id,
                IsActive = true
            };

            // Auto-generate SKU
            var allProducts = await _productRepo.GetAllAsync(tenantId);
            var maxSku = allProducts
                .Select(p => int.TryParse(p.Sku, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            product.Sku = (maxSku + 1).ToString("D6");

            var createdProduct = await _productRepo.InsertAsync(product);
            created.ProductId = createdProduct.Id;
            await _repo.UpdateAsync(created);

            // Create product stock from the insumo's initial purchase
            if (hasInitialPurchase && totalBaseUnits > 0)
            {
                var stockBalance = new StockBalance
                {
                    TenantId = tenantId,
                    ProductId = createdProduct.Id,
                    ProductName = createdProduct.Name,
                    ProductSku = createdProduct.Sku,
                    CurrentBalance = totalBaseUnits,
                    AvailableBalance = totalBaseUnits,
                    AverageCost = costPerUserUnit,
                    Unit = "UN"
                };
                await _stockBalanceRepo.InsertAsync(stockBalance);

                var stockMove = new StockMove
                {
                    TenantId = tenantId,
                    ProductId = createdProduct.Id,
                    ProductName = createdProduct.Name,
                    ProductSku = createdProduct.Sku,
                    Type = StockMoveType.Entrada,
                    Quantity = totalBaseUnits,
                    PreviousBalance = 0,
                    NewBalance = totalBaseUnits,
                    UnitCost = costPerUserUnit,
                    TotalCost = totalBaseUnits * costPerUserUnit,
                    ReferenceType = "InsumoPurchase",
                    ReferenceId = created.Id,
                    Note = $"Estoque inicial - revenda de {created.Name}",
                    UserId = "system"
                };
                await _stockMoveRepo.InsertAsync(stockMove);
            }
        }

        return ServiceResult<Insumo>.Ok(created);
    }

    public async Task<ServiceResult<Insumo>> UpdateAsync(string id, UpdateInsumoRequest request, string tenantId)
    {
        var insumo = await _repo.GetByIdAsync(id, tenantId);
        if (insumo is null)
            return ServiceResult<Insumo>.Fail("Insumo nao encontrado.");

        if (request.Name is not null) insumo.Name = request.Name;
        if (request.Description is not null) insumo.Description = request.Description;
        if (request.MinStock.HasValue) insumo.MinStock = InsumoUnitHelper.ToBase(request.MinStock.Value, insumo.Unit);
        if (request.IsActive.HasValue) insumo.IsActive = request.IsActive.Value;

        await _repo.UpdateAsync(insumo);
        return ServiceResult<Insumo>.Ok(insumo);
    }

    public async Task<bool> DeleteAsync(string id, string tenantId)
        => await _repo.DeleteAsync(id, tenantId);

    public async Task<ServiceResult<InsumoPurchase>> RegisterPurchaseAsync(CreatePurchaseRequest request, string tenantId)
    {
        if (request.Quantity <= 0)
            return ServiceResult<InsumoPurchase>.Fail("Informe a quantidade de embalagens.");
        if (request.UnitCost <= 0)
            return ServiceResult<InsumoPurchase>.Fail("Informe o custo por embalagem.");

        var insumo = await _repo.GetByIdAsync(request.InsumoId, tenantId);
        if (insumo is null)
            return ServiceResult<InsumoPurchase>.Fail("Insumo nao encontrado.");

        if (request.PackageSize <= 0)
            return ServiceResult<InsumoPurchase>.Fail("Informe o conteudo por embalagem.");

        // totalBaseUnits = embalagens × conteudo_por_embalagem (já na menor unidade: g, ml, un)
        // costPerBaseUnit = preco_embalagem / conteudo_por_embalagem
        var totalBaseUnits = (long)(request.Quantity * request.PackageSize);
        var costPerBaseUnit = request.UnitCost / request.PackageSize;

        var previousStock = insumo.CurrentStock;
        var previousAvgCost = insumo.AverageCost;

        // Weighted average cost (in base units: g, ml, un)
        var totalCostBefore = insumo.CurrentStock * insumo.AverageCost;
        var totalCostIncoming = totalBaseUnits * costPerBaseUnit;
        var newStock = insumo.CurrentStock + totalBaseUnits;
        var newAvgCost = newStock > 0 ? (totalCostBefore + totalCostIncoming) / newStock : 0;

        insumo.CurrentStock = newStock;
        insumo.AverageCost = newAvgCost;
        insumo.LastCost = costPerBaseUnit;

        await _repo.UpdateAsync(insumo);

        var purchase = new InsumoPurchase
        {
            TenantId = tenantId,
            InsumoId = insumo.Id,
            InsumoName = insumo.Name,
            InsumoCode = insumo.Code,
            Quantity = totalBaseUnits,
            RemainingStock = totalBaseUnits,
            UnitCost = costPerBaseUnit,
            TotalCost = request.Quantity * request.UnitCost,
            PreviousStock = previousStock,
            NewStock = newStock,
            PreviousAvgCost = previousAvgCost,
            NewAvgCost = newAvgCost,
            InputUnit = insumo.Unit.ToString(),
            InputQuantity = request.Quantity,
            InputUnitCost = request.UnitCost,
            Note = $"{request.Note ?? ""} ({request.Quantity:N0}x embalagem de {request.PackageSize})".Trim()
        };

        var created = await _purchaseRepo.InsertAsync(purchase);

        // Create financial entry for the purchase
        var financialEntry = new FinancialEntry
        {
            TenantId = tenantId,
            Type = FinancialType.Despesa,
            Category = FinancialCategory.Compra,
            Description = $"Compra de insumo: {insumo.Name}",
            Amount = request.Quantity * request.UnitCost,
            DueDate = DateTime.UtcNow,
            PaymentDate = DateTime.UtcNow,
            AmountPaid = request.Quantity * request.UnitCost,
            AmountDue = 0,
            Status = FinancialStatus.Pago,
            ReferenceId = created.Id,
            ReferenceType = "InsumoPurchase"
        };
        await _financialRepo.InsertAsync(financialEntry);

        await _recipeService.RecalculateByInsumoAsync(insumo.Id, tenantId);

        // If insumo is sellable, also add to product stock
        if (insumo.IsSellable && !string.IsNullOrEmpty(insumo.ProductId))
        {
            var product = await _productRepo.GetByIdAsync(insumo.ProductId, tenantId);
            if (product is not null)
            {
                var factor = InsumoUnitHelper.Factor(insumo.Unit);
                var costPerUserUnit = costPerBaseUnit * factor;

                var balance = await _stockBalanceRepo.GetByProductAsync(product.Id, tenantId);
                if (balance is not null)
                {
                    var prevBalance = balance.CurrentBalance;
                    var totalCostBefore2 = balance.CurrentBalance * balance.AverageCost;
                    var totalCostNew = totalBaseUnits * costPerUserUnit;
                    balance.CurrentBalance += totalBaseUnits;
                    balance.AvailableBalance = balance.CurrentBalance - balance.ReservedBalance;
                    balance.AverageCost = balance.CurrentBalance > 0
                        ? (totalCostBefore2 + totalCostNew) / balance.CurrentBalance
                        : 0;
                    await _stockBalanceRepo.UpdateAsync(balance);

                    var move = new StockMove
                    {
                        TenantId = tenantId,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductSku = product.Sku,
                        Type = StockMoveType.Entrada,
                        Quantity = totalBaseUnits,
                        PreviousBalance = prevBalance,
                        NewBalance = balance.CurrentBalance,
                        UnitCost = costPerUserUnit,
                        TotalCost = totalBaseUnits * costPerUserUnit,
                        ReferenceType = "InsumoPurchase",
                        ReferenceId = created.Id,
                        Note = $"Compra de revenda: {insumo.Name}",
                        UserId = "system"
                    };
                    await _stockMoveRepo.InsertAsync(move);
                }
            }
        }

        return ServiceResult<InsumoPurchase>.Ok(created);
    }

    public async Task<ServiceResult<bool>> DeletePurchaseAsync(string insumoId, string purchaseId, string tenantId)
    {
        var insumo = await _repo.GetByIdAsync(insumoId, tenantId);
        if (insumo is null)
            return ServiceResult<bool>.Fail("Insumo nao encontrado.");

        var purchase = await _purchaseRepo.GetByIdAsync(purchaseId, tenantId);
        if (purchase is null || purchase.InsumoId != insumoId)
            return ServiceResult<bool>.Fail("Compra nao encontrada.");

        // Only allow deletion if stock from this purchase hasn't been used
        if (purchase.RemainingStock < purchase.Quantity)
        {
            var used = purchase.Quantity - purchase.RemainingStock;
            var factor = InsumoUnitHelper.Factor(insumo.Unit);
            var usedDisplay = factor > 1 ? $"{(decimal)used / factor:N2} {insumo.Unit}" : $"{used} {insumo.BaseUnit}";
            return ServiceResult<bool>.Fail(
                $"Nao e possivel excluir esta compra. {usedDisplay} do estoque ja foram consumidos. " +
                $"Apenas compras com estoque intacto podem ser excluidas.");
        }

        // Reverse the stock
        insumo.CurrentStock -= purchase.Quantity;
        if (insumo.CurrentStock < 0) insumo.CurrentStock = 0;

        // Recalculate average cost from remaining purchases
        var allPurchases = await _purchaseRepo.GetByInsumoAsync(insumoId, tenantId);
        var remaining = allPurchases.Where(p => p.Id != purchaseId).ToList();

        if (insumo.CurrentStock > 0 && remaining.Count > 0)
        {
            var totalRemaining = remaining.Sum(p => p.RemainingStock);
            var totalCost = remaining.Sum(p => p.RemainingStock * p.UnitCost);
            insumo.AverageCost = totalRemaining > 0 ? totalCost / totalRemaining : 0;
            insumo.LastCost = remaining.OrderByDescending(p => p.CreatedAt).First().UnitCost;
        }
        else
        {
            insumo.AverageCost = 0;
            insumo.LastCost = 0;
        }

        await _repo.UpdateAsync(insumo);
        await _purchaseRepo.DeleteAsync(purchaseId, tenantId);

        // Delete the linked financial entry
        var financialEntry = await _financialRepo.GetByReferenceAsync(purchaseId, "InsumoPurchase", tenantId);
        if (financialEntry is not null)
            await _financialRepo.DeleteAsync(financialEntry.Id, tenantId);

        await _recipeService.RecalculateByInsumoAsync(insumo.Id, tenantId);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<List<InsumoPurchase>> GetPurchasesAsync(string insumoId, string tenantId)
        => await _purchaseRepo.GetByInsumoAsync(insumoId, tenantId);

    public async Task<ServiceResult<Insumo>> ConsumeAsync(string insumoId, decimal quantityInUserUnit, string tenantId, string note)
    {
        var insumo = await _repo.GetByIdAsync(insumoId, tenantId);
        if (insumo is null)
            return ServiceResult<Insumo>.Fail("Insumo nao encontrado.");

        var baseQty = InsumoUnitHelper.ToBase(quantityInUserUnit, insumo.Unit);

        if (insumo.CurrentStock < baseQty)
            return ServiceResult<Insumo>.Fail(
                $"Estoque insuficiente de '{insumo.Name}'. " +
                $"Necessario: {quantityInUserUnit:N2} {insumo.Unit}, " +
                $"Disponivel: {InsumoUnitHelper.FromBase(insumo.CurrentStock, insumo.Unit):N2} {insumo.Unit}");

        insumo.CurrentStock -= baseQty;
        await _repo.UpdateAsync(insumo);

        return ServiceResult<Insumo>.Ok(insumo);
    }

    /// <summary>Consume directly in base units (grams, ml, units). Uses FIFO - oldest purchases first.</summary>
    public async Task<ServiceResult<Insumo>> ConsumeBaseAsync(string insumoId, long baseQuantity, string tenantId, string note)
    {
        var insumo = await _repo.GetByIdAsync(insumoId, tenantId);
        if (insumo is null)
            return ServiceResult<Insumo>.Fail("Insumo nao encontrado.");

        if (insumo.CurrentStock < baseQuantity)
        {
            var available = InsumoUnitHelper.FromBase(insumo.CurrentStock, insumo.Unit);
            var needed = InsumoUnitHelper.FromBase(baseQuantity, insumo.Unit);
            return ServiceResult<Insumo>.Fail(
                $"Estoque insuficiente de '{insumo.Name}'. " +
                $"Necessario: {needed:N2} {insumo.Unit}, " +
                $"Disponivel: {available:N2} {insumo.Unit}");
        }

        // FIFO: consume from oldest purchases first
        var purchases = await _purchaseRepo.GetByInsumoAsync(insumoId, tenantId);
        var sortedByDate = purchases.Where(p => p.RemainingStock > 0).OrderBy(p => p.CreatedAt).ToList();

        var remaining = baseQuantity;
        foreach (var p in sortedByDate)
        {
            if (remaining <= 0) break;
            var consume = Math.Min(remaining, p.RemainingStock);
            p.RemainingStock -= consume;
            remaining -= consume;
            await _purchaseRepo.UpdateAsync(p);
        }

        insumo.CurrentStock -= baseQuantity;
        await _repo.UpdateAsync(insumo);

        return ServiceResult<Insumo>.Ok(insumo);
    }
}
