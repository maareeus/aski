using Askii.Authorization;
using Askii.Common.Exceptions;
using Askii.Database;
using Askii.Features.Auth;
using Askii.Features.Users;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAZIONE VERSIONING E API EXPLORER
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
})
.AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
})
.AddOpenApi(versioned =>
{
    versioned.Document.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer", 
            BearerFormat = "JWT",
            Description = "Inserisci qui il tuo token JWT."
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", document),
                new List<string>()
            }
        });

        return Task.CompletedTask;
    });
});

// 3. DATABASE
var dbconnection = "Data Source=askii.db;Cache=shared;Foreign Keys=True;";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    o.UseSqlite(dbconnection); 
    o.AddInterceptors(new SqlitePragmaInterceptor());
});

// 4. REGISTRAZIONE SERVIZI (DI)
// Risolve l'errore di avvio "UNKNOWN parameter TokenService"
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<Askii.Database.Entities.Options>();

// 5. AUTENTICAZIONE E AUTORIZZAZIONE
JwtAuthorization.Init(builder);

// 6. GESTIONE ERRORI
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// --- INIZIO PIPELINE DEI MIDDLEWARE ---

app.UseExceptionHandler();

// L'ordine è FONDAMENTALE: l'autenticazione deve precedere l'autorizzazione
app.UseAuthentication();
app.UseAuthorization();

// 7. INIZIALIZZAZIONE DB
// Inizializzazione WAL e ottimizzazioni globali del file DB
await DbIniializer.Init(app);
var appOptions = app.Services.GetRequiredService<Askii.Database.Entities.Options>();
await appOptions.Seed();

// 8. MAPPATURA ENDPOINT E GRUPPI
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var versionedGroup = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

// Mappa i tuoi endpoint (Assicurati che MapAuth contenga i tuoi vari MapPost)
versionedGroup.MapAuth();
versionedGroup.MapUsers();
versionedGroup.MapSettings();

// 9. DOCUMENTAZIONE API E SCALAR UI
// Espone /openapi/{documentName}.json; WithDocumentPerVersion applica le
// convenzioni di API versioning all'endpoint del documento.
app.MapOpenApi().WithDocumentPerVersion();

app.MapScalarApiReference(options =>
{
    options.AddPreferredSecuritySchemes("Bearer");
    options.AddHttpAuthentication("Bearer", x =>
    {
        x.Token = "INCOLLA_QUI_IL_TUO_TOKEN_DI_TEST"; // Opzionale: per precompilare la UI
    });
});

app.Run();