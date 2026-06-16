namespace Aski.ControlPlane.Entities;

/// <summary>
/// Registro di idempotenza dei webhook Stripe. Stripe può ritrasmettere lo stesso
/// evento più volte: salvando l'<see cref="EventId"/> (univoco) evitiamo doppio
/// provisioning o doppie transizioni di stato. La presenza della riga = già gestito.
/// </summary>
public class ProcessedStripeEvent
{
    /// <summary>Id dell'evento Stripe (evt_...). Chiave primaria.</summary>
    public required string EventId { get; set; }

    public required string EventType { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
}
