using Askii.Common;
using Askii.Database.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Askii.Features.Auth;

public class TokenService(IConfiguration configuration)
{
    /// <summary>
    /// Suffisso applicato all'audience del token di sfida 2FA.
    ///
    /// È la difesa che impedisce di usare la sfida come token d'accesso: la
    /// pipeline JwtBearer valida solo l'audience configurata, quindi un token
    /// con audience diversa viene rifiutato su ogni endpoint protetto. Bastasse
    /// un claim distintivo, un token di sfida passerebbe comunque le policy che
    /// richiedono solo l'autenticazione.
    /// </summary>
    private const string SuffissoAudienceSfida = ":tfa";

    private const int DurataTokenOre = 8;
    private const int DurataSfidaMinuti = 5;

    private SymmetricSecurityKey Chiave()
    {
        var secretKey = configuration["JWT:Key"] ?? throw new InvalidOperationException("Missing chiave jwt");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    private string? Issuer => configuration["Jwt:Issuer"];
    private string? Audience => configuration["Jwt:Audience"];

    public string GenerateToken(User user)
    {
        var creds = new SigningCredentials(Chiave(), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            // Impronta dello stato di autorizzazione al momento dell'emissione:
            // se cambia lato utente, questo token viene rifiutato.
            new(AskiiClaims.Stamp, user.SecurityStamp)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(DurataTokenOre),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Token che attesta il superamento della password e autorizza soltanto il
    /// completamento del secondo fattore. Non porta il ruolo e ha un'audience
    /// dedicata, quindi non apre nessun endpoint.
    /// </summary>
    public string GenerateTfaChallenge(User user)
    {
        var creds = new SigningCredentials(Chiave(), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience + SuffissoAudienceSfida,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(DurataSfidaMinuti),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida una sfida e restituisce l'utente a cui si riferisce, oppure null
    /// se il token è assente, scaduto, alterato o non è una sfida.
    /// </summary>
    public Guid? ReadTfaChallenge(string? challengeToken)
    {
        if (string.IsNullOrWhiteSpace(challengeToken)) return null;

        var parametri = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience + SuffissoAudienceSfida,
            IssuerSigningKey = Chiave(),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(challengeToken, parametri, out _);

            // Il gestore mappa "sub" su NameIdentifier quando il mapping inbound
            // è attivo, quindi si accettano entrambe le chiavi.
            var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(id, out var guid) ? guid : null;
        }
        catch (Exception)
        {
            // Firma non valida, scaduto, audience sbagliata: in tutti i casi la
            // sfida non è utilizzabile e il chiamante riceve un 401 generico.
            return null;
        }
    }
}
