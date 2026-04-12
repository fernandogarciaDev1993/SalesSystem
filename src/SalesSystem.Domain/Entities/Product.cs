using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class Product : BaseEntity
{
    [BsonElement("sku")]        public string  Sku         { get; set; } = string.Empty;
    [BsonElement("barcode")]    public string? Barcode     { get; set; }
    [BsonElement("name")]       public string  Name        { get; set; } = string.Empty;
    [BsonElement("description")]public string? Description { get; set; }
    [BsonElement("categoryId")] [BsonRepresentation(BsonType.ObjectId)] public string? CategoryId  { get; set; }
    [BsonElement("supplierId")] [BsonRepresentation(BsonType.ObjectId)] public string? SupplierId  { get; set; }
    [BsonElement("images")]     public List<string> Images { get; set; } = [];
    [BsonElement("unit")]       public ProductUnit  Unit   { get; set; } = ProductUnit.UN;
    [BsonElement("costPrice")]  public decimal CostPrice   { get; set; }
    [BsonElement("salePrice")]  public decimal SalePrice   { get; set; }
    [BsonElement("minSalePrice")] public decimal MinSalePrice { get; set; }
    [BsonElement("taxRate")]    public decimal TaxRate     { get; set; }
    [BsonElement("ncm")]        public string? Ncm         { get; set; }
    [BsonElement("cfop")]       public string? Cfop        { get; set; }
    [BsonElement("recipeId")]       [BsonRepresentation(BsonType.ObjectId)] public string? RecipeId { get; set; }
    [BsonElement("insumoId")]       [BsonRepresentation(BsonType.ObjectId)] public string? InsumoId { get; set; }
    [BsonElement("recipe")]         public List<RecipeItem> Recipe         { get; set; } = [];
    [BsonElement("calculatedCost")] public decimal          CalculatedCost { get; set; }
    [BsonElement("hasRecipe")]      public bool             HasRecipe      { get; set; }
    [BsonElement("operationalCostPct")] public decimal OperationalCostPct { get; set; }
    [BsonElement("profitMarginPct")]    public decimal ProfitMarginPct    { get; set; }
    [BsonElement("isActive")]   public bool    IsActive    { get; set; } = true;
}

public class RecipeItem
{
    [BsonElement("insumoId")]   [BsonRepresentation(BsonType.ObjectId)] public string  InsumoId   { get; set; } = string.Empty;
    [BsonElement("insumoName")] public string  InsumoName  { get; set; } = string.Empty;
    [BsonElement("insumoCode")] public string  InsumoCode  { get; set; } = string.Empty;
    [BsonElement("quantity")]   public decimal Quantity     { get; set; }
    [BsonElement("unit")]       public string  Unit         { get; set; } = "KG";
    [BsonElement("unitCost")]   public decimal UnitCost     { get; set; }
    [BsonElement("totalCost")]  public decimal TotalCost    { get; set; }
}

public enum ProductUnit { UN, KG, CX, LT, MT, PC }