using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SalesSystem.Web.Services;

namespace SalesSystem.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ApiService _api;

    public CustomAuthStateProvider(ApiService api)
    {
        _api = api;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsIdentity identity;

        if (_api.IsAuthenticated)
        {
            identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, _api.UserName ?? "User"),
                new Claim(ClaimTypes.Role, _api.UserRole ?? "Viewer")
            }, "jwt");
        }
        else
        {
            identity = new ClaimsIdentity();
        }

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
