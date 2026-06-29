namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Refresh token persistito per rinnovare l'access token JWT senza re-login.
/// Rotazione: a ogni refresh il token usato viene revocato e ne viene emesso uno nuovo.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>Valore opaco del token (random sicuro).</summary>
    public required string Token { get; set; }

    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
