namespace Aski.Ticketing.Api.Domain;

/// <summary>Azienda cliente gestita dentro l'istanza di ticketing.</summary>
public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<AppUser> Users { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
}

/// <summary>Software/prodotto per cui si aprono ticket.</summary>
public class SoftwareProduct
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<Ticket> Tickets { get; set; } = new();
}

/// <summary>
/// Utente dell'istanza. Il <see cref="Role"/> determina i permessi.
/// I Client appartengono a una <see cref="Company"/>; Admin/Dev non sono vincolati.
/// </summary>
public class AppUser
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? FullName { get; set; }
    public TicketRole Role { get; set; }

    /// <summary>Azienda di appartenenza (obbligatoria per i Client).</summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Assegnazioni del Dev a aziende/software.</summary>
    public List<DevAssignment> Assignments { get; set; } = new();
}

/// <summary>
/// Assegnazione di un Dev a un'azienda e/o a un software. Definisce l'ambito di
/// ticket che il Dev può vedere e lavorare. Un CompanyId/SoftwareId nullo = qualsiasi.
/// </summary>
public class DevAssignment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? SoftwareId { get; set; }
    public SoftwareProduct? Software { get; set; }
}

/// <summary>Ticket di supporto.</summary>
public class Ticket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int? SoftwareId { get; set; }
    public SoftwareProduct? Software { get; set; }

    /// <summary>Utente (Client/Admin) che ha aperto il ticket.</summary>
    public int CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;

    /// <summary>Dev assegnato alla lavorazione (opzionale).</summary>
    public int? AssignedDevUserId { get; set; }
    public AppUser? AssignedDevUser { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public List<TicketComment> Comments { get; set; } = new();
}

/// <summary>Commento su un ticket. IsInternal = nota visibile solo a Dev/Admin.</summary>
public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int AuthorUserId { get; set; }
    public AppUser AuthorUser { get; set; } = null!;

    public required string Body { get; set; }

    /// <summary>Se true il commento è interno (non visibile ai Client).</summary>
    public bool IsInternal { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
