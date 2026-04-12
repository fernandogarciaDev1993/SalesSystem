using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface ITenantService
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<Tenant?> GetByIdAsync(string id);
    Task<List<Tenant>> GetAllAsync();
    Task<Tenant> CreateAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
}

public interface ITenantContext
{
    string TenantId { get; }
    List<string> Modules { get; }
}

public class TenantUiConfigDto
{
    public string? LogoUrl { get; set; }
    public string? CompanyDisplayName { get; set; }
    public string PrimaryColor { get; set; } = "#2563EB";
    public string SidebarBg { get; set; } = "#1E293B";
    public string SidebarText { get; set; } = "#F1F5F9";
    public string BodyBg { get; set; } = "#F8FAFC";
    public string FontFamily { get; set; } = "Inter";
    public string BorderRadius { get; set; } = "8px";
    public bool DarkMode { get; set; }
    public string? CustomCss { get; set; }
}

public class TenantService : ITenantService
{
    private readonly ITenantRepository _repo;

    public TenantService(ITenantRepository repo)
    {
        _repo = repo;
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
        => await _repo.GetBySubdomainAsync(subdomain);

    public async Task<Tenant?> GetByIdAsync(string id)
        => await _repo.GetByIdAsync(id);

    public async Task<List<Tenant>> GetAllAsync()
        => await _repo.GetAllAsync();

    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        var existing = await _repo.GetBySubdomainAsync(tenant.Subdomain);
        if (existing is not null)
            throw new InvalidOperationException($"Subdomain '{tenant.Subdomain}' already exists.");

        return await _repo.InsertAsync(tenant);
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        var existing = await _repo.GetByIdAsync(tenant.Id);
        if (existing is null)
            throw new InvalidOperationException("Tenant not found.");

        await _repo.UpdateAsync(tenant);
    }
}
