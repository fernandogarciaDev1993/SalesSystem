using SalesSystem.Domain.Entities;

namespace SalesSystem.Tests.Helpers;

public static class TestDataBuilder
{
    public const string TenantId = "tenant-test-001";

    public static Insumo CreateInsumo(
        string? id = null,
        string code = "00001",
        string name = "Farinha de Trigo",
        InsumoUnit unit = InsumoUnit.KG,
        long currentStock = 5000,
        decimal averageCost = 0.005m,
        bool isSellable = false,
        decimal salePrice = 0m,
        string? productId = null)
    {
        return new Insumo
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            Code = code,
            Name = name,
            Unit = unit,
            BaseUnit = InsumoUnitHelper.BaseUnitName(unit),
            CurrentStock = currentStock,
            MinStock = 1000,
            AverageCost = averageCost,
            LastCost = averageCost,
            IsSellable = isSellable,
            SalePrice = salePrice,
            ProductId = productId,
            IsActive = true
        };
    }

    public static InsumoPurchase CreateInsumoPurchase(
        string? id = null,
        string insumoId = "insumo-1",
        long quantity = 2000,
        decimal unitCost = 0.005m,
        long remainingStock = 2000,
        DateTime? createdAt = null)
    {
        return new InsumoPurchase
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            InsumoId = insumoId,
            InsumoName = "Farinha de Trigo",
            InsumoCode = "00001",
            Quantity = quantity,
            RemainingStock = remainingStock,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            PreviousStock = 0,
            NewStock = quantity,
            PreviousAvgCost = 0,
            NewAvgCost = unitCost,
            InputUnit = "KG",
            InputQuantity = 1,
            InputUnitCost = unitCost * 1000,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public static Order CreateOrder(
        string? id = null,
        OrderStatus status = OrderStatus.Rascunho,
        List<OrderItem>? items = null,
        List<OrderPayment>? payments = null)
    {
        return new Order
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            OrderNumber = "",
            Status = status,
            CustomerId = "customer-1",
            CustomerName = "Test Customer",
            Items = items ?? new List<OrderItem>
            {
                CreateOrderItem()
            },
            Payments = payments ?? new List<OrderPayment>()
        };
    }

    public static OrderItem CreateOrderItem(
        string productId = "product-1",
        string name = "Product A",
        decimal quantity = 2,
        decimal unitPrice = 50m,
        decimal unitCost = 20m,
        decimal discountPct = 0m,
        decimal taxRate = 0m)
    {
        return new OrderItem
        {
            ProductId = productId,
            Sku = "000001",
            Name = name,
            Unit = "UN",
            Quantity = quantity,
            UnitPrice = unitPrice,
            UnitCost = unitCost,
            DiscountPct = discountPct,
            TaxRate = taxRate
        };
    }

    public static StockBalance CreateStockBalance(
        string productId = "product-1",
        decimal currentBalance = 100,
        decimal reservedBalance = 0,
        decimal averageCost = 10m)
    {
        return new StockBalance
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            ProductId = productId,
            ProductName = "Product A",
            ProductSku = "000001",
            CurrentBalance = currentBalance,
            ReservedBalance = reservedBalance,
            AvailableBalance = currentBalance - reservedBalance,
            AverageCost = averageCost,
            Unit = "UN"
        };
    }

    public static StockMove CreateStockMove(
        string productId = "product-1",
        StockMoveType type = StockMoveType.Entrada,
        decimal quantity = 10,
        decimal unitCost = 10m)
    {
        return new StockMove
        {
            TenantId = TenantId,
            ProductId = productId,
            ProductName = "Product A",
            ProductSku = "000001",
            Type = type,
            Quantity = quantity,
            UnitCost = unitCost,
            UserId = "user-1"
        };
    }

    public static Recipe CreateRecipe(
        string? id = null,
        string name = "Bolo de Chocolate",
        int yieldQuantity = 10,
        List<RecipeIngredient>? ingredients = null)
    {
        return new Recipe
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            Code = "REC-00001",
            Name = name,
            YieldQuantity = yieldQuantity,
            Ingredients = ingredients ?? new List<RecipeIngredient>(),
            IsActive = true
        };
    }

    public static FinancialEntry CreateFinancialEntry(
        string? id = null,
        FinancialType type = FinancialType.Receita,
        decimal amount = 100m,
        decimal amountPaid = 0m,
        FinancialStatus status = FinancialStatus.Pendente)
    {
        return new FinancialEntry
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            Type = type,
            Category = FinancialCategory.Venda,
            Description = "Test entry",
            Amount = amount,
            AmountPaid = amountPaid,
            AmountDue = amount - amountPaid,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = status
        };
    }

    public static Product CreateProduct(
        string? id = null,
        string sku = "000001",
        string name = "Product A",
        decimal costPrice = 10m,
        decimal salePrice = 25m,
        decimal minSalePrice = 10m)
    {
        return new Product
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TenantId = TenantId,
            Sku = sku,
            Name = name,
            CostPrice = costPrice,
            SalePrice = salePrice,
            MinSalePrice = minSalePrice,
            Unit = ProductUnit.UN,
            IsActive = true
        };
    }
}
