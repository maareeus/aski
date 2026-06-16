using Aski.Shared;

namespace Aski.ControlPlane.Entities;

/// <summary>
/// Impostazioni globali Stripe del Super Admin. Riga singola (Id = 1).
///
/// <see cref="Mode"/> (deciso dal Super Admin) determina se il billing è simulato
/// o passa da Stripe Test/Live. Le chiavi Test e Live restano separate: cambiare
/// modalità non distrugge l'altro ambiente.
///
/// SecretKey e WebhookSecret sono cifrate a riposo tramite ASP.NET DataProtection
/// (vedi ValueConverter configurato in ControlPlaneDbContext.OnModelCreating).
/// PublishableKey non è segreta (esposta al client) ma la teniamo qui per coerenza.
/// </summary>
public class StripeSettings
{
    /// <summary>Chiave primaria. Vincolata a 1: esiste una sola riga di impostazioni.</summary>
    public int Id { get; set; } = 1;

    /// <summary>Modalità globale: Simulato / Test (sandbox) / Live. Default Simulato.</summary>
    public StripeMode Mode { get; set; } = StripeMode.Simulated;

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

    // --- Helper non mappati ---

    /// <summary>True se il billing è simulato (nessuna chiamata a Stripe).</summary>
    public bool IsSimulated => Mode == StripeMode.Simulated;

    /// <summary>Publishable key attiva secondo <see cref="Mode"/> (null se simulato).</summary>
    public string? ActivePublishableKey => Mode switch
    {
        StripeMode.Test => TestPublishableKey,
        StripeMode.Live => LivePublishableKey,
        _ => null
    };

    /// <summary>Secret key attiva secondo <see cref="Mode"/> (null se simulato).</summary>
    public string? ActiveSecretKey => Mode switch
    {
        StripeMode.Test => TestSecretKey,
        StripeMode.Live => LiveSecretKey,
        _ => null
    };

    /// <summary>Webhook signing secret attivo secondo <see cref="Mode"/> (null se simulato).</summary>
    public string? ActiveWebhookSecret => Mode switch
    {
        StripeMode.Test => TestWebhookSecret,
        StripeMode.Live => LiveWebhookSecret,
        _ => null
    };
}
