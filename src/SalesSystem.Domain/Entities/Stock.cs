using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class StockBalance : BaseEntity
{
    [BsonElement("productId")]   [BsonRepresentation(BsonType.ObjectId)] public string  ProductId        { get; set; } = string.Empty;
    [BsonElement("productName")] public string  ProductName      { get; set; } = string.Empty;
    [BsonElement("productSku")]  public string  ProductSku       { get; set; } = string.Empty;
    [BsonElement("currentBalance")]   public decimal CurrentBalance   { get; set; }
    [BsonElement("reservedBalance")]  public decimal ReservedBalance  { get; set; }
    [BsonElement("availableBalance")] public decimal AvailableBalance { get; set; }
    [BsonElement("minBalance")]  public decimal MinBalance       { get; set; }
    [BsonElement("maxBalance")]  public decimal MaxBalance       { get; set; }
    [BsonElement("averageCost")] public decimal AverageCost      { get; set; }
    [BsonElement("unit")]        public string  Unit             { get; set; } = "UN";
}

public class StockMove : BaseEntity
{
    [BsonElement("productId")]   [BsonRepresentation(BsonType.ObjectId)] public string        ProductId       { get; set; } = string.Empty;
    [BsonElement("productName")] public string        ProductName     { get; set; } = string.Empty;
    [BsonElement("productSku")]  public string        ProductSku      { get; set; } = string.Empty;
    [BsonElement("type")]        public StockMoveType Type            { get; set; }
    [BsonElement("quantity")]    public decimal       Quantity        { get; set; }
    [BsonElement("previousBalance")] public decimal   PreviousBalance { get; set; }
    [BsonElement("newBalance")]  public decimal       NewBalance      { get; set; }
    [BsonElement("unitCost")]    public decimal       UnitCost        { get; set; }
    [BsonElement("totalCost")]   public decimal       TotalCost       { get; set; }
    [BsonElement("referenceId")] public string?       ReferenceId     { get; set; }
    [BsonElement("referenceType")] public string?     ReferenceType   { get; set; }
    [BsonElement("note")]        public string?       Note            { get; set; }
    [BsonElement("userId")]      public string        UserId          { get; set; } = string.Empty;
}

public enum StockMoveType { Entrada, Saida, Ajuste, Devolucao, Reserva, Liberacao }