using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SalesSystem.Application.Services;
using SalesSystem.Infrastructure.Middleware;
using SalesSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
var mongoConn = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoDb   = builder.Configuration["MongoDB:Database"] ?? "salesdb";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
builder.Services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDb));

// Repositories
builder.Services.AddScoped<ITenantRepository,       TenantRepository>();
builder.Services.AddScoped<IUserRepository,         UserRepository>();
builder.Services.AddScoped<IProductRepository,      ProductRepository>();
builder.Services.AddScoped<ICustomerRepository,     CustomerRepository>();
builder.Services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();
builder.Services.AddScoped<IStockMoveRepository,    StockMoveRepository>();
builder.Services.AddScoped<IOrderRepository,        OrderRepository>();
builder.Services.AddScoped<IFinancialRepository,    FinancialRepository>();

// Services
builder.Services.AddScoped<ITenantService,    TenantService>();
builder.Services.AddScoped<IAuthService,      AuthService>();
builder.Services.AddScoped<IUserService,      UserService>();
builder.Services.AddScoped<IProductService,   ProductService>();
builder.Services.AddScoped<ICustomerService,  CustomerService>();
builder.Services.AddScoped<IStockService,     StockService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IOrderService,     OrderService>();

builder.Services.AddHttpContextAccessor();

// JWT
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret nao configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<MongoIndexSetup>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapControllers();
app.Run();

// Indices MongoDB
public class MongoIndexSetup : IHostedService
{
    private readonly IMongoDatabase _db;
    public MongoIndexSetup(IMongoDatabase db) => _db = db;

    public async Task StartAsync(CancellationToken ct)
    {
        await CreateIndex("products",    "tenantId", "isActive");
        await CreateUniqueIndex("products", "tenantId", "sku");
        await CreateIndex("orders",      "tenantId", "status");
        await CreateIndex("stock_moves", "tenantId", "productId");
        await CreateIndex("financials",  "tenantId", "status");
        await CreateIndex("customers",   "tenantId", "document");
        await CreateUniqueIndex("tenants", "subdomain");
    }

    private async Task CreateIndex(string col, params string[] fields)
    {
        var c = _db.GetCollection<MongoDB.Bson.BsonDocument>(col);
        var keys = Builders<MongoDB.Bson.BsonDocument>.IndexKeys;
        var k = keys.Ascending(fields[0]);
        for (int i = 1; i < fields.Length; i++) k = keys.Ascending(fields[i]);
        await c.Indexes.CreateOneAsync(new CreateIndexModel<MongoDB.Bson.BsonDocument>(k));
    }

    private async Task CreateUniqueIndex(string col, params string[] fields)
    {
        var c = _db.GetCollection<MongoDB.Bson.BsonDocument>(col);
        var keys = Builders<MongoDB.Bson.BsonDocument>.IndexKeys;
        var k = keys.Ascending(fields[0]);
        for (int i = 1; i < fields.Length; i++) k = keys.Ascending(fields[i]);
        await c.Indexes.CreateOneAsync(new CreateIndexModel<MongoDB.Bson.BsonDocument>(
            k, new CreateIndexOptions { Unique = true }));
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}