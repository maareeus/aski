using System.Text;
using System.Threading.RateLimiting;
using Aski.Ticketing.Api.Auth;
using Aski.Ticketing.Api.Data;
using Aski.Ticketing.Api.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string DevJwtKey = "DEV-ONLY-change-me-please-32+chars-minimum-key!!";

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// DbContext isolato del tenant. La connection string viene iniettata dal Control
// Plane in fase di provisioning (env ConnectionStrings__Tenant).
var connectionString = builder.Configuration.GetConnectionString("Tenant")
                       ?? "Host=localhost;Port=5432;Database=aski_ticketing_dev;Username=postgres;Password=postgres";
builder.Services.AddDbContext<TicketingDbContext>(opt => opt.UseNpgsql(connectionString));

// JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key))
    jwt.Key = DevJwtKey; // override in produzione

// Hardening: in produzione la chiave JWT deve essere robusta e non quella di sviluppo.
if (builder.Environment.IsProduction() && (jwt.Key == DevJwtKey || jwt.Key.Length < 32))
    throw new InvalidOperationException(
        "Imposta Jwt__Key (>= 32 caratteri) in produzione: la chiave di sviluppo non è ammessa.");

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Sicurezza per default: ogni endpoint richiede un JWT valido, salvo
    // [AllowAnonymous] esplicito (es. il login). Nessuna rotta resta pubblica per errore.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Rate limiting: anti brute-force sul login.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

// Migrazione + seed dell'admin iniziale all'avvio.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    if (app.Configuration.GetValue("Seed:ApplyMigrations", true))
        await db.Database.MigrateAsync();
    await SeedAdminAsync(db, app.Configuration, app.Environment);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHsts();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Crea l'utente Admin iniziale se la tabella utenti è vuota.
static async Task SeedAdminAsync(TicketingDbContext db, IConfiguration config, IWebHostEnvironment env)
{
    if (await db.Users.AnyAsync()) return;

    var email = config["Seed:AdminEmail"] ?? "admin@aski.local";
    var password = config["Seed:AdminPassword"] ?? "ChangeMe123!";

    if (env.IsProduction() && (string.IsNullOrWhiteSpace(password) || password == "ChangeMe123!"))
        throw new InvalidOperationException(
            "Imposta Seed__AdminPassword (via env) in produzione: la password di default non è ammessa.");

    db.Users.Add(new AppUser
    {
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = TicketRole.Admin,
        FullName = "Administrator",
        CreatedAtUtc = DateTime.UtcNow
    });
    await db.SaveChangesAsync();
}
