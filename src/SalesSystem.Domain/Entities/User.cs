using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class User : BaseEntity
{
    [BsonElement("name")]                 public string          Name                 { get; set; } = string.Empty;
    [BsonElement("email")]                public string          Email                { get; set; } = string.Empty;
    [BsonElement("passwordHash")]         public string          PasswordHash         { get; set; } = string.Empty;
    [BsonElement("role")]                 public UserRole        Role                 { get; set; } = UserRole.Vendedor;
    [BsonElement("permissions")]          public List<string>    Permissions          { get; set; } = [];
    [BsonElement("isActive")]             public bool            IsActive             { get; set; } = true;
    [BsonElement("lastLogin")]            public DateTime?       LastLogin            { get; set; }
    [BsonElement("refreshTokens")]        public List<RefreshToken> RefreshTokens     { get; set; } = [];
    [BsonElement("avatarUrl")]            public string?         AvatarUrl            { get; set; }
    [BsonElement("phone")]                public string?         Phone                { get; set; }
    [BsonElement("passwordResetToken")]   public string?         PasswordResetToken   { get; set; }
    [BsonElement("passwordResetExpires")] public DateTime?       PasswordResetExpires { get; set; }
}

public class RefreshToken
{
    [BsonElement("token")]     public string    Token     { get; set; } = string.Empty;
    [BsonElement("expires")]   public DateTime  Expires   { get; set; }
    [BsonElement("createdAt")] public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("revokedAt")] public DateTime? RevokedAt { get; set; }
    [BsonElement("userAgent")] public string?   UserAgent { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive  => !IsExpired && !IsRevoked;
}

public enum UserRole { Admin, Gerente, Vendedor, Estoquista, Financeiro, Viewer }

public static class Permission
{
    public const string ProductsRead   = "products.read";
    public const string ProductsWrite  = "products.write";
    public const string ProductsDelete = "products.delete";
    public const string CustomersRead  = "customers.read";
    public const string CustomersWrite = "customers.write";
    public const string StockRead      = "stock.read";
    public const string StockWrite     = "stock.write";
    public const string OrdersRead     = "orders.read";
    public const string OrdersCreate   = "orders.create";
    public const string OrdersConfirm  = "orders.confirm";
    public const string OrdersCancel   = "orders.cancel";
    public const string FinancialRead  = "financial.read";
    public const string FinancialWrite = "financial.write";
    public const string ConfigWrite    = "config.write";
    public const string UsersWrite     = "users.write";

    public static List<string> DefaultFor(UserRole role) => role switch
    {
        UserRole.Admin      => [ProductsRead,ProductsWrite,ProductsDelete,CustomersRead,CustomersWrite,StockRead,StockWrite,OrdersRead,OrdersCreate,OrdersConfirm,OrdersCancel,FinancialRead,FinancialWrite,ConfigWrite,UsersWrite],
        UserRole.Gerente    => [ProductsRead,ProductsWrite,CustomersRead,CustomersWrite,StockRead,StockWrite,OrdersRead,OrdersCreate,OrdersConfirm,OrdersCancel,FinancialRead,FinancialWrite],
        UserRole.Vendedor   => [ProductsRead,CustomersRead,CustomersWrite,StockRead,OrdersRead,OrdersCreate],
        UserRole.Estoquista => [ProductsRead,ProductsWrite,StockRead,StockWrite],
        UserRole.Financeiro => [FinancialRead,FinancialWrite,OrdersRead],
        UserRole.Viewer     => [ProductsRead,CustomersRead,StockRead,OrdersRead,FinancialRead],
        _ => []
    };
}