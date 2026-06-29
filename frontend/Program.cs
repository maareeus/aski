using Aski.Tickets.Web;
using Aski.Tickets.Web.Auth;
using Aski.Tickets.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base URL dell'API (configurabile in wwwroot/appsettings.json).
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5095";

// Auth + storage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

// HttpClient verso l'API con bearer handler
builder.Services.AddScoped<BearerHandler>();
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<BearerHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
