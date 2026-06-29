namespace Aski.Tickets.Api.Domain;

/// <summary>Ticket di assistenza aperto da un cliente su un software.</summary>
public class Ticket
{
    public int Id { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    /// <summary>Azienda cliente proprietaria del ticket.</summary>
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Software a cui si riferisce (opzionale).</summary>
    public int? SoftwareId { get; set; }
    public SoftwareProduct? Software { get; set; }

    /// <summary>Versione specifica del software a cui si riferisce (opzionale).</summary>
    public int? SoftwareVersionId { get; set; }
    public SoftwareVersion? SoftwareVersion { get; set; }

    /// <summary>Utente che ha aperto il ticket (Client o Admin).</summary>
    public required string CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;

    /// <summary>Agent assegnato alla lavorazione (opzionale).</summary>
    public string? AssignedAgentUserId { get; set; }
    public AppUser? AssignedAgentUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }

    public List<TicketComment> Comments { get; set; } = new();
}
