using Aski.ControlPlane.Data;
using Aski.ControlPlane.Services.Infrastructure;
using Aski.ControlPlane.Services.Provisioning;
using Aski.ControlPlane.Services.Stripe;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Servizi ---
// AddControllersWithViews abilita sia le API attribute-routed sia l'UI MVC (Razor).
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();

// DataProtection con chiavi persistite su disco: i segreti Stripe cifrati a riposo
// devono restare decifrabili dopo un riavvio. In produzione puntare a uno store
// condiviso (volume/Redis/KMS) se si scala su più istanze.
var keyRingPath = builder.Configuration["DataProtection:KeyRingPath"]
                  ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("Aski.ControlPlane");

// DbContext del Control Plane (PostgreSQL).
var connectionString = builder.Configuration.GetConnectionString("ControlPlane")
                       ?? "Host=localhost;Port=5432;Database=aski_controlplane;Username=postgres;Password=postgres";
builder.Services.AddDbContext<ControlPlaneDbContext>(opt => opt.UseNpgsql(connectionString));

// --- Billing / Stripe ---
builder.Services.AddScoped<IStripeContextProvider, StripeContextProvider>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<StripeWebhookHandler>();

// --- Infrastruttura / Provisioning ---
builder.Services.AddSingleton<IInfrastructureProviderFactory, InfrastructureProviderFactory>();

// Scelta del coordinatore via config (Provisioning:Mode):
//   "Docker"  -> DockerProvisioningCoordinator (richiede demone Docker raggiungibile)
//   "Logging" -> LoggingProvisioningCoordinator (placeholder, per testare il billing senza Docker)
var provisioningMode = builder.Configuration["Provisioning:Mode"] ?? "Logging";
if (string.Equals(provisioningMode, "Docker", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IProvisioningCoordinator, DockerProvisioningCoordinator>();
else
    builder.Services.AddScoped<IProvisioningCoordinator, LoggingProvisioningCoordinator>();

var app = builder.Build();

// --- Pipeline HTTP ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// API attribute-routed (controller con [Route("api/...")]).
app.MapControllers();
// UI MVC convenzionale.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
