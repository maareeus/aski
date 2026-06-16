namespace Aski.ControlPlane.Services.Provisioning;

/// <summary>
/// Ponte tra lo stato dell'abbonamento (deciso dai webhook Stripe) e
/// l'infrastruttura concreta (container app + database nel pool Postgres).
///
/// La macchina a stati degli abbonamenti chiama questi metodi; l'implementazione
/// reale (Fase 3) usa IInfrastructureProvider via factory. Le operazioni di
/// provisioning sono volutamente asincrone: il webhook deve restituire 200 a
/// Stripe entro pochi secondi, quindi l'implementazione di produzione deve
/// accodare il lavoro pesante (background worker) anziché eseguirlo inline.
/// </summary>
public interface IProvisioningCoordinator
{
    /// <summary>Abbonamento attivo: crea (se serve) e avvia i container del progetto.</summary>
    Task ProvisionAndStartAsync(int projectId, CancellationToken ct = default);

    /// <summary>Insoluto/sospeso: ferma i container mantenendo i dati.</summary>
    Task SuspendAsync(int projectId, CancellationToken ct = default);

    /// <summary>Pagamento ripristinato: riavvia i container già provisionati.</summary>
    Task ResumeAsync(int projectId, CancellationToken ct = default);

    /// <summary>Abbonamento cancellato: ferma i container, conserva i dati per retention.</summary>
    Task StopAsync(int projectId, CancellationToken ct = default);
}
