using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Tests.Helpers;

namespace SalesSystem.Tests.Services;

public class FinancialServiceTests
{
    private readonly Mock<IFinancialRepository> _financialRepo = new();
    private readonly FinancialService _sut;

    public FinancialServiceTests()
    {
        _financialRepo.Setup(r => r.InsertAsync(It.IsAny<FinancialEntry>()))
            .ReturnsAsync((FinancialEntry e) => e);
        _financialRepo.Setup(r => r.UpdateAsync(It.IsAny<FinancialEntry>()))
            .Returns(Task.CompletedTask);
        _financialRepo.Setup(r => r.GetAllAsync(TestDataBuilder.TenantId))
            .ReturnsAsync(new List<FinancialEntry>());

        _sut = new FinancialService(_financialRepo.Object);
    }

    [Fact]
    public async Task Create_ZeroAmount_ReturnsFail()
    {
        var entry = TestDataBuilder.CreateFinancialEntry(amount: 0m);

        var result = await _sut.CreateAsync(entry);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task Create_AutoGeneratesCode()
    {
        var entry = TestDataBuilder.CreateFinancialEntry(amount: 100m);

        var result = await _sut.CreateAsync(entry);

        result.Success.Should().BeTrue();
        result.Data!.Code.Should().StartWith("FIN-");
    }

    [Fact]
    public async Task Create_CalculatesAmountDue()
    {
        var entry = TestDataBuilder.CreateFinancialEntry(amount: 100m, amountPaid: 30m);

        var result = await _sut.CreateAsync(entry);

        result.Success.Should().BeTrue();
        result.Data!.AmountDue.Should().Be(70m);
    }

    [Fact]
    public async Task Update_FullyPaid_SetsStatusPago()
    {
        // Arrange
        var entry = TestDataBuilder.CreateFinancialEntry(amount: 100m, amountPaid: 0m, status: FinancialStatus.Pendente);
        _financialRepo.Setup(r => r.GetByIdAsync(entry.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(entry);

        // Now mark as fully paid
        entry.AmountPaid = 100m;

        // Act
        var result = await _sut.UpdateAsync(entry);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(FinancialStatus.Pago);
        result.Data!.AmountDue.Should().Be(0m);
    }

    [Fact]
    public async Task GetSummary_CalculatesCorrectly()
    {
        // Arrange
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 12, 31);

        var entries = new List<FinancialEntry>
        {
            TestDataBuilder.CreateFinancialEntry(type: FinancialType.Receita, amount: 500m, status: FinancialStatus.Pago),
            TestDataBuilder.CreateFinancialEntry(type: FinancialType.Receita, amount: 300m, status: FinancialStatus.Pendente),
            TestDataBuilder.CreateFinancialEntry(type: FinancialType.Despesa, amount: 200m, status: FinancialStatus.Pago),
            TestDataBuilder.CreateFinancialEntry(type: FinancialType.Despesa, amount: 100m, status: FinancialStatus.Vencido)
        };

        _financialRepo.Setup(r => r.GetByPeriodAsync(TestDataBuilder.TenantId, from, to))
            .ReturnsAsync(entries);

        // Act
        var summary = await _sut.GetSummaryAsync(TestDataBuilder.TenantId, from, to);

        // Assert
        summary.TotalReceitas.Should().Be(800m);
        summary.TotalDespesas.Should().Be(300m);
        summary.Saldo.Should().Be(500m);
        summary.Pendentes.Should().Be(1);
        summary.Vencidos.Should().Be(1);
    }
}
