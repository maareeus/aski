namespace Aski.ControlPlane.Entities;

/// <summary>
/// Impostazioni globali Stripe del Super Admin. Riga singola (Id = 1).
///
/// Mantiene due set di chiavi completi (Test e Live) + un toggle <see cref="IsTestMode"/>:
/// passare da sandbox a produzione NON distrugge le chiavi dell'altro ambiente.
///
/// SecretKey e WebhookSecret sono cifrate a riposo tramite ASP.NET DataProtection
/// (vedi ValueConverter configurato in ControlPlaneDbContext.OnModelCreating).
/// PublishableKey non è segreta (esposta al client) ma la teniamo qui per coerenza.
/// </summary>
public class StripeSettings
{
    /// <summary>Chiave primaria. Vincolata a 1: esiste una sola riga di impostazioni.</summary>
    public int Id { get; set; } = 1;

    /// <summary>Se true il sistema usa le chiavi Test (sandbox), altrimenti le Live.</summary>
    public bool IsTestMode { get; set; } = true;

    // --- Ambiente Test (sandbox) ---
    public string? TestPublishableKey { get; set; }
    /// <summary>Cifrata a riposo.</summary>
    public string? TestSecretKey { get; set; }
    /// <summary>Cifrata a riposo.</summary>
    public string? TestWebhookSecret { get; set; }

    // --- Ambiente Live (produzione) ---
    public string? LivePublishableKey { get; set; }
    /// <summary>Cifrata a riposo.</summary>
    public string? LiveSecretKey { get; set; }
    /// <summary>Cifrata a riposo.</summary>
    public string? LiveWebhookSecret { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    // --- Helper non mappati: risolvono le chiavi attive in base alla modalità ---

    /// <summary>Publishable key attiva secondo <see cref="IsTestMode"/>.</summary>
    public string? ActivePublishableKey => IsTestMode ? TestPublishableKey : LivePublishableKey;

    /// <summary>Secret key attiva secondo <see cref="IsTestMode"/>.</summary>
    public string? ActiveSecretKey => IsTestMode ? TestSecretKey : LiveSecretKey;

    /// <summary>Webhook signing secret attivo secondo <see cref="IsTestMode"/>.</summary>
    public string? ActiveWebhookSecret => IsTestMode ? TestWebhookSecret : LiveWebhookSecret;
}
