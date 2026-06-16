using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aski.Ticketing.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Aski.Ticketing.Api.Auth;

/// <summary>Opzioni JWT (bind da sezione "Jwt" della configurazione).</summary>
public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "aski-ticketing";
    public string Audience { get; set; } = "aski-ticketing";
    public int ExpiryMinutes { get; set; } = 480;
}

/// <summary>Emette i JWT con i claim di ruolo e azienda per l'istanza di ticketing.</summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _opt;

    public JwtTokenService(JwtOptions opt) => _opt = opt;

    public string CreateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.CompanyId is not null)
            claims.Add(new Claim(AskiClaims.CompanyId, user.CompanyId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
