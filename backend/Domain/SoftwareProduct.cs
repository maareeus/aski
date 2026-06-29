namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Software assistito. Contiene lo storico delle <see cref="Versions"/>:
/// il software si crea una volta, poi al suo interno si aggiungono le versioni.
/// </summary>
public class SoftwareProduct
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Storico versioni del software.</summary>
    public List<SoftwareVersion> Versions { get; set; } = new();

    public List<Ticket> Tickets { get; set; } = new();

    /// <summary>Aziende che usano il software.</summary>
    public List<Company> Companies { get; set; } = new();
    /// <summary>Utenti (Agent) competenti su questo software.</summary>
    public List<AppUser> Users { get; set; } = new();
}
