using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.Shared;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
// Disambigua dalle entità Stripe omonime. BillingPortal NON importato per evitare
// il clash SessionService/SessionCreateOptions con Checkout: usato via global::.
using Plan = Aski.ControlPlane.Entities.Plan;

namespace Aski.ControlPlane.Services.Stripe;

/// <summary>
/// Facciata applicativa su Stripe: sincronizzazione piani, sessioni di Checkout
/// e di Customer Portal. Tutte le chiamate usano il client risolto a runtime da
/// <see cref="IStripeContextProvider"/> (chiavi Test o Live secondo IsTestMode).
/// </summary>
public sealed class StripeService
{
    private readonly IStripeContextProvider _ctx;
    private readonly ControlPlaneDbContext _db;
    private readonly ILogger<StripeService> _log;

    public StripeService(IStripeContextProvider ctx, ControlPlaneDbContext db, ILogger<StripeService> log)
    {
        _ctx = ctx;
        _db = db;
        _log = log;
    }

    /// <summary>
    /// Sincronizza un Plan con Stripe: crea il Product se assente e (ri)crea un Price.
    /// I Price di Stripe sono immutabili: se importo/valuta/periodo cambiano si crea
    /// un nuovo Price e si aggiorna lo StripePriceId del piano.
    /// </summary>
    public async Task SyncPlanAsync(Plan plan, CancellationToken ct = default)
    {
        var ctx = await _ctx.GetAsync(ct);
        var productService = new ProductService(ctx.Client);
        var priceService = new PriceService(ctx.Client);

        // 1. Product
        if (string.IsNullOrWhiteSpace(plan.StripeProductId))
        {
            var product = await productService.CreateAsync(new ProductCreateOptions
            {
                Name = plan.Name,
                Description = string.IsNullOrWhiteSpace(plan.Description) ? null : plan.Description
            }, cancellationToken: ct);
            plan.StripeProductId = product.Id;
        }
        else
        {
            await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
            {
                Name = plan.Name,
                Description = string.IsNullOrWhiteSpace(plan.Description) ? null : plan.Description
            }, cancellationToken: ct);
        }

        // 2. Price (nuovo se non esiste o se i parametri economici sono cambiati)
        var needsNewPrice = string.IsNullOrWhiteSpace(plan.StripePriceId);
        if (!needsNewPrice)
        {
            var current = await priceService.GetAsync(plan.StripePriceId, cancellationToken: ct);
            needsNewPrice = current.UnitAmount != plan.Amount
                            || !string.Equals(current.Currency, plan.Currency, StringComparison.OrdinalIgnoreCase)
                            || current.Recurring?.Interval != ToStripeInterval(plan.Period);
            if (needsNewPrice)
            {
                // Disattiva il vecchio price per non lasciarlo acquistabile.
                await priceService.UpdateAsync(plan.StripePriceId,
                    new PriceUpdateOptions { Active = false }, cancellationToken: ct);
            }
        }

        if (needsNewPrice)
        {
            var price = await priceService.CreateAsync(new PriceCreateOptions
            {
                Product = plan.StripeProductId,
                UnitAmount = plan.Amount,
                Currency = plan.Currency,
                Recurring = new PriceRecurringOptions { Interval = ToStripeInterval(plan.Period) }
            }, cancellationToken: ct);
            plan.StripePriceId = price.Id;
        }

        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Piano {PlanId} sincronizzato con Stripe (product={Product}, price={Price})",
            plan.Id, plan.StripeProductId, plan.StripePriceId);
    }

    /// <summary>
    /// Crea una sessione di Stripe Checkout (mode=subscription) per l'acquisto di un piano.
    /// Crea il Customer Stripe del tenant se mancante. I metadata trasportano tenantId
    /// e planId così il webhook può correlare la subscription risultante.
    /// </summary>
    public async Task<string> CreateCheckoutSessionAsync(
        Tenant tenant, Plan plan, int projectId, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            throw new InvalidOperationException($"Piano {plan.Id} non sincronizzato con Stripe (StripePriceId nullo).");

        var ctx = await _ctx.GetAsync(ct);

        // Customer Stripe (creato una volta sola e ridondato sul tenant).
        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            var customer = await new CustomerService(ctx.Client).CreateAsync(new CustomerCreateOptions
            {
                Name = tenant.CompanyName,
                Email = tenant.BillingEmail,
                Metadata = new Dictionary<string, string> { ["tenantId"] = tenant.Id.ToString() }
            }, cancellationToken: ct);
            tenant.StripeCustomerId = customer.Id;
            tenant.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        var metadata = new Dictionary<string, string>
        {
            ["tenantId"] = tenant.Id.ToString(),
            ["planId"] = plan.Id.ToString(),
            ["projectId"] = projectId.ToString()
        };

        var session = await new SessionService(ctx.Client).CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = tenant.StripeCustomerId,
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = plan.StripePriceId, Quantity = 1 }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            // I metadata vengono propagati anche alla Subscription per il webhook.
            Metadata = metadata,
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata }
        }, cancellationToken: ct);

        return session.Url;
    }

    /// <summary>
    /// Crea una sessione di Stripe Customer Portal per gestione carta/disdetta.
    /// </summary>
    public async Task<string> CreateCustomerPortalSessionAsync(
        Tenant tenant, string returnUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
            throw new InvalidOperationException($"Tenant {tenant.Id} senza StripeCustomerId.");

        var ctx = await _ctx.GetAsync(ct);
        var session = await new global::Stripe.BillingPortal.SessionService(ctx.Client).CreateAsync(
            new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = tenant.StripeCustomerId,
                ReturnUrl = returnUrl
            }, cancellationToken: ct);

        return session.Url;
    }

    /// <summary>Mappa il periodo interno sull'interval ricorrente di Stripe.</summary>
    private static string ToStripeInterval(BillingPeriod period) => period switch
    {
        BillingPeriod.Monthly => "month",
        BillingPeriod.Annual => "year",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Periodo non supportato")
    };
}
