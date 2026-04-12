using FluentAssertions;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Tests.Domain;

public class InsumoUnitHelperTests
{
    [Theory]
    [InlineData(InsumoUnit.KG, 1000)]
    [InlineData(InsumoUnit.LT, 1000)]
    [InlineData(InsumoUnit.UN, 1)]
    [InlineData(InsumoUnit.GR, 1)]
    [InlineData(InsumoUnit.ML, 1)]
    public void Factor_ReturnsCorrectValue(InsumoUnit unit, int expected)
    {
        InsumoUnitHelper.Factor(unit).Should().Be(expected);
    }

    [Theory]
    [InlineData(InsumoUnit.KG, "g")]
    [InlineData(InsumoUnit.LT, "ml")]
    [InlineData(InsumoUnit.UN, "un")]
    public void BaseUnitName_ReturnsCorrectUnit(InsumoUnit unit, string expected)
    {
        InsumoUnitHelper.BaseUnitName(unit).Should().Be(expected);
    }

    [Fact]
    public void ToBase_KG_ConvertsToGrams()
    {
        InsumoUnitHelper.ToBase(2.5m, InsumoUnit.KG).Should().Be(2500);
    }

    [Fact]
    public void ToBase_LT_ConvertsToMilliliters()
    {
        InsumoUnitHelper.ToBase(1.5m, InsumoUnit.LT).Should().Be(1500);
    }

    [Fact]
    public void ToBase_UN_NoConversion()
    {
        InsumoUnitHelper.ToBase(5m, InsumoUnit.UN).Should().Be(5);
    }

    [Fact]
    public void FromBase_Grams_ConvertsToKG()
    {
        InsumoUnitHelper.FromBase(2500, InsumoUnit.KG).Should().Be(2.5m);
    }

    [Fact]
    public void CostPerBase_KG_DividesByFactor()
    {
        InsumoUnitHelper.CostPerBase(3m, InsumoUnit.KG).Should().Be(0.003m);
    }

    [Fact]
    public void CostPerUnit_FromBase_MultipliesByFactor()
    {
        InsumoUnitHelper.CostPerUnit(0.003m, InsumoUnit.KG).Should().Be(3m);
    }
}
