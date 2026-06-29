namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Assegnazione di visibilità su un ticket: il PM assegna un utente tramite una Unit.
/// Un utente in più Unit può comparire più volte (una riga per unit).
/// </summary>
public class TicketAssignment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
