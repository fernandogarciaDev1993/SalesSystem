using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IProductService
{
    Task<Product?> GetByIdAsync(string id, string tenantId);
    Task<List<Product>> GetAllAsync(string tenantId);
    Task<ServiceResult<Product>> CreateAsync(CreateProductRequest request, string tenantId);
    Task<ServiceResult<Product>> UpdateAsync(string id, UpdateProductRequest request, string tenantId);
    Task<bool> DeleteAsync(string id, string tenantId);
}

public class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MinSalePrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
    public ProductUnit Unit { get; set; } = ProductUnit.UN;
    public string? RecipeId { get; set; }
    public string? InsumoId { get; set; }
    public decimal OperationalCostPct { get; set; }
    public decimal ProfitMarginPct { get; set; }
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? MinSalePrice { get; set; }
    public decimal? TaxRate { get; set; }
    public bool? IsActive { get; set; }
    public string? RecipeId { get; set; }
    public string? InsumoId { get; set; }
    public decimal? OperationalCostPct { get; set; }
    public decimal? ProfitMarginPct { get; set; }
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;

    public ProductService(IProductRepository repo)
    {
        _repo = repo;
    }

    private static decimal CalculateSalePrice(decimal costPrice, decimal operationalPct, decimal profitPct, decimal taxPct)
    {
        if (costPrice <= 0) return 0;
        var divisor = 1m - (operationalPct / 100m) - (profitPct / 100m) - (taxPct / 100m);
        return divisor > 0 ? Math.Round(costPrice / divisor, 2) : 0;
    }

    public async Task<Product?> GetByIdAsync(string id, string tenantId)
        => await _repo.GetByIdAsync(id, tenantId);

    public async Task<List<Product>> GetAllAsync(string tenantId)
        => await _repo.GetAllAsync(tenantId);

    public async Task<ServiceResult<Product>> CreateAsync(CreateProductRequest request, string tenantId)
    {
        // Auto-generate numeric SKU based on last record
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            var all = await _repo.GetAllAsync(tenantId);
            var maxSku = all
                .Select(p => int.TryParse(p.Sku, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            request.Sku = (maxSku + 1).ToString("D6");
        }

        var existing = await _repo.GetBySkuAsync(request.Sku, tenantId);
        if (existing is not null)
            return ServiceResult<Product>.Fail($"Ja existe um produto com SKU '{request.Sku}'.");

        if (request.SalePrice < request.MinSalePrice)
            return ServiceResult<Product>.Fail("Sale price cannot be less than minimum sale price.");

        var product = new Product
        {
            TenantId = tenantId,
            Sku = request.Sku,
            Barcode = request.Barcode,
            Name = request.Name,
            Description = request.Description,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            MinSalePrice = request.MinSalePrice,
            TaxRate = request.TaxRate,
            Ncm = request.Ncm,
            Cfop = request.Cfop,
            Unit = request.Unit,
            RecipeId = request.RecipeId,
            InsumoId = request.InsumoId,
            HasRecipe = !string.IsNullOrEmpty(request.RecipeId),
            IsActive = true
        };

        product.OperationalCostPct = request.OperationalCostPct;
        product.ProfitMarginPct = request.ProfitMarginPct;
        if (product.SalePrice == 0 && product.CostPrice > 0)
        {
            product.SalePrice = CalculateSalePrice(product.CostPrice, product.OperationalCostPct, product.ProfitMarginPct, product.TaxRate);
            product.MinSalePrice = product.CostPrice;
        }

        var created = await _repo.InsertAsync(product);
        return ServiceResult<Product>.Ok(created);
    }

    public async Task<ServiceResult<Product>> UpdateAsync(string id, UpdateProductRequest request, string tenantId)
    {
        var product = await _repo.GetByIdAsync(id, tenantId);
        if (product is null)
            return ServiceResult<Product>.Fail("Product not found.");

        if (request.Name is not null) product.Name = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.CostPrice.HasValue) product.CostPrice = request.CostPrice.Value;
        if (request.SalePrice.HasValue) product.SalePrice = request.SalePrice.Value;
        if (request.MinSalePrice.HasValue) product.MinSalePrice = request.MinSalePrice.Value;
        if (request.TaxRate.HasValue) product.TaxRate = request.TaxRate.Value;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
        if (request.OperationalCostPct.HasValue) product.OperationalCostPct = request.OperationalCostPct.Value;
        if (request.ProfitMarginPct.HasValue) product.ProfitMarginPct = request.ProfitMarginPct.Value;
        if (request.RecipeId is not null)
        {
            product.RecipeId = string.IsNullOrEmpty(request.RecipeId) ? null : request.RecipeId;
            product.HasRecipe = !string.IsNullOrEmpty(product.RecipeId);
        }
        if (request.InsumoId is not null)
        {
            product.InsumoId = string.IsNullOrEmpty(request.InsumoId) ? null : request.InsumoId;
        }

        if (product.SalePrice < product.MinSalePrice)
            return ServiceResult<Product>.Fail("Sale price cannot be less than minimum sale price.");

        await _repo.UpdateAsync(product);
        return ServiceResult<Product>.Ok(product);
    }

    public async Task<bool> DeleteAsync(string id, string tenantId)
        => await _repo.DeleteAsync(id, tenantId);
}
