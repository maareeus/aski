namespace Aski.Tickets.Api.Domain;

/// <summary>Ticket di assistenza. Numero leggibile TXXXXX. Assegnazione singola (utente+unit).</summary>
public class Ticket
{
    public int Id { get; set; }

    /// <summary>Numero leggibile, es. "T00007". Generato alla creazione.</summary>
    public string? Number { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int? SoftwareId { get; set; }
    public SoftwareProduct? Software { get; set; }

    public int? SoftwareVersionId { get; set; }
    public SoftwareVersion? SoftwareVersion { get; set; }

    public required string CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;

    /// <summary>Operatore assegnato (unica assegnazione). Default null.</summary>
    public string? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }

    /// <summary>Unit con cui è gestito il ticket.</summary>
    public int? AssignedUnitId { get; set; }
    public Unit? AssignedUnit { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }

    public List<TicketComment> Comments { get; set; } = new();
    public List<TicketAttachment> Attachments { get; set; } = new();
}
