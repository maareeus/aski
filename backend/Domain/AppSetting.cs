namespace Aski.Tickets.Api.Domain;

/// <summary>
/// Impostazioni globali del prodotto (riga singola, Id = 1): nome mostrato,
/// logo e favicon configurabili dall'admin.
/// </summary>
public class AppSetting
{
    public int Id { get; set; } = 1;
    public string BrandName { get; set; } = "Aski";

    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }

    public byte[]? FaviconData { get; set; }
    public string? FaviconContentType { get; set; }

    /// <summary>Aggiornato a ogni modifica: usato per il cache-busting di logo/favicon.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
