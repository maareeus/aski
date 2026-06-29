namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Versione di un software (voce di storico). Non si modifica la "versione corrente":
/// si aggiungono nuove versioni nel tempo.
/// </summary>
public class SoftwareVersion
{
    public int Id { get; set; }

    public int SoftwareId { get; set; }
    public SoftwareProduct Software { get; set; } = null!;

    public required string Version { get; set; }
    public string? Notes { get; set; }
    /// <summary>Note di rilascio in formato Markdown (visibili ai clienti nel portale).</summary>
    public string? ReleaseNotes { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
