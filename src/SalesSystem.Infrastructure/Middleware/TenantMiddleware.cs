using Microsoft.AspNetCore.Http;
using SalesSystem.Application.Services;

namespace SalesSystem.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
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

        if (tenant is null)        { context.Response.StatusCode = 404; await context.Response.WriteAsJsonAsync(new { error = "Tenant nao encontrado." }); return; }
        if (!tenant.IsActive)      { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { error = "Tenant inativo." }); return; }

        context.Items["TenantId"]      = tenant.Id;
        context.Items["TenantModules"] = tenant.Modules;

        await _next(context);
    }
}