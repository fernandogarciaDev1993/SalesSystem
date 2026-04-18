using SalesSystem.Web.Components;
using SalesSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient pointing to the API
var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5000";
builder.Services.AddScoped<HttpClient>(sp =>
    new HttpClient { BaseAddress = new Uri(apiUrl) });

// ApiService (scoped per circuit, persists token in sessionStorage)
builder.Services.AddScoped<ApiService>();

// LabelService: resolves tenant-configurable vocabulary (e.g. "Receita" vs "Ficha Técnica")
builder.Services.AddScoped<ILabelService, LabelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
