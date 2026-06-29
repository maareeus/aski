using System.Text;
using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Database (SQLite + EF Core) ---
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=aski-tickets.db";
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connectionString));

// --- Identity (utenti + ruoli) ---
builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false; // lunghezza + maiusc/minusc/cifra bastano
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --- JWT ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key))
    jwt.Key = "DEV-ONLY-change-me-please-32+chars-minimum-key!!";
if (builder.Environment.IsProduction() && jwt.Key.Length < 32)
    throw new InvalidOperationException("Imposta Jwt__Key (>= 32 caratteri) in produzione.");
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// --- CORS per la SPA Blazor ---
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
{
    if (corsOrigins.Length > 0)
        p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
    else if (builder.Environment.IsDevelopment())
        p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod(); // dev: qualunque origine locale
}));

builder.Services.AddControllers();

// --- OpenAPI / Swagger con auth Bearer ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Aski Tickets API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci il token JWT (senza prefisso 'Bearer ')."
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// --- Migrazione automatica + seed (ruoli + admin) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
