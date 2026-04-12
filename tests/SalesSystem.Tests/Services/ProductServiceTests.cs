using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Tests.Helpers;

namespace SalesSystem.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _productRepo.Setup(r => r.InsertAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product e) => e);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _productRepo.Setup(r => r.GetAllAsync(TestDataBuilder.TenantId))
            .ReturnsAsync(new List<Product>());
        _productRepo.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), TestDataBuilder.TenantId))
            .ReturnsAsync((Product?)null);

        _sut = new ProductService(_productRepo.Object);
    }

    [Fact]
    public async Task Create_AutoGeneratesSku()
    {
        var request = new CreateProductRequest
        {
            Sku = "",
            Name = "New Product",
            CostPrice = 10m,
            SalePrice = 25m
        };

        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        result.Success.Should().BeTrue();
        result.Data!.Sku.Should().Be("000001");
    }

    [Fact]
    public async Task Create_SalePriceBelowMinimum_ReturnsFail()
    {
        var request = new CreateProductRequest
        {
            Sku = "SKU001",
            Name = "Cheap Product",
            CostPrice = 10m,
            SalePrice = 5m,
            MinSalePrice = 8m
        };

        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("minimum sale price");
    }

    [Fact]
    public async Task Create_AutoCalculatesSalePrice_MarkupDivisor()
    {
        // Arrange: cost=10, operational=15%, profit=20%, tax=5%
        // Divisor = 1 - 0.15 - 0.20 - 0.05 = 0.60
        // SalePrice = 10 / 0.60 = 16.67 (rounded)
        var request = new CreateProductRequest
        {
            Sku = "",
            Name = "Auto Price Product",
            CostPrice = 10m,
            SalePrice = 0m, // triggers auto-calculation
            OperationalCostPct = 15m,
            ProfitMarginPct = 20m,
            TaxRate = 5m
        };

        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        result.Success.Should().BeTrue();
        result.Data!.SalePrice.Should().BeApproximately(16.67m, 0.01m);
    }

    [Fact]
    public async Task Create_ZeroCost_SalePriceStaysZero()
    {
        var request = new CreateProductRequest
        {
            Sku = "",
            Name = "Free Product",
            CostPrice = 0m,
            SalePrice = 0m,
            OperationalCostPct = 15m,
            ProfitMarginPct = 20m,
            TaxRate = 5m
        };

        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        result.Success.Should().BeTrue();
        result.Data!.SalePrice.Should().Be(0m);
    }
}
