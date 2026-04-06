using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class Customer : BaseEntity
{
    [BsonElement("type")]              public CustomerType       Type              { get; set; } = CustomerType.PF;
    [BsonElement("name")]              public string             Name              { get; set; } = string.Empty;
    [BsonElement("tradeName")]         public string?            TradeName         { get; set; }
    [BsonElement("document")]          public string             Document          { get; set; } = string.Empty;
    [BsonElement("stateRegistration")] public string?            StateRegistration { get; set; }
    [BsonElement("email")]             public string?            Email             { get; set; }
    [BsonElement("phone")]             public string?            Phone             { get; set; }
    [BsonElement("mobile")]            public string?            Mobile            { get; set; }
    [BsonElement("addresses")]         public List<CustomerAddress> Addresses      { get; set; } = [];
    [BsonElement("creditLimit")]       public decimal            CreditLimit       { get; set; }
    [BsonElement("currentDebt")]       public decimal            CurrentDebt       { get; set; }
    [BsonElement("paymentTermDays")]   public int                PaymentTermDays   { get; set; }
    [BsonElement("tags")]              public List<string>       Tags              { get; set; } = [];
    [BsonElement("notes")]             public string?            Notes             { get; set; }
    [BsonElement("isActive")]          public bool               IsActive          { get; set; } = true;
}

public class CustomerAddress
{
    [BsonElement("type")]       public AddressType Type       { get; set; } = AddressType.Principal;
    [BsonElement("zipCode")]    public string      ZipCode    { get; set; } = string.Empty;
    [BsonElement("street")]     public string      Street     { get; set; } = string.Empty;
    [BsonElement("number")]     public string      Number     { get; set; } = string.Empty;
    [BsonElement("complement")] public string?     Complement { get; set; }
    [BsonElement("district")]   public string      District   { get; set; } = string.Empty;
    [BsonElement("city")]       public string      City       { get; set; } = string.Empty;
    [BsonElement("state")]      public string      State      { get; set; } = string.Empty;
    [BsonElement("ibgeCode")]   public string?     IbgeCode   { get; set; }
}

public enum CustomerType { PF, PJ }
public enum AddressType  { Principal, Entrega, Cobranca }