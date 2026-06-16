using System.Text;
using Aski.Ticketing.Api.Auth;
using Aski.Ticketing.Api.Data;
using Aski.Ticketing.Api.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
    jwt.Key = "DEV-ONLY-change-me-please-32+chars-minimum-key!!"; // override in produzione
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
builder.Services.AddAuthorization();

var app = builder.Build();

// Migrazione + seed dell'admin iniziale all'avvio.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    if (app.Configuration.GetValue("Seed:ApplyMigrations", true))
        await db.Database.MigrateAsync();
    await SeedAdminAsync(db, app.Configuration);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Crea l'utente Admin iniziale se la tabella utenti è vuota.
static async Task SeedAdminAsync(TicketingDbContext db, IConfiguration config)
{
    if (await db.Users.AnyAsync()) return;

    var email = config["Seed:AdminEmail"] ?? "admin@aski.local";
    var password = config["Seed:AdminPassword"] ?? "ChangeMe123!";

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
