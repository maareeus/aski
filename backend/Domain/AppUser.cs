using Microsoft.AspNetCore.Identity;

namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Utente applicativo (Identity). Anagrafica + associazioni.
/// I Client appartengono a una <see cref="CompanyId"/>; un'azienda può avere più Client.
/// Agli utenti (in particolare gli Agent) possono essere associati più software:
/// l'assistenza dell'Agent è limitata ai software che ha assegnati.
/// </summary>
public class AppUser : IdentityUser
{
    // Anagrafica
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? JobTitle { get; set; }   // ruolo in azienda
    public string? Phone { get; set; }

    /// <summary>Nome visualizzato (composto da nome/cognome se presenti).</summary>
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Azienda di appartenenza (obbligatoria per i Client).</summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<RefreshToken> RefreshTokens { get; set; } = new();

    /// <summary>Software assegnati all'utente (ambito di assistenza per gli Agent).</summary>
    public List<SoftwareProduct> Softwares { get; set; } = new();
}
