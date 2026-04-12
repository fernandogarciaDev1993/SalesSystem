using Microsoft.AspNetCore.Http;
using SalesSystem.Application.Services;

namespace SalesSystem.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    private static readonly string[] _skipPaths = ["/openapi", "/scalar", "/api/tenants", "/api/auth", "/api/setup", "/api/admin"];

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var path = context.Request.Path.Value ?? "";
        if (_skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        string? subdomain = null;

        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var hdr))
            subdomain = hdr.ToString();
        else
        {
            var parts = context.Request.Host.Host.Split(".");
            if (parts.Length >= 3) subdomain = parts[0];
        }

        if (string.IsNullOrEmpty(subdomain))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant nao identificado." });
            return;
        }

        var tenant = await tenantService.GetBySubdomainAsync(subdomain);

        if (tenant is not null)
        {
            if (!tenant.IsActive) { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { error = "Tenant inativo." }); return; }
            context.Items["TenantId"]      = tenant.Id;
            context.Items["TenantModules"] = tenant.Modules;
        }
        else
        {
            // Development: use the header value directly as tenantId
            context.Items["TenantId"]      = subdomain;
            context.Items["TenantModules"] = new List<string> { "produtos","clientes","estoque","pedidos","financeiro","insumos" };
        }

        await _next(context);
    }
}