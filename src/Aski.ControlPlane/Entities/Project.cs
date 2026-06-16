using Aski.Shared;

namespace Aski.ControlPlane.Entities;

/// <summary>
/// Progetto = singola istanza di ticketing isolata (single-tenant) di un cliente.
///
/// Il provisioning dell'infrastruttura (container app + database nel pool Postgres)
/// è pilotato dallo stato dell'<see cref="Subscription"/> collegato.
/// L'utente sceglie solo il <see cref="Server"/> (regione); l'assegnazione del
/// <see cref="DbContainer"/> avviene automaticamente rispettando il limite N.
/// </summary>
public class Project
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Abbonamento che finanzia questo progetto (1:1).</summary>
    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public required string Name { get; set; }

    /// <summary>Sottodominio assegnato (es. "acme" -> acme.aski.app).</summary>
    public string? Subdomain { get; set; }

    /// <summary>Dominio personalizzato opzionale collegato dal tenant.</summary>
    public string? CustomDomain { get; set; }

    // --- Infrastruttura (valorizzata in fase di provisioning) ---

    /// <summary>Server/regione scelto dal tenant tra quelli abilitati.</summary>
    public int? ServerId { get; set; }
    public Server? Server { get; set; }

    /// <summary>Container Postgres del pool assegnato a questo progetto.</summary>
    public int? DbContainerId { get; set; }
    public DbContainer? DbContainer { get; set; }

    /// <summary>Nome del database logico dentro il container condiviso.</summary>
    public string? DatabaseName { get; set; }

    /// <summary>Utente Postgres dedicato del progetto (privilegi solo sul proprio DB).</summary>
    public string? DbUser { get; set; }

    /// <summary>Password dell'utente dedicato. Cifrata a riposo (DataProtection).</summary>
    public string? DbPassword { get; set; }

    /// <summary>Id runtime del container applicativo (ticketing) provisionato.</summary>
    public string? AppContainerId { get; set; }

    public ProvisioningStatus ProvisioningStatus { get; set; } = ProvisioningStatus.NotProvisioned;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
