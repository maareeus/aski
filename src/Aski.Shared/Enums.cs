namespace Aski.Shared;

/// <summary>
/// Periodo di fatturazione di un piano d'abbonamento.
/// Mappa sul campo "interval" dei Price di Stripe (month/year).
/// </summary>
public enum BillingPeriod
{
    Monthly = 0,
    Annual = 1
}

/// <summary>
/// Stato del ciclo di vita di un abbonamento.
/// Pilota direttamente lo stato dei container del progetto:
///   Active   -> container avviati
///   PastDue  -> insoluto, container sospesi (dati conservati)
///   Suspended-> sospeso manualmente o per insoluto prolungato
///   Canceled -> abbonamento chiuso, container fermati (dati conservati)
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Checkout iniziato ma pagamento non ancora confermato.</summary>
    Pending = 0,
    /// <summary>Pagamento ok, infrastruttura attiva.</summary>
    Active = 1,
    /// <summary>Fattura insoluta: container sospesi, dati conservati.</summary>
    PastDue = 2,
    /// <summary>Sospeso: container fermati, dati conservati.</summary>
    Suspended = 3,
    /// <summary>Cancellato: container fermati, dati conservati per retention.</summary>
    Canceled = 4
}

/// <summary>
/// Tipo di server/infrastruttura su cui provisionare i progetti.
/// Seleziona quale IInfrastructureProvider istanziare nella factory.
/// </summary>
public enum ServerType
{
    /// <summary>VPS gestita via Docker.DotNet + reverse proxy Traefik.</summary>
    VpsDocker = 0,
    /// <summary>AWS Elastic Container Service via AWS SDK for .NET.</summary>
    AwsEcs = 1
}

/// <summary>
/// Modalità Stripe globale decisa dal Super Admin (non dal cliente).
/// Simulated = billing finto, nessuna chiamata a Stripe: l'acquisto attiva subito
///             abbonamento e provisioning (utile per demo/onboarding senza Stripe).
/// Test      = Stripe sandbox (chiavi test).
/// Live      = Stripe produzione (chiavi live).
/// </summary>
public enum StripeMode
{
    Simulated = 0,
    Test = 1,
    Live = 2
}

/// <summary>
/// Ruolo di un utente del Control Plane.
/// SuperAdmin = proprietario della piattaforma (gestisce Stripe, piani, server).
/// TenantOwner = cliente self-service che gestisce la propria org e i progetti.
/// </summary>
public enum PortalUserRole
{
    SuperAdmin = 0,
    TenantOwner = 1
}

/// <summary>
/// Stato di provisioning di una singola istanza/progetto cliente.
/// </summary>
public enum ProvisioningStatus
{
    /// <summary>Non ancora provisionato.</summary>
    NotProvisioned = 0,
    /// <summary>Provisioning in corso (job in coda/esecuzione).</summary>
    Provisioning = 1,
    /// <summary>Container attivi e raggiungibili.</summary>
    Running = 2,
    /// <summary>Container fermati ma dati intatti.</summary>
    Stopped = 3,
    /// <summary>Provisioning fallito.</summary>
    Failed = 4
}
