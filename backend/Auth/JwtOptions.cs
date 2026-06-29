namespace Aski.Tickets.Api.Auth;

/// <summary>Opzioni JWT (sezione "Jwt" della configurazione).</summary>
public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "aski-tickets";
    public string Audience { get; set; } = "aski-tickets";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
