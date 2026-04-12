using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Tests.Helpers;

namespace SalesSystem.Tests.Services;

public class StockServiceTests
{
    private readonly Mock<IStockBalanceRepository> _balanceRepo = new();
    private readonly Mock<IStockMoveRepository> _moveRepo = new();
    private readonly StockService _sut;

    public StockServiceTests()
    {
        _moveRepo.Setup(r => r.InsertAsync(It.IsAny<StockMove>()))
            .ReturnsAsync((StockMove m) => m);
        _balanceRepo.Setup(r => r.InsertAsync(It.IsAny<StockBalance>()))
            .ReturnsAsync((StockBalance b) => b);
        _balanceRepo.Setup(r => r.UpdateAsync(It.IsAny<StockBalance>()))
            .Returns(Task.CompletedTask);

        _sut = new StockService(_balanceRepo.Object, _moveRepo.Object);
    }

    [Fact]
    public async Task AddMove_Entrada_RecalculatesWeightedAvgCost()
    {
        // Arrange: existing balance of 100 units at $10 avg
        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 100, averageCost: 10m);
        _balanceRepo.Setup(r => r.GetByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(balance);

        var move = TestDataBuilder.CreateStockMove(type: StockMoveType.Entrada, quantity: 50, unitCost: 20m);

        // Act
        var result = await _sut.AddMoveAsync(move);

        // Assert: (100*10 + 50*20) / 150 = 2000/150 = 13.333...
        result.Success.Should().BeTrue();
        balance.AverageCost.Should().BeApproximately(13.3333m, 0.0001m);
        balance.CurrentBalance.Should().Be(150);
    }

    [Fact]
    public async Task AddMove_Entrada_NewProduct_CreatesBalance()
    {
        // Arrange: no existing balance
        _balanceRepo.Setup(r => r.GetByProductAsync("product-new", TestDataBuilder.TenantId))
            .ReturnsAsync((StockBalance?)null);

        var move = TestDataBuilder.CreateStockMove(productId: "product-new", type: StockMoveType.Entrada, quantity: 30, unitCost: 5m);

        // Act
        var result = await _sut.AddMoveAsync(move);

        // Assert
        result.Success.Should().BeTrue();
        _balanceRepo.Verify(r => r.InsertAsync(It.Is<StockBalance>(b =>
            b.ProductId == "product-new" &&
            b.CurrentBalance == 30 &&
            b.AverageCost == 5m)), Times.Once);
    }

    [Fact]
    public async Task AddMove_Saida_InsufficientBalance_ReturnsFail()
    {
        // Arrange
        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 5);
        _balanceRepo.Setup(r => r.GetByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(balance);

        var move = TestDataBuilder.CreateStockMove(type: StockMoveType.Saida, quantity: 10);

        // Act
        var result = await _sut.AddMoveAsync(move);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task AddMove_Saida_DeductsBalance()
    {
        // Arrange
        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 100);
        _balanceRepo.Setup(r => r.GetByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(balance);

        var move = TestDataBuilder.CreateStockMove(type: StockMoveType.Saida, quantity: 30);

        // Act
        var result = await _sut.AddMoveAsync(move);

        // Assert
        result.Success.Should().BeTrue();
        balance.CurrentBalance.Should().Be(70);
    }

    [Fact]
    public async Task AddMove_Reserva_InsufficientAvailable_ReturnsFail()
    {
        // Arrange: 100 current, 90 reserved → 10 available
        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 100, reservedBalance: 90);
        _balanceRepo.Setup(r => r.GetByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(balance);

        var move = TestDataBuilder.CreateStockMove(type: StockMoveType.Reserva, quantity: 20);

        // Act
        var result = await _sut.AddMoveAsync(move);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Insufficient available");
    }

    [Fact]
    public async Task AddMove_AlwaysUpdatesAvailableBalance()
    {
        // Arrange
        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 100, reservedBalance: 20);
        _balanceRepo.Setup(r => r.GetByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(balance);

        var move = TestDataBuilder.CreateStockMove(type: StockMoveType.Entrada, quantity: 50, unitCost: 10m);

        // Act
        var result = await _sut.AddMoveAsync(move);

        // Assert: available = currentBalance(150) - reserved(20) = 130
        result.Success.Should().BeTrue();
        balance.AvailableBalance.Should().Be(130);
    }

    [Fact]
    public async Task AddMove_ZeroQuantity_ReturnsFail()
    {
        var move = TestDataBuilder.CreateStockMove(quantity: 0);

        var result = await _sut.AddMoveAsync(move);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("greater than zero");
    }
}
