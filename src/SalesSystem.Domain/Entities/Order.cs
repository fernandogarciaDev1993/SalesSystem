using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class Order : BaseEntity
{
    [BsonElement("orderNumber")]     public string          OrderNumber      { get; set; } = string.Empty;
    [BsonElement("type")]            public OrderType       Type             { get; set; } = OrderType.Venda;
    [BsonElement("status")]          public OrderStatus     Status           { get; set; } = OrderStatus.Rascunho;
    [BsonElement("customerId")]      [BsonRepresentation(BsonType.ObjectId)] public string CustomerId { get; set; } = string.Empty;
    [BsonElement("customerName")]    public string          CustomerName     { get; set; } = string.Empty;
    [BsonElement("customerDocument")]public string          CustomerDocument { get; set; } = string.Empty;
    [BsonElement("sellerId")]        public string          SellerId         { get; set; } = string.Empty;
    [BsonElement("sellerName")]      public string          SellerName       { get; set; } = string.Empty;
    [BsonElement("items")]           public List<OrderItem>    Items         { get; set; } = [];
    [BsonElement("subtotal")]        public decimal         Subtotal         { get; set; }
    [BsonElement("discountTotal")]   public decimal         DiscountTotal    { get; set; }
    [BsonElement("taxTotal")]        public decimal         TaxTotal         { get; set; }
    [BsonElement("shippingCost")]    public decimal         ShippingCost     { get; set; }
    [BsonElement("total")]           public decimal         Total            { get; set; }
    [BsonElement("totalCost")]       public decimal         TotalCost        { get; set; }
    [BsonElement("grossMargin")]     public decimal         GrossMargin      { get; set; }
    [BsonElement("grossMarginPct")]  public decimal         GrossMarginPct   { get; set; }
    [BsonElement("payments")]        public List<OrderPayment> Payments      { get; set; } = [];
    [BsonElement("amountPaid")]      public decimal         AmountPaid       { get; set; }
    [BsonElement("amountDue")]       public decimal         AmountDue        { get; set; }
    [BsonElement("deliveryAddress")] public CustomerAddress? DeliveryAddress { get; set; }
    [BsonElement("notes")]           public string?         Notes            { get; set; }
    [BsonElement("internalNotes")]   public string?         InternalNotes    { get; set; }
    [BsonElement("confirmedAt")]     public DateTime?       ConfirmedAt      { get; set; }
    [BsonElement("cancelledAt")]     public DateTime?       CancelledAt      { get; set; }
    [BsonElement("cancelReason")]    public string?         CancelReason     { get; set; }
}

public class OrderItem
{
    [BsonElement("productId")]     [BsonRepresentation(BsonType.ObjectId)] public string  ProductId     { get; set; } = string.Empty;
    [BsonElement("sku")]           public string  Sku            { get; set; } = string.Empty;
    [BsonElement("name")]          public string  Name           { get; set; } = string.Empty;
    [BsonElement("unit")]          public string  Unit           { get; set; } = "UN";
    [BsonElement("quantity")]      public decimal Quantity       { get; set; }
    [BsonElement("unitPrice")]     public decimal UnitPrice      { get; set; }
    [BsonElement("discountPct")]   public decimal DiscountPct    { get; set; }
    [BsonElement("discountAmount")]public decimal DiscountAmount { get; set; }
    [BsonElement("unitCost")]      public decimal UnitCost       { get; set; }
    [BsonElement("taxRate")]       public decimal TaxRate        { get; set; }
    [BsonElement("taxAmount")]     public decimal TaxAmount      { get; set; }
    [BsonElement("totalPrice")]    public decimal TotalPrice     { get; set; }
    [BsonElement("totalCost")]     public decimal TotalCost      { get; set; }
    [BsonElement("ncm")]           public string? Ncm            { get; set; }
    [BsonElement("cfop")]          public string? Cfop           { get; set; }
}

public class OrderPayment
{
    [BsonElement("method")]       public PaymentMethod  Method       { get; set; }
    [BsonElement("amount")]       public decimal        Amount       { get; set; }
    [BsonElement("installments")] public int            Installments { get; set; } = 1;
    [BsonElement("dueDate")]      public DateTime       DueDate      { get; set; }
    [BsonElement("paidAt")]       public DateTime?      PaidAt       { get; set; }
    [BsonElement("status")]       public PaymentStatus  Status       { get; set; } = PaymentStatus.Pendente;
    [BsonElement("reference")]    public string?        Reference    { get; set; }
}

public enum OrderType     { Venda, Orcamento, PedidoFuturo }
public enum OrderStatus   { Rascunho, Confirmado, Faturado, Cancelado, Devolvido }
public enum PaymentMethod { Dinheiro, Pix, CartaoCredito, CartaoDebito, Boleto, Transferencia, Cheque, Crediario }
public enum PaymentStatus { Pendente, Pago, Vencido, Cancelado }