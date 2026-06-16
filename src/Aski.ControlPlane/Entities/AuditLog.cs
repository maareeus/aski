namespace Aski.ControlPlane.Entities;

/// <summary>
/// Voce di audit: traccia chi ha fatto cosa nel Control Plane (operazioni
/// amministrative sensibili, autenticazione, provisioning). Append-only.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Id dell'utente che ha eseguito l'azione (null se anonimo/sistema).</summary>
    public int? ActorUserId { get; set; }

    /// <summary>Email dell'attore (ridondata per leggibilità storica).</summary>
    public string? ActorEmail { get; set; }

    /// <summary>Codice azione, es. "stripe.settings.update", "plan.create", "auth.login".</summary>
    public required string Action { get; set; }

    /// <summary>Oggetto dell'azione, es. "Plan#3", "Server#1", "Project#7".</summary>
    public string? Target { get; set; }

    /// <summary>Dettagli liberi (mai segreti in chiaro).</summary>
    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
