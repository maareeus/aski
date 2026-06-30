using System.Net.Http.Json;
using Aski.Tickets.Portal.Models;
using Microsoft.JSInterop;

namespace Aski.Tickets.Portal.Services;

/// <summary>Carica e applica nome, logo e favicon configurati lato admin.</summary>
public sealed class BrandService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private bool _loaded;

    public BrandService(HttpClient http, IJSRuntime js) { _http = http; _js = js; }

    public string Name { get; private set; } = "Aski";
    public bool HasLogo { get; private set; }
    public bool HasFavicon { get; private set; }
    public long Version { get; private set; }

    public event Action? Changed;

    private string ApiBase => _http.BaseAddress!.ToString().TrimEnd('/');
    public string LogoUrl => $"{ApiBase}/api/settings/logo?v={Version}";
    public string FaviconUrl => $"{ApiBase}/api/settings/favicon?v={Version}";

    public string Title(string suffix) =>
        string.IsNullOrWhiteSpace(suffix) ? Name : $"{Name} - {suffix}";

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            var s = await _http.GetFromJsonAsync<BrandSettings>("api/settings");
            if (s is not null)
            {
                Name = string.IsNullOrWhiteSpace(s.BrandName) ? "Aski" : s.BrandName;
                HasLogo = s.HasLogo; HasFavicon = s.HasFavicon; Version = s.Version;
            }
        }
        catch { /* default */ }
        _loaded = true;
        if (HasFavicon) { try { await _js.InvokeVoidAsync("setFavicon", FaviconUrl); } catch { } }
        Changed?.Invoke();
    }
}
