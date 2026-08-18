using System.Security.Claims;
using Askii.Common.Authorization;
using Askii.Features.Auth;
using Microsoft.Extensions.Configuration;

namespace Askii.Tests.Infrastructure;

public static class TestFactory
{
    public const string JwtKey = "chiave-di-test-lunga-almeno-32-caratteri";
    public const string JwtIssuer = "AskiiTestIssuer";
    public const string JwtAudience = "AskiiTestAudience";

    public static IConfiguration Configuration(
        string? key = JwtKey,
        string? issuer = JwtIssuer,
        string? audience = JwtAudience)
    {
        var values = new Dictionary<string, string?>();
        if (key is not null) values["Jwt:Key"] = key;
        if (issuer is not null) values["Jwt:Issuer"] = issuer;
        if (audience is not null) values["Jwt:Audience"] = audience;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    public static TokenService TokenService(
        string? key = JwtKey,
        string? issuer = JwtIssuer,
        string? audience = JwtAudience)
        => new(Configuration(key, issuer, audience));

    /// <summary>
    /// Registro con la mappa predefinita: i test verificano l'assegnazione reale
    /// dei permessi, non una inventata per l'occasione.
    /// </summary>
    public static IPermissionRegistry Permessi() => new PermissionRegistry();

    /// <summary>
    /// Utente autenticato come lo vede un endpoint: i claim usano i tipi che
    /// le extension in Common/Extensions/ClaimsPrincipal.cs vanno a leggere.
    /// </summary>
    public static ClaimsPrincipal Principal(
        Guid? userId = null,
        string? role = Askii.Common.Roles.Client,
        string? email = "mario@example.com")
    {
        var claims = new List<Claim>();
        if (userId is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
        if (email is not null) claims.Add(new Claim(ClaimTypes.Email, email));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
