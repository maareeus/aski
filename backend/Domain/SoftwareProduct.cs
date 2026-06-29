namespace Aski.Tickets.Api.Domain;

/// <summary>Software assistito. La versione è obbligatoria (ogni software è versionato).</summary>
public class SoftwareProduct
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Ticket> Tickets { get; set; } = new();

    /// <summary>Aziende che usano il software.</summary>
    public List<Company> Companies { get; set; } = new();
    /// <summary>Utenti (Agent) competenti su questo software.</summary>
    public List<AppUser> Users { get; set; } = new();
}
