using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class Recipe : BaseEntity
{
    [BsonElement("code")]           public string Code { get; set; } = string.Empty;
    [BsonElement("name")]           public string Name { get; set; } = string.Empty;
    [BsonElement("description")]    public string? Description { get; set; }
    [BsonElement("ingredients")]    public List<RecipeIngredient> Ingredients { get; set; } = [];
    [BsonElement("steps")]          public List<RecipeStep> Steps { get; set; } = [];
    [BsonElement("calculatedCost")] public decimal CalculatedCost { get; set; }
    [BsonElement("yieldQuantity")]  public int YieldQuantity { get; set; } = 1;
    [BsonElement("costPerUnit")]    public decimal CostPerUnit { get; set; }
    [BsonElement("isInsumo")]       public bool IsInsumo { get; set; }
    [BsonElement("outputInsumoId")] [BsonRepresentation(BsonType.ObjectId)] public string? OutputInsumoId { get; set; }
    [BsonElement("category")]        public string Category { get; set; } = string.Empty;
    [BsonElement("isActive")]       public bool IsActive { get; set; } = true;
}

public static class RecipeCategories
{
    public static readonly List<string> All =
    [
        "Prato Principal",
        "Acompanhamento",
        "Sobremesa",
        "Bebida",
        "Molho",
        "Salada",
        "Entrada",
        "Lanche",
        "Sopa",
        "Base",
        "Outro"
    ];
}

public class RecipeIngredient
{
    [BsonElement("insumoId")]   [BsonRepresentation(BsonType.ObjectId)] public string InsumoId { get; set; } = string.Empty;
    [BsonElement("insumoName")] public string InsumoName { get; set; } = string.Empty;
    [BsonElement("insumoCode")] public string InsumoCode { get; set; } = string.Empty;
    [BsonElement("quantity")]   public decimal Quantity { get; set; }
    [BsonElement("unit")]       public string Unit { get; set; } = string.Empty;
    [BsonElement("unitCost")]   public decimal UnitCost { get; set; }
    [BsonElement("totalCost")]  public decimal TotalCost { get; set; }
    [BsonElement("isRecipe")]   public bool IsRecipe { get; set; }
}

public class RecipeStep
{
    [BsonElement("order")]       public int Order { get; set; }
    [BsonElement("description")] public string Description { get; set; } = string.Empty;
}
