namespace Aski.Tickets.Api.Domain;

/// <summary>Ciclo di vita di un ticket di assistenza.</summary>
public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Waiting = 2,    // in attesa di info dal cliente / terzi
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
