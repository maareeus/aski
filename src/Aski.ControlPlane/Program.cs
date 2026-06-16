using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Infrastructure;
using Aski.ControlPlane.Services.Provisioning;
using Aski.ControlPlane.Services.Stripe;
using System.Threading.RateLimiting;
using Aski.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
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

// Hardening: in produzione vietiamo le credenziali DB di default.
if (builder.Environment.IsProduction() && connectionString.Contains("Password=postgres"))
    throw new InvalidOperationException(
        "Credenziali DB di default non ammesse in produzione: imposta ConnectionStrings__ControlPlane.");

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

// --- Autenticazione (cookie) + autorizzazione per ruolo ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", p => p.RequireRole(nameof(PortalUserRole.SuperAdmin)));
    options.AddPolicy("Tenant", p => p.RequireRole(nameof(PortalUserRole.TenantOwner)));

    // Sicurezza per default: ogni endpoint richiede autenticazione, a meno di
    // [AllowAnonymous] esplicito (login/registrazione e webhook Stripe). Così
    // nessuna rotta resta accidentalmente pubblica.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- Rate limiting (anti brute-force / abuso) ---
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Login/registrazione: stretto, per IP.
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    // Webhook Stripe: più alto (Stripe può raffica di eventi), per IP.
    o.AddPolicy("webhook", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

// Migrazione + seed del Super Admin iniziale.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
    await db.Database.MigrateAsync();
    await SeedSuperAdminAsync(db, app.Configuration, app.Environment);
}

// --- Pipeline HTTP ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts(); // HSTS solo fuori da sviluppo (in dev i certificati locali variano).
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// API attribute-routed (controller con [Route("api/...")]).
app.MapControllers();
// UI MVC convenzionale. Home smista per ruolo dopo il login.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Crea il Super Admin iniziale se non esiste alcun SuperAdmin.
static async Task SeedSuperAdminAsync(ControlPlaneDbContext db, IConfiguration config, IWebHostEnvironment env)
{
    if (await db.PortalUsers.AnyAsync(u => u.Role == PortalUserRole.SuperAdmin))
        return;

    var email = config["Seed:SuperAdminEmail"] ?? "admin@aski.local";
    var password = config["Seed:SuperAdminPassword"] ?? "ChangeMe123!";

    // Hardening: in produzione la password seed deve arrivare da configurazione/env, non dal default.
    if (env.IsProduction() && (string.IsNullOrWhiteSpace(password) || password == "ChangeMe123!"))
        throw new InvalidOperationException(
            "Imposta Seed__SuperAdminPassword (via env) in produzione: la password di default non è ammessa.");

    db.PortalUsers.Add(new PortalUser
    {
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        DisplayName = "Super Admin",
        Role = PortalUserRole.SuperAdmin,
        CreatedAtUtc = DateTime.UtcNow
    });
    await db.SaveChangesAsync();
}
