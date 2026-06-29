using Microsoft.AspNetCore.Identity;

namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Utente applicativo, estende IdentityUser (chiave string/GUID).
/// I Client sono legati a una <see cref="CompanyId"/> (azienda cliente);
/// Admin e Agent non hanno azienda.
/// </summary>
public class AppUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>Azienda di appartenenza (obbligatoria per i Client).</summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
