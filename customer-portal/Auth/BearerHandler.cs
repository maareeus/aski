using Blazored.LocalStorage;

namespace Aski.Tickets.Portal.Auth;

/// <summary>Allega l'access token JWT (da localStorage) alle richieste verso l'API.</summary>
public sealed class BearerHandler : DelegatingHandler
{
    private readonly ILocalStorageService _storage;

    public BearerHandler(ILocalStorageService storage) => _storage = storage;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _storage.GetItemAsStringAsync(TokenKeys.Access, ct);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}

public static class TokenKeys
{
    public const string Access = "aski_portal_access";
    public const string Refresh = "aski_portal_refresh";
}
