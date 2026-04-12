using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class FinancialEntry : BaseEntity
{
    [BsonElement("code")]             public string            Code             { get; set; } = string.Empty;
    [BsonElement("type")]             public FinancialType     Type             { get; set; }
    [BsonElement("category")]         public FinancialCategory Category         { get; set; }
    [BsonElement("description")]      public string            Description      { get; set; } = string.Empty;
    [BsonElement("amount")]           public decimal           Amount           { get; set; }
    [BsonElement("amountPaid")]       public decimal           AmountPaid       { get; set; }
    [BsonElement("amountDue")]        public decimal           AmountDue        { get; set; }
    [BsonElement("dueDate")]          public DateTime          DueDate          { get; set; }
    [BsonElement("paymentDate")]      public DateTime?         PaymentDate      { get; set; }
    [BsonElement("status")]           public FinancialStatus   Status           { get; set; } = FinancialStatus.Pendente;
    [BsonElement("referenceId")]      public string?           ReferenceId      { get; set; }
    [BsonElement("referenceType")]    public string?           ReferenceType    { get; set; }
    [BsonElement("paymentMethod")]    public string?           PaymentMethod    { get; set; }
    [BsonElement("costCenterId")]     public string?           CostCenterId     { get; set; }
    [BsonElement("costCenterName")]   public string?           CostCenterName   { get; set; }
    [BsonElement("attachments")]      public List<string>      Attachments      { get; set; } = [];
    [BsonElement("notes")]            public string?           Notes            { get; set; }
    [BsonElement("customerId")]       public string?           CustomerId       { get; set; }
    [BsonElement("customerName")]     public string?           CustomerName     { get; set; }
    [BsonElement("installmentIndex")] public int               InstallmentIndex { get; set; } = 1;
    [BsonElement("installmentTotal")] public int               InstallmentTotal { get; set; } = 1;
}

public enum FinancialType     { Receita, Despesa }
public enum FinancialStatus   { Pendente, Pago, Vencido, Cancelado }
public enum FinancialCategory { Venda, Compra, FolhaPagamento, Aluguel, Imposto, Servico, Marketing, Logistica, Outro, CaixaInicial, Gasolina, AporteFinanceiro }