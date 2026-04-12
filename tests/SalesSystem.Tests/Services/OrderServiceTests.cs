using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Tests.Helpers;

namespace SalesSystem.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IStockService> _stockService = new();
    private readonly Mock<IFinancialService> _financialService = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _orderRepo.Setup(r => r.InsertAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);
        _orderRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.CountByStatusAsync(It.IsAny<OrderStatus>(), TestDataBuilder.TenantId))
            .ReturnsAsync(0);

        _financialService.Setup(s => s.CreateAsync(It.IsAny<FinancialEntry>()))
            .ReturnsAsync((FinancialEntry e) => ServiceResult<FinancialEntry>.Ok(e));

        _sut = new OrderService(_orderRepo.Object, _stockService.Object, _financialService.Object);
    }

    [Fact]
    public async Task Create_EmptyItems_ReturnsFail()
    {
        var order = TestDataBuilder.CreateOrder(items: new List<OrderItem>());

        var result = await _sut.CreateAsync(order);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("at least one item");
    }

    [Fact]
    public async Task Create_CalculatesItemAndOrderTotals()
    {
        // Arrange: 2 items
        var items = new List<OrderItem>
        {
            TestDataBuilder.CreateOrderItem(quantity: 3, unitPrice: 100m, unitCost: 40m, discountPct: 10, taxRate: 5),
            TestDataBuilder.CreateOrderItem(productId: "p2", quantity: 1, unitPrice: 50m, unitCost: 20m)
        };
        var order = TestDataBuilder.CreateOrder(items: items);

        // Act
        var result = await _sut.CreateAsync(order);

        // Assert
        result.Success.Should().BeTrue();
        var data = result.Data!;

        // Item 1: price=300, discount=30, totalPrice=270, tax=13.5, cost=120
        data.Items[0].TotalPrice.Should().Be(270m);
        data.Items[0].TaxAmount.Should().Be(13.5m);

        // Item 2: price=50, discount=0, totalPrice=50, tax=0, cost=20
        data.Items[1].TotalPrice.Should().Be(50m);

        // Order: subtotal=350, discount=30, tax=13.5, total=350-30+13.5=333.5
        data.Subtotal.Should().Be(350m);
        data.DiscountTotal.Should().Be(30m);
        data.TaxTotal.Should().Be(13.5m);
        data.Total.Should().Be(333.5m);
        data.TotalCost.Should().Be(140m);
    }

    [Fact]
    public async Task Create_GeneratesOrderNumber()
    {
        var order = TestDataBuilder.CreateOrder();

        var result = await _sut.CreateAsync(order);

        result.Success.Should().BeTrue();
        result.Data!.OrderNumber.Should().StartWith("PED-");
    }

    [Fact]
    public async Task Confirm_NonDraftOrder_ReturnsFail()
    {
        var order = TestDataBuilder.CreateOrder(status: OrderStatus.Confirmado);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(order);

        var result = await _sut.ConfirmAsync(order.Id, TestDataBuilder.TenantId);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("draft");
    }

    [Fact]
    public async Task Confirm_InsufficientStock_ReturnsFail_NoDeduction()
    {
        // Arrange: draft order with 1 item needing qty=10, but only 3 available
        var order = TestDataBuilder.CreateOrder(items: new List<OrderItem>
        {
            TestDataBuilder.CreateOrderItem(quantity: 10)
        });
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(order);

        var lowBalance = TestDataBuilder.CreateStockBalance(currentBalance: 3, reservedBalance: 0);
        _stockService.Setup(s => s.GetBalanceByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(lowBalance);

        // Act
        var result = await _sut.ConfirmAsync(order.Id, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("insuficiente");

        // CRITICAL: AddMoveAsync should NEVER be called
        _stockService.Verify(s => s.AddMoveAsync(It.IsAny<StockMove>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_Success_DeductsStockAndCreatesFinancial()
    {
        // Arrange
        var order = TestDataBuilder.CreateOrder(items: new List<OrderItem>
        {
            TestDataBuilder.CreateOrderItem(quantity: 5, unitPrice: 20m, unitCost: 8m)
        });
        // Need to calculate totals first (CreateAsync does this)
        order.Total = 100m;

        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(order);

        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 50);
        _stockService.Setup(s => s.GetBalanceByProductAsync("product-1", TestDataBuilder.TenantId))
            .ReturnsAsync(balance);
        _stockService.Setup(s => s.AddMoveAsync(It.IsAny<StockMove>()))
            .ReturnsAsync(ServiceResult<StockMove>.Ok(new StockMove()));

        // Act
        var result = await _sut.ConfirmAsync(order.Id, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(OrderStatus.Confirmado);
        _stockService.Verify(s => s.AddMoveAsync(It.Is<StockMove>(m => m.Type == StockMoveType.Saida)), Times.Once);
        _financialService.Verify(s => s.CreateAsync(It.Is<FinancialEntry>(f => f.Type == FinancialType.Receita)), Times.Once);
    }

    [Fact]
    public async Task Confirm_NoPayments_CreatesSinglePaidEntry()
    {
        // Arrange
        var order = TestDataBuilder.CreateOrder(payments: new List<OrderPayment>());
        order.Total = 200m;

        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(order);

        var balance = TestDataBuilder.CreateStockBalance(currentBalance: 100);
        _stockService.Setup(s => s.GetBalanceByProductAsync(It.IsAny<string>(), TestDataBuilder.TenantId))
            .ReturnsAsync(balance);
        _stockService.Setup(s => s.AddMoveAsync(It.IsAny<StockMove>()))
            .ReturnsAsync(ServiceResult<StockMove>.Ok(new StockMove()));

        // Act
        var result = await _sut.ConfirmAsync(order.Id, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        _financialService.Verify(s => s.CreateAsync(It.Is<FinancialEntry>(f =>
            f.Status == FinancialStatus.Pago &&
            f.Amount == 200m &&
            f.AmountPaid == 200m)), Times.Once);
    }

    [Fact]
    public async Task Cancel_ConfirmedOrder_ReversesStock()
    {
        // Arrange
        var order = TestDataBuilder.CreateOrder(status: OrderStatus.Confirmado);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(order);
        _stockService.Setup(s => s.AddMoveAsync(It.IsAny<StockMove>()))
            .ReturnsAsync(ServiceResult<StockMove>.Ok(new StockMove()));

        // Act
        var result = await _sut.CancelAsync(order.Id, "Customer request", TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(OrderStatus.Cancelado);
        _stockService.Verify(s => s.AddMoveAsync(It.Is<StockMove>(m => m.Type == StockMoveType.Devolucao)), Times.Once);
    }

    [Fact]
    public async Task Cancel_DraftOrder_NoStockReversal()
    {
        // Arrange
        var order = TestDataBuilder.CreateOrder(status: OrderStatus.Rascunho);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(order);

        // Act
        var result = await _sut.CancelAsync(order.Id, "Changed mind", TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        _stockService.Verify(s => s.AddMoveAsync(It.IsAny<StockMove>()), Times.Never);
    }
}
