using Aski.Shared;

namespace Aski.ControlPlane.Entities;

/// <summary>
/// Piano d'abbonamento configurato dal Super Admin e sincronizzato con Stripe.
///
/// Un Plan = un Price di Stripe = un singolo periodo (mensile O annuale).
/// Per offrire mensile e annuale dello stesso prodotto si creano due Plan
/// che condividono lo stesso <see cref="StripeProductId"/> ma hanno
/// <see cref="StripePriceId"/> e <see cref="Period"/> distinti.
/// </summary>
public class Plan
{
    public int Id { get; set; }

    /// <summary>Nome commerciale del piano (es. "Starter", "Pro").</summary>
    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Id del Product su Stripe (prod_...). Null finché non sincronizzato.</summary>
    public string? StripeProductId { get; set; }

    /// <summary>Id del Price su Stripe (price_...). Null finché non sincronizzato.</summary>
    public string? StripePriceId { get; set; }

    /// <summary>Importo in unità minime della valuta (es. centesimi). 1999 = 19,99.</summary>
    public long Amount { get; set; }

    /// <summary>Valuta ISO-4217 minuscola come richiesto da Stripe (es. "eur", "usd").</summary>
    public required string Currency { get; set; }

    /// <summary>Periodo di fatturazione: mensile o annuale.</summary>
    public BillingPeriod Period { get; set; }

    /// <summary>Se false il piano non è acquistabile nel Customer Portal.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
