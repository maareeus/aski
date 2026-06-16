using Aski.Shared;

namespace Aski.ControlPlane.Entities;

/// <summary>
/// Abbonamento di un tenant a un <see cref="Plan"/>, sincronizzato con una
/// Subscription di Stripe. Il suo <see cref="Status"/> è la sorgente di verità
/// che pilota il ciclo di vita dei container del progetto collegato:
///   Active    -> provisioning / resume
///   PastDue   -> suspend (insoluto)
///   Suspended -> container fermi
///   Canceled  -> container fermi, dati conservati
/// </summary>
public class Subscription
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    /// <summary>Id della Subscription su Stripe (sub_...).</summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Id del Customer su Stripe (cus_...), ridondato per comodità di lookup.</summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>Stato corrente. Default Pending finché il primo pagamento non conferma.</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

    /// <summary>Fine del periodo corrente comunicata da Stripe (per grace period/UI).</summary>
    public DateTime? CurrentPeriodEndUtc { get; set; }

    /// <summary>True se l'utente ha programmato la disdetta a fine periodo.</summary>
    public bool CancelAtPeriodEnd { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Progetto provisionato da questo abbonamento (1:1).</summary>
    public Project? Project { get; set; }
}
