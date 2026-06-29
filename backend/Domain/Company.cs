namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Azienda cliente assistita. Gestione completa in una fase successiva; qui serve
/// come riferimento per i Client (<see cref="AppUser.CompanyId"/>).
/// </summary>
public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<AppUser> Users { get; set; } = new();
}
