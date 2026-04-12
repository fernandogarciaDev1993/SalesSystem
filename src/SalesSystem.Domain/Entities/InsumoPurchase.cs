using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class InsumoPurchase : BaseEntity
{
    [BsonElement("insumoId")]   [BsonRepresentation(BsonType.ObjectId)] public string  InsumoId   { get; set; } = string.Empty;
    [BsonElement("insumoName")] public string  InsumoName  { get; set; } = string.Empty;
    [BsonElement("insumoCode")] public string  InsumoCode  { get; set; } = string.Empty;
    [BsonElement("quantity")]   public long    Quantity    { get; set; }
    [BsonElement("unitCost")]   public decimal UnitCost    { get; set; }
    [BsonElement("totalCost")]  public decimal TotalCost   { get; set; }
    [BsonElement("previousStock")]   public long    PreviousStock   { get; set; }
    [BsonElement("newStock")]        public long    NewStock        { get; set; }
    [BsonElement("previousAvgCost")] public decimal PreviousAvgCost { get; set; }
    [BsonElement("newAvgCost")]      public decimal NewAvgCost      { get; set; }
    [BsonElement("supplierId")]      public string? SupplierId      { get; set; }
    [BsonElement("note")]            public string? Note            { get; set; }
    [BsonElement("remainingStock")]  public long    RemainingStock  { get; set; }
    [BsonElement("inputUnit")]       public string  InputUnit       { get; set; } = string.Empty;
    [BsonElement("inputQuantity")]   public decimal InputQuantity   { get; set; }
    [BsonElement("inputUnitCost")]   public decimal InputUnitCost   { get; set; }
}
