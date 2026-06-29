namespace Aski.Tickets.Api.Domain;

/// <summary>Azienda cliente assistita. Ha anagrafica, più utenti e più software associati.</summary>
public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Anagrafica
    public string? VatNumber { get; set; }   // P.IVA / C.F.
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<AppUser> Users { get; set; } = new();

    /// <summary>Software in uso dall'azienda (molti-a-molti).</summary>
    public List<SoftwareProduct> Softwares { get; set; } = new();
}
