using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Tests.Helpers;

namespace SalesSystem.Tests.Services;

public class InsumoServiceTests
{
    private readonly Mock<IInsumoRepository> _insumoRepo = new();
    private readonly Mock<IInsumoPurchaseRepository> _purchaseRepo = new();
    private readonly Mock<IRecipeService> _recipeService = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IFinancialRepository> _financialRepo = new();
    private readonly Mock<IStockBalanceRepository> _stockBalanceRepo = new();
    private readonly Mock<IStockMoveRepository> _stockMoveRepo = new();
    private readonly InsumoService _sut;

    public InsumoServiceTests()
    {
        _insumoRepo.Setup(r => r.InsertAsync(It.IsAny<Insumo>()))
            .ReturnsAsync((Insumo e) => e);
        _insumoRepo.Setup(r => r.UpdateAsync(It.IsAny<Insumo>()))
            .Returns(Task.CompletedTask);
        _insumoRepo.Setup(r => r.GetAllAsync(TestDataBuilder.TenantId))
            .ReturnsAsync(new List<Insumo>());

        _purchaseRepo.Setup(r => r.InsertAsync(It.IsAny<InsumoPurchase>()))
            .ReturnsAsync((InsumoPurchase e) => e);
        _purchaseRepo.Setup(r => r.UpdateAsync(It.IsAny<InsumoPurchase>()))
            .Returns(Task.CompletedTask);

        _productRepo.Setup(r => r.InsertAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product e) => e);
        _productRepo.Setup(r => r.GetAllAsync(TestDataBuilder.TenantId))
            .ReturnsAsync(new List<Product>());

        _financialRepo.Setup(r => r.InsertAsync(It.IsAny<FinancialEntry>()))
            .ReturnsAsync((FinancialEntry e) => e);

        _stockBalanceRepo.Setup(r => r.InsertAsync(It.IsAny<StockBalance>()))
            .ReturnsAsync((StockBalance e) => e);
        _stockMoveRepo.Setup(r => r.InsertAsync(It.IsAny<StockMove>()))
            .ReturnsAsync((StockMove e) => e);

        _recipeService.Setup(r => r.RecalculateByInsumoAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(0);

        _sut = new InsumoService(
            _insumoRepo.Object,
            _purchaseRepo.Object,
            _recipeService.Object,
            _productRepo.Object,
            _financialRepo.Object,
            _stockBalanceRepo.Object,
            _stockMoveRepo.Object);
    }

    [Fact]
    public async Task Create_AutoGeneratesCode()
    {
        // Arrange
        _insumoRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), TestDataBuilder.TenantId))
            .ReturnsAsync((Insumo?)null);

        var request = new CreateInsumoRequest
        {
            Code = "",
            Name = "Sugar",
            Unit = InsumoUnit.KG
        };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Code.Should().Be("00001");
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsFail()
    {
        // Arrange
        _insumoRepo.Setup(r => r.GetByCodeAsync("DUP01", TestDataBuilder.TenantId))
            .ReturnsAsync(TestDataBuilder.CreateInsumo(code: "DUP01"));

        var request = new CreateInsumoRequest { Code = "DUP01", Name = "Dup" };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("DUP01");
    }

    [Fact]
    public async Task Create_WithInitialPurchase_SetsStockAndCost()
    {
        // Arrange: 2 packages of 500g each at R$5/package
        // totalBaseUnits = 2 * 500 = 1000
        // costPerBase = 5 / 500 = 0.01
        _insumoRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), TestDataBuilder.TenantId))
            .ReturnsAsync((Insumo?)null);

        var request = new CreateInsumoRequest
        {
            Code = "",
            Name = "Flour",
            Unit = InsumoUnit.KG,
            InitialQuantity = 2,
            InitialUnitCost = 5m,
            InitialPackageSize = 500
        };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.CurrentStock.Should().Be(1000);
        result.Data!.AverageCost.Should().Be(0.01m);
    }

    [Fact]
    public async Task Create_WithInitialPurchase_CreatesFinancialEntry()
    {
        // Arrange
        _insumoRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), TestDataBuilder.TenantId))
            .ReturnsAsync((Insumo?)null);

        var request = new CreateInsumoRequest
        {
            Code = "",
            Name = "Flour",
            Unit = InsumoUnit.KG,
            InitialQuantity = 2,
            InitialUnitCost = 5m,
            InitialPackageSize = 500
        };

        // Act
        await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        _financialRepo.Verify(r => r.InsertAsync(It.Is<FinancialEntry>(f =>
            f.Type == FinancialType.Despesa &&
            f.Category == FinancialCategory.Compra &&
            f.Amount == 10m)), Times.Once);
    }

    [Fact]
    public async Task Create_Sellable_AutoCreatesProduct()
    {
        // Arrange
        _insumoRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), TestDataBuilder.TenantId))
            .ReturnsAsync((Insumo?)null);

        var request = new CreateInsumoRequest
        {
            Code = "",
            Name = "Eggs",
            Unit = InsumoUnit.UN,
            IsSellable = true,
            SalePrice = 15m,
            InitialQuantity = 1,
            InitialUnitCost = 12m,
            InitialPackageSize = 20
        };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        _productRepo.Verify(r => r.InsertAsync(It.Is<Product>(p =>
            p.Name == "Eggs" &&
            p.SalePrice == 15m)), Times.Once);
    }

    [Fact]
    public async Task RegisterPurchase_CalculatesWeightedAvgCost()
    {
        // Arrange: existing stock = 1000g at 0.003/g
        // Buy 2 packages of 1000g at R$5/package → 2000g at 0.005/g
        // New avg = (1000*0.003 + 2000*0.005) / 3000 = (3 + 10) / 3000 = 13/3000 ≈ 0.004333
        var insumo = TestDataBuilder.CreateInsumo(currentStock: 1000, averageCost: 0.003m);
        _insumoRepo.Setup(r => r.GetByIdAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);

        var request = new CreatePurchaseRequest
        {
            InsumoId = insumo.Id,
            Quantity = 2,
            UnitCost = 5m,
            PackageSize = 1000
        };

        // Act
        var result = await _sut.RegisterPurchaseAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        insumo.CurrentStock.Should().Be(3000);
        insumo.AverageCost.Should().BeApproximately(0.004333m, 0.0001m);
    }

    [Fact]
    public async Task RegisterPurchase_ZeroQuantity_ReturnsFail()
    {
        var request = new CreatePurchaseRequest
        {
            InsumoId = "some-id",
            Quantity = 0,
            UnitCost = 5m,
            PackageSize = 100
        };

        var result = await _sut.RegisterPurchaseAsync(request, TestDataBuilder.TenantId);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterPurchase_CreatesFinancialEntry()
    {
        // Arrange
        var insumo = TestDataBuilder.CreateInsumo(currentStock: 1000, averageCost: 0.003m);
        _insumoRepo.Setup(r => r.GetByIdAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);

        var request = new CreatePurchaseRequest
        {
            InsumoId = insumo.Id,
            Quantity = 3,
            UnitCost = 10m,
            PackageSize = 500
        };

        // Act
        await _sut.RegisterPurchaseAsync(request, TestDataBuilder.TenantId);

        // Assert: total = 3 * 10 = 30
        _financialRepo.Verify(r => r.InsertAsync(It.Is<FinancialEntry>(f =>
            f.Amount == 30m &&
            f.ReferenceType == "InsumoPurchase")), Times.Once);
    }

    [Fact]
    public async Task ConsumeBase_FIFO_ConsumesOldestFirst()
    {
        // Arrange: 3 purchases at different dates
        var insumoId = "insumo-fifo";
        var insumo = TestDataBuilder.CreateInsumo(id: insumoId, currentStock: 3000);

        var p1 = TestDataBuilder.CreateInsumoPurchase(insumoId: insumoId, quantity: 1000, remainingStock: 1000,
            createdAt: new DateTime(2024, 1, 1));
        var p2 = TestDataBuilder.CreateInsumoPurchase(insumoId: insumoId, quantity: 1000, remainingStock: 1000,
            createdAt: new DateTime(2024, 2, 1));
        var p3 = TestDataBuilder.CreateInsumoPurchase(insumoId: insumoId, quantity: 1000, remainingStock: 1000,
            createdAt: new DateTime(2024, 3, 1));

        _insumoRepo.Setup(r => r.GetByIdAsync(insumoId, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);
        _purchaseRepo.Setup(r => r.GetByInsumoAsync(insumoId, TestDataBuilder.TenantId))
            .ReturnsAsync(new List<InsumoPurchase> { p1, p2, p3 });

        // Act: consume 1500 — should consume all of p1 (1000) and 500 of p2
        var result = await _sut.ConsumeBaseAsync(insumoId, 1500, TestDataBuilder.TenantId, "recipe");

        // Assert
        result.Success.Should().BeTrue();
        p1.RemainingStock.Should().Be(0);
        p2.RemainingStock.Should().Be(500);
        p3.RemainingStock.Should().Be(1000);
        insumo.CurrentStock.Should().Be(1500);
    }

    [Fact]
    public async Task ConsumeBase_InsufficientStock_ReturnsFail()
    {
        var insumo = TestDataBuilder.CreateInsumo(currentStock: 100);
        _insumoRepo.Setup(r => r.GetByIdAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);

        var result = await _sut.ConsumeBaseAsync(insumo.Id, 500, TestDataBuilder.TenantId, "test");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("insuficiente");
    }

    [Fact]
    public async Task DeletePurchase_FullyConsumed_ReturnsFail()
    {
        // Arrange: purchase with all stock consumed
        var insumo = TestDataBuilder.CreateInsumo();
        var purchase = TestDataBuilder.CreateInsumoPurchase(
            insumoId: insumo.Id,
            quantity: 1000,
            remainingStock: 0);

        _insumoRepo.Setup(r => r.GetByIdAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);
        _purchaseRepo.Setup(r => r.GetByIdAsync(purchase.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(purchase);

        // Act
        var result = await _sut.DeletePurchaseAsync(insumo.Id, purchase.Id, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("consumidos");
    }

    [Fact]
    public async Task DeletePurchase_Untouched_Succeeds()
    {
        // Arrange: purchase fully intact
        var insumo = TestDataBuilder.CreateInsumo(currentStock: 2000);
        var purchase = TestDataBuilder.CreateInsumoPurchase(
            insumoId: insumo.Id,
            quantity: 2000,
            remainingStock: 2000,
            createdAt: DateTime.UtcNow);

        _insumoRepo.Setup(r => r.GetByIdAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);
        _purchaseRepo.Setup(r => r.GetByIdAsync(purchase.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(purchase);
        _purchaseRepo.Setup(r => r.GetByInsumoAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(new List<InsumoPurchase> { purchase });
        _purchaseRepo.Setup(r => r.DeleteAsync(purchase.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(true);
        _financialRepo.Setup(r => r.GetByReferenceAsync(purchase.Id, "InsumoPurchase", TestDataBuilder.TenantId))
            .ReturnsAsync((FinancialEntry?)null);

        // Act
        var result = await _sut.DeletePurchaseAsync(insumo.Id, purchase.Id, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        _purchaseRepo.Verify(r => r.DeleteAsync(purchase.Id, TestDataBuilder.TenantId), Times.Once);
    }

    [Fact]
    public async Task DeletePurchase_DeletesFinancialEntry()
    {
        // Arrange
        var insumo = TestDataBuilder.CreateInsumo(currentStock: 1000);
        var purchase = TestDataBuilder.CreateInsumoPurchase(
            insumoId: insumo.Id,
            quantity: 1000,
            remainingStock: 1000);
        var financial = TestDataBuilder.CreateFinancialEntry(type: FinancialType.Despesa, amount: 50m);

        _insumoRepo.Setup(r => r.GetByIdAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);
        _purchaseRepo.Setup(r => r.GetByIdAsync(purchase.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(purchase);
        _purchaseRepo.Setup(r => r.GetByInsumoAsync(insumo.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(new List<InsumoPurchase> { purchase });
        _purchaseRepo.Setup(r => r.DeleteAsync(purchase.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(true);
        _financialRepo.Setup(r => r.GetByReferenceAsync(purchase.Id, "InsumoPurchase", TestDataBuilder.TenantId))
            .ReturnsAsync(financial);
        _financialRepo.Setup(r => r.DeleteAsync(financial.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(true);

        // Act
        await _sut.DeletePurchaseAsync(insumo.Id, purchase.Id, TestDataBuilder.TenantId);

        // Assert
        _financialRepo.Verify(r => r.DeleteAsync(financial.Id, TestDataBuilder.TenantId), Times.Once);
    }
}
