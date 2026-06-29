using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aski.Tickets.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aski.Tickets.Api.Auth;

/// <summary>Custom claim types dell'applicazione.</summary>
public static class AppClaims
{
    public const string CompanyId = "companyId";
    public const string FullName = "fullName";
}

/// <summary>Genera access token JWT e refresh token opachi.</summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _opt;

    public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public int AccessTokenLifetimeSeconds => _opt.AccessTokenMinutes * 60;

    /// <summary>Crea l'access token JWT con id, email, ruoli e claim applicativi.</summary>
    public string CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
        };
        if (!string.IsNullOrEmpty(user.FullName)) claims.Add(new Claim(AppClaims.FullName, user.FullName));
        if (user.CompanyId is not null) claims.Add(new Claim(AppClaims.CompanyId, user.CompanyId.Value.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Crea un refresh token opaco crittograficamente sicuro.</summary>
    public RefreshToken CreateRefreshToken(string userId)
    {
        var raw = RandomNumberGenerator.GetBytes(48);
        return new RefreshToken
        {
            Token = Convert.ToBase64String(raw).Replace("+", "-").Replace("/", "_").TrimEnd('='),
            UserId = userId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_opt.RefreshTokenDays)
        };
    }
}
