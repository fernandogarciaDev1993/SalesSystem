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
    [BsonElement("isActive")]   public bool    IsActive    { get; set; } = true;
}

public enum ProductUnit { UN, KG, CX, LT, MT, PC }