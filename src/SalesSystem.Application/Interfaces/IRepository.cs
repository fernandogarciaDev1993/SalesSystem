using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, string tenantId);
    Task<List<T>> GetAllAsync(string tenantId);
    Task<T> InsertAsync(T entity);
    Task UpdateAsync(T entity);
    Task<bool> DeleteAsync(string id, string tenantId);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(string id);
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<List<Tenant>> GetAllAsync();
    Task<Tenant> InsertAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, string tenantId);
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(string sku, string tenantId);
    Task<List<Product>> GetByRecipeInsumoAsync(string insumoId, string tenantId);
}

public interface IInsumoRepository : IRepository<Insumo>
{
    Task<Insumo?> GetByCodeAsync(string code, string tenantId);
}

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<List<Recipe>> GetInsumosAsync(string tenantId);
    Task<List<Recipe>> GetByIngredientInsumoAsync(string insumoId, string tenantId);
    Task<List<Recipe>> GetByIngredientRecipeAsync(string recipeId, string tenantId);
}

public interface IInsumoPurchaseRepository : IRepository<InsumoPurchase>
{
    Task<List<InsumoPurchase>> GetByInsumoAsync(string insumoId, string tenantId);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByDocumentAsync(string document, string tenantId);
}

public interface IStockBalanceRepository : IRepository<StockBalance>
{
    Task<StockBalance?> GetByProductAsync(string productId, string tenantId);
}

public interface IStockMoveRepository : IRepository<StockMove>
{
    Task<List<StockMove>> GetByProductAsync(string productId, string tenantId);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, string tenantId);
    Task<long> CountByStatusAsync(OrderStatus status, string tenantId);
}

public interface IFinancialRepository : IRepository<FinancialEntry>
{
    Task<List<FinancialEntry>> GetByPeriodAsync(string tenantId, DateTime from, DateTime to);
    Task<List<FinancialEntry>> GetPendingAsync(string tenantId);
    Task<FinancialEntry?> GetByReferenceAsync(string referenceId, string referenceType, string tenantId);
}
