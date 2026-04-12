using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Scalar.AspNetCore;
using SalesSystem.Application.Interfaces;
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
builder.Services.AddScoped<IInsumoRepository,         InsumoRepository>();
builder.Services.AddScoped<IInsumoPurchaseRepository,  InsumoPurchaseRepository>();
builder.Services.AddScoped<IRecipeRepository,          RecipeRepository>();

// Services
builder.Services.AddScoped<ITenantService,    TenantService>();
builder.Services.AddScoped<IAuthService,      AuthService>();
builder.Services.AddScoped<IUserService,      UserService>();
builder.Services.AddScoped<IProductService,   ProductService>();
builder.Services.AddScoped<ICustomerService,  CustomerService>();
builder.Services.AddScoped<IStockService,     StockService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IOrderService,     OrderService>();
builder.Services.AddScoped<IRecipeService,  RecipeService>();
builder.Services.AddScoped<IInsumoService,  InsumoService>();

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

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("GlobalAdmin", policy =>
        policy.RequireClaim("permission", "admin.global"));
});
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddHostedService<MongoIndexSetup>();
builder.Services.AddHostedService<DataSeeder>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "SalesSystem API";
    options.Theme = ScalarTheme.BluePlanet;
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapControllers();
app.Run();

// Indices MongoDB
public class MongoIndexSetup : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public MongoIndexSetup(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

            await CreateIndex(db, "products",    "tenantId", "isActive");
            await CreateUniqueIndex(db, "products", "tenantId", "sku");
            await CreateIndex(db, "orders",      "tenantId", "status");
            await CreateIndex(db, "stock_moves", "tenantId", "productId");
            await CreateIndex(db, "financials",  "tenantId", "status");
            await CreateIndex(db, "customers",   "tenantId", "document");
            await CreateUniqueIndex(db, "insumos", "tenantId", "code");
            await CreateIndex(db, "recipes", "tenantId", "isActive");
            await CreateIndex(db, "insumo_purchases", "tenantId", "insumoId");
            await CreateUniqueIndex(db, "tenants", "subdomain");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoIndexSetup] Warning: Could not create indexes - {ex.Message}");
        }
    }

    private static async Task CreateIndex(IMongoDatabase db, string col, params string[] fields)
    {
        var c = db.GetCollection<MongoDB.Bson.BsonDocument>(col);
        var keys = Builders<MongoDB.Bson.BsonDocument>.IndexKeys;
        var k = keys.Ascending(fields[0]);
        for (int i = 1; i < fields.Length; i++) k = keys.Ascending(fields[i]);
        await c.Indexes.CreateOneAsync(new CreateIndexModel<MongoDB.Bson.BsonDocument>(k));
    }

    private static async Task CreateUniqueIndex(IMongoDatabase db, string col, params string[] fields)
    {
        var c = db.GetCollection<MongoDB.Bson.BsonDocument>(col);
        var keys = Builders<MongoDB.Bson.BsonDocument>.IndexKeys;
        var k = keys.Ascending(fields[0]);
        for (int i = 1; i < fields.Length; i++) k = keys.Ascending(fields[i]);
        await c.Indexes.CreateOneAsync(new CreateIndexModel<MongoDB.Bson.BsonDocument>(
            k, new CreateIndexOptions { Unique = true }));
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

// Data Seeder - creates default tenant and admin user on startup
public class DataSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public DataSeeder(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Create default tenant if it doesn't exist
            var tenant = await tenantRepo.GetBySubdomainAsync("demo");
            if (tenant is null)
            {
                tenant = new SalesSystem.Domain.Entities.Tenant
                {
                    Name = "Empresa Demo",
                    Subdomain = "demo",
                    IsActive = true,
                    Modules = ["produtos", "clientes", "estoque", "pedidos", "financeiro", "insumos"]
                };
                tenant = await tenantRepo.InsertAsync(tenant);
                Console.WriteLine($"[DataSeeder] Created default tenant 'demo' with Id={tenant.Id}");
            }

            // Create default admin user if it doesn't exist
            var user = await userRepo.GetByEmailAsync("admin@admin.com", tenant.Id);
            if (user is null)
            {
                user = new SalesSystem.Domain.Entities.User
                {
                    Name = "Administrador",
                    Email = "admin@admin.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                    Role = SalesSystem.Domain.Entities.UserRole.Admin,
                    Permissions = [..SalesSystem.Domain.Entities.Permission.DefaultFor(SalesSystem.Domain.Entities.UserRole.Admin), SalesSystem.Domain.Entities.Permission.AdminGlobal],
                    TenantId = tenant.Id,
                    IsActive = true
                };
                await userRepo.InsertAsync(user);
                Console.WriteLine($"[DataSeeder] Created default admin user 'admin@admin.com'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataSeeder] Warning: Could not seed data - {ex.Message}");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}