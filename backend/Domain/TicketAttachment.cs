namespace Aski.Tickets.Api.Domain;

/// <summary>Allegato di un ticket. Il file è salvato su disco; qui i metadati.</summary>
public class TicketAttachment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    /// <summary>Percorso relativo del file salvato (sotto la cartella uploads).</summary>
    public required string StoredPath { get; set; }

    public required string UploadedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
