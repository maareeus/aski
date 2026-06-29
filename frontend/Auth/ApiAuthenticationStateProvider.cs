using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Aski.Tickets.Web.Auth;

/// <summary>
/// Stato di autenticazione derivato dall'access token JWT salvato in localStorage.
/// Decodifica i claim del token (ruoli inclusi) per popolare il ClaimsPrincipal.
/// </summary>
public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public ApiAuthenticationStateProvider(ILocalStorageService storage) => _storage = storage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsStringAsync(TokenKeys.Access);
        if (string.IsNullOrWhiteSpace(token)) return Anonymous;

        var claims = ParseClaims(token);
        // expired?
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (long.TryParse(exp, out var sec) &&
            DateTimeOffset.FromUnixTimeSeconds(sec) < DateTimeOffset.UtcNow)
            return Anonymous;

        // Il JWT non ha un claim "name": uso l'email come display name.
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Email, ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return Enumerable.Empty<Claim>();
        var payload = Decode(parts[1]);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload)
                   ?? new Dictionary<string, JsonElement>();

        var claims = new List<Claim>();
        foreach (var kv in dict)
        {
            if (kv.Value.ValueKind == JsonValueKind.Array)
                claims.AddRange(kv.Value.EnumerateArray().Select(v => new Claim(kv.Key, v.ToString())));
            else
                claims.Add(new Claim(kv.Key, kv.Value.ToString()));
        }
        return claims;
    }

    private static string Decode(string b64Url)
    {
        var s = b64Url.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
