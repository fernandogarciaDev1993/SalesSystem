using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace SalesSystem.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
    private string? _token;
    private bool _initialized;

    public ApiService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public string? UserName { get; private set; }
    public string? UserRole { get; private set; }
    public string? TenantId { get; private set; }
    public string? TenantSubdomain { get; private set; }
    public List<string> Permissions { get; private set; } = [];
    public bool IsGlobalAdmin => Permissions.Contains("admin.global");
    public UiConfigDto? CurrentTheme { get; set; }
    public VocabularyDto? CurrentVocabulary { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _token = await _js.InvokeAsync<string?>("authStorage.get", "auth_token");
            UserName = await _js.InvokeAsync<string?>("authStorage.get", "auth_userName");
            UserRole = await _js.InvokeAsync<string?>("authStorage.get", "auth_userRole");
            TenantId = await _js.InvokeAsync<string?>("authStorage.get", "auth_tenantId");
            TenantSubdomain = await _js.InvokeAsync<string?>("authStorage.get", "auth_tenantSubdomain");

            var permissionsJson = await _js.InvokeAsync<string?>("authStorage.get", "auth_permissions");
            if (!string.IsNullOrEmpty(permissionsJson))
            {
                Permissions = JsonSerializer.Deserialize<List<string>>(permissionsJson, _jsonOpts) ?? [];
            }

            var themeJson = await _js.InvokeAsync<string?>("authStorage.get", "auth_theme");
            if (!string.IsNullOrEmpty(themeJson))
            {
                CurrentTheme = JsonSerializer.Deserialize<UiConfigDto>(themeJson, _jsonOpts);
            }

            var vocabJson = await _js.InvokeAsync<string?>("authStorage.get", "auth_vocabulary");
            if (!string.IsNullOrEmpty(vocabJson))
            {
                CurrentVocabulary = JsonSerializer.Deserialize<VocabularyDto>(vocabJson, _jsonOpts);
            }

            if (!string.IsNullOrEmpty(_token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
        }
        catch
        {
            // JS interop not available during prerender
        }
    }

    public async Task<LoginResult> LoginAsync(string email, string password, string tenantSubdomain)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new
            {
                email,
                password,
                tenantId = tenantSubdomain
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await GetErrorAsync(response);
                return new LoginResult { Success = false, Error = error ?? "Email ou senha invalidos." };
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOpts);
            if (auth is null || string.IsNullOrEmpty(auth.Token))
                return new LoginResult { Success = false, Error = "Resposta invalida do servidor." };

            // Set in memory
            _token = auth.Token;
            UserName = auth.Name;
            UserRole = auth.Role;
            TenantId = auth.TenantId;
            TenantSubdomain = auth.TenantSubdomain;
            Permissions = auth.Permissions ?? [];
            CurrentTheme = auth.UiConfig;
            CurrentVocabulary = auth.Vocabulary;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            // Persist in sessionStorage
            await _js.InvokeVoidAsync("authStorage.set", "auth_token", _token);
            await _js.InvokeVoidAsync("authStorage.set", "auth_userName", UserName);
            await _js.InvokeVoidAsync("authStorage.set", "auth_userRole", UserRole);
            await _js.InvokeVoidAsync("authStorage.set", "auth_tenantId", TenantId);
            await _js.InvokeVoidAsync("authStorage.set", "auth_tenantSubdomain", TenantSubdomain);
            await _js.InvokeVoidAsync("authStorage.set", "auth_permissions", JsonSerializer.Serialize(Permissions));
            if (CurrentTheme is not null)
                await _js.InvokeVoidAsync("authStorage.set", "auth_theme", JsonSerializer.Serialize(CurrentTheme));
            if (CurrentVocabulary is not null)
                await _js.InvokeVoidAsync("authStorage.set", "auth_vocabulary", JsonSerializer.Serialize(CurrentVocabulary));

            return new LoginResult { Success = true };
        }
        catch
        {
            return new LoginResult { Success = false, Error = "Erro ao conectar com o servidor." };
        }
    }

    public async Task LogoutAsync()
    {
        _token = null;
        UserName = null;
        UserRole = null;
        TenantId = null;
        TenantSubdomain = null;
        Permissions = [];
        CurrentTheme = null;
        CurrentVocabulary = null;
        _http.DefaultRequestHeaders.Authorization = null;

        try
        {
            await _js.InvokeVoidAsync("authStorage.remove", "auth_token");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_userName");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_userRole");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_tenantId");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_tenantSubdomain");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_permissions");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_theme");
            await _js.InvokeVoidAsync("authStorage.remove", "auth_vocabulary");
            await _js.InvokeAsync<object>("themeManager.clear");
        }
        catch { }
    }

    private void SetTenantHeader()
    {
        _http.DefaultRequestHeaders.Remove("X-Tenant-ID");
        var tenant = TenantSubdomain ?? "demo";
        _http.DefaultRequestHeaders.Add("X-Tenant-ID", tenant);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(_jsonOpts);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PostAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOpts);
    }

    public async Task<bool> PostAsync<TRequest>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PostAsJsonAsync(url, data);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostAsync(string url)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PostAsync(url, null);
        return response.IsSuccessStatusCode;
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PutAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOpts);
    }

    public async Task<bool> PutAsync<TRequest>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PutAsJsonAsync(url, data);
        return response.IsSuccessStatusCode;
    }

    public async Task<RawResponse> PostRawAsync<TRequest>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PostAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode)
        {
            var error = await GetErrorAsync(response);
            return new RawResponse { IsSuccess = false, Error = error ?? $"HTTP {(int)response.StatusCode}" };
        }
        return new RawResponse { IsSuccess = true };
    }

    public async Task<(TResponse? Data, string? Error)> PutWithErrorAsync<TRequest, TResponse>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PutAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode)
        {
            var error = await GetErrorAsync(response);
            return (default, error ?? $"HTTP {(int)response.StatusCode}");
        }
        try
        {
            var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOpts);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (default, $"Erro ao processar resposta: {ex.Message}");
        }
    }

    public async Task<bool> DeleteAsync(string url)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task<(TResponse? Data, string? Error)> PostWithErrorAsync<TRequest, TResponse>(string url, TRequest data)
    {
        await EnsureInitializedAsync();
        SetTenantHeader();
        var response = await _http.PostAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode)
        {
            var error = await GetErrorAsync(response);
            return (default, error ?? $"HTTP {(int)response.StatusCode}");
        }
        try
        {
            var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOpts);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (default, $"Erro ao processar resposta: {ex.Message}");
        }
    }

    public async Task<string?> GetErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOpts);
            return error?.Error;
        }
        catch
        {
            return "Erro desconhecido";
        }
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime Expires { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
    public string TenantId { get; set; } = "";
    public string TenantSubdomain { get; set; } = "";
    public UiConfigDto? UiConfig { get; set; }
    public VocabularyDto? Vocabulary { get; set; }
}

public class VocabularyDto
{
    public string PresetId { get; set; } = "confeitaria";
    public Dictionary<string, VocabularyTermDto> Terms { get; set; } = [];
}

public class VocabularyTermDto
{
    public string Singular           { get; set; } = "";
    public string Plural             { get; set; } = "";
    public string ArticleSingular    { get; set; } = "a";
    public string ArticlePlural      { get; set; } = "as";
    public string IndefiniteSingular { get; set; } = "uma";
}

public class UiConfigDto
{
    public string? LogoUrl { get; set; }
    public string? CompanyDisplayName { get; set; }
    public string PrimaryColor { get; set; } = "";
    public string PrimaryDark { get; set; } = "";
    public string PrimaryLight { get; set; } = "";
    public string AccentColor { get; set; } = "";
    public string SidebarBg { get; set; } = "";
    public string SidebarText { get; set; } = "";
    public string TopbarBg { get; set; } = "";
    public string BodyBg { get; set; } = "";
    public string FontFamily { get; set; } = "";
    public string BorderRadius { get; set; } = "";
    public bool DarkMode { get; set; }
    public string? CustomCss { get; set; }
}

public class RawResponse
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
}

public class ErrorResponse
{
    public string? Error { get; set; }
}
