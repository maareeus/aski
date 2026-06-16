using Stripe;

namespace Aski.ControlPlane.Services.Stripe;

/// <summary>
/// Configurazione Stripe risolta a runtime per la richiesta corrente:
/// client già autenticato con la secret key attiva (Test o Live) + i segreti
/// correlati. Costruito da <see cref="IStripeContextProvider"/> leggendo il DB.
/// </summary>
public sealed class StripeContext
{
    public required IStripeClient Client { get; init; }
    public required string WebhookSecret { get; init; }
    public string? PublishableKey { get; init; }
    public bool IsTestMode { get; init; }
}

/// <summary>
/// Risolve la configurazione Stripe attiva leggendo <c>StripeSettings</c> dal DB,
/// decifrando i segreti e selezionando le chiavi Test o Live secondo IsTestMode.
/// </summary>
public interface IStripeContextProvider
{
    /// <summary>Costruisce il contesto Stripe attivo. Lancia se le chiavi non sono configurate.</summary>
    Task<StripeContext> GetAsync(CancellationToken ct = default);
}
