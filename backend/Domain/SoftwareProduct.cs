namespace Aski.Tickets.Api.Domain;

/// <summary>Software/prodotto su cui si fornisce assistenza (oggetto dei ticket).</summary>
public class SoftwareProduct
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Ticket> Tickets { get; set; } = new();
}
