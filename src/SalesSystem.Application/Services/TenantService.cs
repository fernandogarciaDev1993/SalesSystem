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
    Task UpdateUiConfigAsync(string tenantId, TenantUiConfig config, string savedBy);
    Task UpdateVocabularyAsync(string tenantId, TenantVocabulary vocabulary);
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
    public string PrimaryDark { get; set; } = "#1D4ED8";
    public string PrimaryLight { get; set; } = "#DBEAFE";
    public string AccentColor { get; set; } = "#F59E0B";
    public string SidebarBg { get; set; } = "#1E293B";
    public string SidebarText { get; set; } = "#F1F5F9";
    public string TopbarBg { get; set; } = "#FFFFFF";
    public string BodyBg { get; set; } = "#F8FAFC";
    public string FontFamily { get; set; } = "Inter";
    public string BorderRadius { get; set; } = "8px";
    public bool DarkMode { get; set; }
    public string? CustomCss { get; set; }

    public static TenantUiConfigDto FromEntity(TenantUiConfig config) => new()
    {
        LogoUrl            = config.LogoUrl,
        CompanyDisplayName = config.CompanyDisplayName,
        PrimaryColor       = config.PrimaryColor,
        PrimaryDark        = config.PrimaryDark,
        PrimaryLight       = config.PrimaryLight,
        AccentColor        = config.AccentColor,
        SidebarBg          = config.SidebarBg,
        SidebarText        = config.SidebarText,
        TopbarBg           = config.TopbarBg,
        BodyBg             = config.BodyBg,
        FontFamily         = config.FontFamily,
        BorderRadius       = config.BorderRadius,
        DarkMode           = config.DarkMode,
        CustomCss          = config.CustomCss
    };
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

    public async Task UpdateUiConfigAsync(string tenantId, TenantUiConfig config, string savedBy)
    {
        var tenant = await _repo.GetByIdAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        // Snapshot current config (without nested history) into history
        var snapshot = new TenantUiConfigSnapshot
        {
            SavedAt = DateTime.UtcNow,
            SavedBy = savedBy,
            Config = new TenantUiConfig
            {
                LogoUrl            = tenant.UiConfig.LogoUrl,
                FaviconUrl         = tenant.UiConfig.FaviconUrl,
                CompanyDisplayName = tenant.UiConfig.CompanyDisplayName,
                FooterText         = tenant.UiConfig.FooterText,
                PrimaryColor       = tenant.UiConfig.PrimaryColor,
                PrimaryDark        = tenant.UiConfig.PrimaryDark,
                PrimaryLight       = tenant.UiConfig.PrimaryLight,
                AccentColor        = tenant.UiConfig.AccentColor,
                DangerColor        = tenant.UiConfig.DangerColor,
                SuccessColor       = tenant.UiConfig.SuccessColor,
                SidebarBg          = tenant.UiConfig.SidebarBg,
                SidebarText        = tenant.UiConfig.SidebarText,
                SidebarAccent      = tenant.UiConfig.SidebarAccent,
                TopbarBg           = tenant.UiConfig.TopbarBg,
                BodyBg             = tenant.UiConfig.BodyBg,
                FontFamily         = tenant.UiConfig.FontFamily,
                BorderRadius       = tenant.UiConfig.BorderRadius,
                DarkMode           = tenant.UiConfig.DarkMode,
                CustomCss          = tenant.UiConfig.CustomCss,
                History            = [] // no nested history in snapshot
            }
        };

        tenant.UiConfig.History.Add(snapshot);

        // Replace UiConfig fields with the new config, preserving history
        tenant.UiConfig.LogoUrl            = config.LogoUrl;
        tenant.UiConfig.FaviconUrl         = config.FaviconUrl;
        tenant.UiConfig.CompanyDisplayName = config.CompanyDisplayName;
        tenant.UiConfig.FooterText         = config.FooterText;
        tenant.UiConfig.PrimaryColor       = config.PrimaryColor;
        tenant.UiConfig.PrimaryDark        = config.PrimaryDark;
        tenant.UiConfig.PrimaryLight       = config.PrimaryLight;
        tenant.UiConfig.AccentColor        = config.AccentColor;
        tenant.UiConfig.DangerColor        = config.DangerColor;
        tenant.UiConfig.SuccessColor       = config.SuccessColor;
        tenant.UiConfig.SidebarBg          = config.SidebarBg;
        tenant.UiConfig.SidebarText        = config.SidebarText;
        tenant.UiConfig.SidebarAccent      = config.SidebarAccent;
        tenant.UiConfig.TopbarBg           = config.TopbarBg;
        tenant.UiConfig.BodyBg             = config.BodyBg;
        tenant.UiConfig.FontFamily         = config.FontFamily;
        tenant.UiConfig.BorderRadius       = config.BorderRadius;
        tenant.UiConfig.DarkMode           = config.DarkMode;
        tenant.UiConfig.CustomCss          = config.CustomCss;

        tenant.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(tenant);
    }

    public async Task UpdateVocabularyAsync(string tenantId, TenantVocabulary vocabulary)
    {
        var tenant = await _repo.GetByIdAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        tenant.Vocabulary = vocabulary;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(tenant);
    }
}
