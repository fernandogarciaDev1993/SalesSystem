using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Tests.Helpers;

namespace SalesSystem.Tests.Services;

public class RecipeServiceTests
{
    private readonly Mock<IRecipeRepository> _recipeRepo = new();
    private readonly Mock<IInsumoRepository> _insumoRepo = new();
    private readonly RecipeService _sut;

    public RecipeServiceTests()
    {
        _recipeRepo.Setup(r => r.InsertAsync(It.IsAny<Recipe>()))
            .ReturnsAsync((Recipe e) => e);
        _recipeRepo.Setup(r => r.UpdateAsync(It.IsAny<Recipe>()))
            .Returns(Task.CompletedTask);
        _recipeRepo.Setup(r => r.GetAllAsync(TestDataBuilder.TenantId))
            .ReturnsAsync(new List<Recipe>());

        _insumoRepo.Setup(r => r.InsertAsync(It.IsAny<Insumo>()))
            .ReturnsAsync((Insumo e) => e);

        _sut = new RecipeService(_recipeRepo.Object, _insumoRepo.Object);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsFail()
    {
        var request = new CreateRecipeRequest { Name = "" };

        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("obrigatorio");
    }

    [Fact]
    public async Task Create_CalculatesCostFromIngredients()
    {
        // Arrange: 300g at 0.005/g + 4 UN at 0.6/un
        // Cost = 300*0.005 + 4*0.6 = 1.5 + 2.4 = 3.9
        var insumoKg = TestDataBuilder.CreateInsumo(id: "insumo-flour", unit: InsumoUnit.KG, averageCost: 0.005m);
        var insumoUn = TestDataBuilder.CreateInsumo(id: "insumo-eggs", unit: InsumoUnit.UN, averageCost: 0.6m);

        _insumoRepo.Setup(r => r.GetByIdAsync("insumo-flour", TestDataBuilder.TenantId))
            .ReturnsAsync(insumoKg);
        _insumoRepo.Setup(r => r.GetByIdAsync("insumo-eggs", TestDataBuilder.TenantId))
            .ReturnsAsync(insumoUn);

        var request = new CreateRecipeRequest
        {
            Name = "Cake",
            YieldQuantity = 1,
            Ingredients = new List<RecipeItemRequest>
            {
                new() { InsumoId = "insumo-flour", Quantity = 300, IsRecipe = false },
                new() { InsumoId = "insumo-eggs", Quantity = 4, IsRecipe = false }
            }
        };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.CalculatedCost.Should().BeApproximately(3.9m, 0.0001m);
    }

    [Fact]
    public async Task Create_CostPerUnit_DividedByYield()
    {
        // Arrange: same as above but yield = 10 → costPerUnit = 3.9 / 10 = 0.39
        var insumoKg = TestDataBuilder.CreateInsumo(id: "insumo-flour", unit: InsumoUnit.KG, averageCost: 0.005m);
        var insumoUn = TestDataBuilder.CreateInsumo(id: "insumo-eggs", unit: InsumoUnit.UN, averageCost: 0.6m);

        _insumoRepo.Setup(r => r.GetByIdAsync("insumo-flour", TestDataBuilder.TenantId))
            .ReturnsAsync(insumoKg);
        _insumoRepo.Setup(r => r.GetByIdAsync("insumo-eggs", TestDataBuilder.TenantId))
            .ReturnsAsync(insumoUn);

        var request = new CreateRecipeRequest
        {
            Name = "Cake",
            YieldQuantity = 10,
            Ingredients = new List<RecipeItemRequest>
            {
                new() { InsumoId = "insumo-flour", Quantity = 300, IsRecipe = false },
                new() { InsumoId = "insumo-eggs", Quantity = 4, IsRecipe = false }
            }
        };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Data!.CostPerUnit.Should().BeApproximately(0.39m, 0.0001m);
    }

    [Fact]
    public async Task Create_IsInsumo_AutoCreatesOutputInsumo()
    {
        // Arrange
        var insumo = TestDataBuilder.CreateInsumo(id: "insumo-1", averageCost: 0.01m);
        _insumoRepo.Setup(r => r.GetByIdAsync("insumo-1", TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);

        var request = new CreateRecipeRequest
        {
            Name = "Pasta Dough",
            IsInsumo = true,
            OutputInsumoId = null,
            YieldQuantity = 1,
            Ingredients = new List<RecipeItemRequest>
            {
                new() { InsumoId = "insumo-1", Quantity = 100, IsRecipe = false }
            }
        };

        // Act
        var result = await _sut.CreateAsync(request, TestDataBuilder.TenantId);

        // Assert
        result.Success.Should().BeTrue();
        _insumoRepo.Verify(r => r.InsertAsync(It.Is<Insumo>(i =>
            i.Name.Contains("producao") &&
            i.Unit == InsumoUnit.UN)), Times.Once);
        result.Data!.OutputInsumoId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Recalculate_UpdatesIngredientCosts()
    {
        // Arrange: recipe with ingredient whose cost changed
        var insumo = TestDataBuilder.CreateInsumo(id: "insumo-updated", averageCost: 0.010m);
        _insumoRepo.Setup(r => r.GetByIdAsync("insumo-updated", TestDataBuilder.TenantId))
            .ReturnsAsync(insumo);

        var recipe = TestDataBuilder.CreateRecipe(ingredients: new List<RecipeIngredient>
        {
            new()
            {
                InsumoId = "insumo-updated",
                InsumoName = "Old Name",
                Quantity = 500,
                Unit = "g",
                UnitCost = 0.005m,       // old cost
                TotalCost = 2.5m,
                IsRecipe = false
            }
        });

        _recipeRepo.Setup(r => r.GetByIdAsync(recipe.Id, TestDataBuilder.TenantId))
            .ReturnsAsync(recipe);

        // Act
        var result = await _sut.RecalculateCostAsync(recipe.Id, TestDataBuilder.TenantId);

        // Assert: new cost = 500 * 0.010 = 5.0
        result.Success.Should().BeTrue();
        result.Data!.Ingredients[0].UnitCost.Should().Be(0.010m);
        result.Data!.CalculatedCost.Should().BeApproximately(5.0m, 0.0001m);
    }
}
