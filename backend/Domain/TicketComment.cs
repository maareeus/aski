namespace Aski.Tickets.Api.Domain;

/// <summary>Commento su un ticket. IsInternal = nota visibile solo a staff (Admin/Agent).</summary>
public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public required string AuthorUserId { get; set; }
    public AppUser AuthorUser { get; set; } = null!;

    public required string Body { get; set; }

    /// <summary>Se true il commento è interno (non visibile ai Client).</summary>
    public bool IsInternal { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
