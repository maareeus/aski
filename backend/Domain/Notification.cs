namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Notifica per un utente legata a un ticket (nuovo ticket, commento, cambio stato,
/// assegnazione, sollecito). Mostrata nella campanella del portale/pannello.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>Destinatario.</summary>
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Tipo evento: created, comment, status, assign, remind, close.</summary>
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
