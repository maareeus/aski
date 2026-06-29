using System.Net.Http.Json;
using Aski.Tickets.Web.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Aski.Tickets.Web.Auth;

/// <summary>Login/logout: chiama l'API, gestisce i token e notifica lo stato auth.</summary>
public sealed class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly ApiAuthenticationStateProvider _authState;

    public AuthService(HttpClient http, ILocalStorageService storage, AuthenticationStateProvider authState)
    {
        _http = http;
        _storage = storage;
        _authState = (ApiAuthenticationStateProvider)authState;
    }

    /// <summary>Esegue il login. Ritorna null se ok, altrimenti il messaggio d'errore.</summary>
    public async Task<string?> LoginAsync(string email, string password)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        }
        catch (Exception ex)
        {
            return $"Connessione API non riuscita: {ex.Message}";
        }

        if (!resp.IsSuccessStatusCode)
            return resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Credenziali non valide."
                : $"Errore login ({(int)resp.StatusCode}).";

        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is null) return "Risposta non valida dal server.";

        await _storage.SetItemAsStringAsync(TokenKeys.Access, auth.AccessToken);
        await _storage.SetItemAsStringAsync(TokenKeys.Refresh, auth.RefreshToken);
        _authState.NotifyChanged();
        return null;
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync(TokenKeys.Access);
        await _storage.RemoveItemAsync(TokenKeys.Refresh);
        _authState.NotifyChanged();
    }
}
