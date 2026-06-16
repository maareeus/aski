using Aski.ControlPlane.Data;
using Aski.Shared;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Aski.ControlPlane.Services.Stripe;

/// <inheritdoc cref="IStripeContextProvider"/>
public sealed class StripeContextProvider : IStripeContextProvider
{
    private readonly ControlPlaneDbContext _db;

    public StripeContextProvider(ControlPlaneDbContext db) => _db = db;

    public async Task<StripeContext> GetAsync(CancellationToken ct = default)
    {
        // Riga singola Id = 1. I segreti vengono decifrati automaticamente dal
        // ValueConverter DataProtection al momento della materializzazione.
        var settings = await _db.StripeSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new InvalidOperationException(
                "Stripe non configurato: nessuna riga StripeSettings. Configurare le chiavi nel Super Admin.");

        if (settings.IsSimulated)
            throw new InvalidOperationException(
                "Stripe è in modalità Simulato: nessuna operazione reale verso Stripe è disponibile.");

        var secretKey = settings.ActiveSecretKey;
        var webhookSecret = settings.ActiveWebhookSecret;

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                $"Secret key Stripe mancante per la modalità {settings.Mode}.");
        if (string.IsNullOrWhiteSpace(webhookSecret))
            throw new InvalidOperationException(
                $"Webhook secret Stripe mancante per la modalità {settings.Mode}.");

        return new StripeContext
        {
            Client = new StripeClient(secretKey),
            WebhookSecret = webhookSecret,
            PublishableKey = settings.ActivePublishableKey,
            IsTestMode = settings.Mode == StripeMode.Test
        };
    }
}
