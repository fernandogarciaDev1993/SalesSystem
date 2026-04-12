using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class Insumo : BaseEntity
{
    [BsonElement("code")]        public string    Code        { get; set; } = string.Empty;
    [BsonElement("name")]        public string    Name        { get; set; } = string.Empty;
    [BsonElement("description")] public string?   Description { get; set; }
    [BsonElement("unit")]        public InsumoUnit Unit       { get; set; } = InsumoUnit.KG;
    [BsonElement("baseUnit")]    public string    BaseUnit    { get; set; } = "g";
    [BsonElement("currentStock")]public long      CurrentStock { get; set; }
    [BsonElement("minStock")]    public long      MinStock     { get; set; }
    [BsonElement("averageCost")] public decimal   AverageCost  { get; set; }
    [BsonElement("lastCost")]    public decimal   LastCost     { get; set; }
    [BsonElement("isSellable")]  public bool      IsSellable   { get; set; }
    [BsonElement("salePrice")]   public decimal   SalePrice    { get; set; }
    [BsonElement("productId")]   [BsonRepresentation(BsonType.ObjectId)] public string? ProductId { get; set; }
    [BsonElement("isActive")]    public bool      IsActive     { get; set; } = true;
}

/// <summary>
/// Unidade de entrada do usuario. Internamente o estoque é armazenado na menor unidade:
/// KG → gramas (g), LT → mililitros (ml), UN/GR/ML/MT → sem conversão.
/// </summary>
public enum InsumoUnit { KG, LT, UN, GR, ML, MT }

public static class InsumoUnitHelper
{
    /// <summary>Fator de conversão: quantas unidades-base cabem em 1 unidade de entrada.</summary>
    public static int Factor(InsumoUnit unit) => unit switch
    {
        InsumoUnit.KG => 1000,   // 1 KG = 1000 g
        InsumoUnit.LT => 1000,   // 1 LT = 1000 ml
        _ => 1
    };

    /// <summary>Nome da unidade-base de armazenamento.</summary>
    public static string BaseUnitName(InsumoUnit unit) => unit switch
    {
        InsumoUnit.KG => "g",
        InsumoUnit.LT => "ml",
        InsumoUnit.GR => "g",
        InsumoUnit.ML => "ml",
        InsumoUnit.UN => "un",
        InsumoUnit.MT => "m",
        _ => "un"
    };

    /// <summary>Converte quantidade na unidade do usuario para unidade-base (inteiro).</summary>
    public static long ToBase(decimal userQty, InsumoUnit unit)
        => (long)Math.Round(userQty * Factor(unit));

    /// <summary>Converte unidade-base para unidade do usuario (para exibição).</summary>
    public static decimal FromBase(long baseQty, InsumoUnit unit)
        => (decimal)baseQty / Factor(unit);

    /// <summary>Custo por unidade-base a partir do custo por unidade do usuario.</summary>
    public static decimal CostPerBase(decimal costPerUnit, InsumoUnit unit)
        => Factor(unit) == 1 ? costPerUnit : costPerUnit / Factor(unit);

    /// <summary>Custo por unidade do usuario a partir do custo por unidade-base.</summary>
    public static decimal CostPerUnit(decimal costPerBase, InsumoUnit unit)
        => costPerBase * Factor(unit);
}
