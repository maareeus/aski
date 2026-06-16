namespace Aski.Ticketing.Api.Domain;

/// <summary>
/// Ruoli applicativi dell'istanza di ticketing.
/// Admin   = gestione totale dell'istanza.
/// Dev     = lavora i ticket e ne gestisce gli stati, per le aziende/software assegnati.
/// Client  = apre ticket e vede solo la propria azienda; può chiudere i propri ticket.
/// </summary>
public enum TicketRole
{
    Admin = 0,
    Dev = 1,
    Client = 2
}

/// <summary>Ciclo di vita di un ticket.</summary>
public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Waiting = 2,
    Resolved = 3,
    Closed = 4
}

/// <summary>Priorità del ticket.</summary>
public enum TicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
