namespace Aski.ControlPlane.Entities;

/// <summary>
/// Azienda cliente registrata sul Customer Portal.
/// Mappa 1:1 con un Customer di Stripe (<see cref="StripeCustomerId"/>).
/// Possiede uno o più <see cref="Project"/> (istanze di ticketing) e i relativi abbonamenti.
/// </summary>
public class Tenant
{
    public int Id { get; set; }

    public required string CompanyName { get; set; }

    /// <summary>Email di contatto principale del tenant.</summary>
    public required string BillingEmail { get; set; }

    /// <summary>Id del Customer su Stripe (cus_...). Creato al primo checkout.</summary>
    public string? StripeCustomerId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<Project> Projects { get; set; } = new();
    public List<Subscription> Subscriptions { get; set; } = new();
}
