namespace Aski.Tickets.Api.Domain;

/// <summary>Voce di rubrica di un'azienda. Gestita dallo staff, visibile agli utenti dell'azienda.</summary>
public class Contact
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public required string Name { get; set; }
    public string? Title { get; set; }      // ruolo/qualifica
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
