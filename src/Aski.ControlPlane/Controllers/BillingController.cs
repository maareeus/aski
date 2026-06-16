using Aski.ControlPlane.Data;
using Aski.ControlPlane.Services.Stripe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Controllers;

/// <summary>
/// Endpoint usati dal Customer Portal per avviare Stripe Checkout (acquisto) e
/// Stripe Customer Portal (gestione carta/disdetta). Restituiscono l'URL a cui
/// reindirizzare il browser dell'utente.
/// </summary>
[ApiController]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly StripeService _stripe;
    private readonly ControlPlaneDbContext _db;
    private readonly IConfiguration _config;

    public BillingController(StripeService stripe, ControlPlaneDbContext db, IConfiguration config)
    {
        _stripe = stripe;
        _db = db;
        _config = config;
    }

    public record CheckoutRequest(int TenantId, int PlanId);
    public record PortalRequest(int TenantId);
    public record RedirectResponse(string Url);

    /// <summary>Crea una sessione di Checkout e ritorna l'URL di redirect.</summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<RedirectResponse>> Checkout(CheckoutRequest req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId, ct);
        if (tenant is null) return NotFound("Tenant non trovato");

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.PlanId && p.IsActive, ct);
        if (plan is null) return NotFound("Piano non trovato o non attivo");

        var baseUrl = PortalBaseUrl();
        var url = await _stripe.CreateCheckoutSessionAsync(
            tenant, plan,
            successUrl: $"{baseUrl}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",
            cancelUrl: $"{baseUrl}/billing/cancel",
            ct);

        return Ok(new RedirectResponse(url));
    }

    /// <summary>Crea una sessione di Customer Portal e ritorna l'URL di redirect.</summary>
    [HttpPost("portal")]
    public async Task<ActionResult<RedirectResponse>> Portal(PortalRequest req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId, ct);
        if (tenant is null) return NotFound("Tenant non trovato");

        var url = await _stripe.CreateCustomerPortalSessionAsync(
            tenant, returnUrl: $"{PortalBaseUrl()}/billing", ct);

        return Ok(new RedirectResponse(url));
    }

    private string PortalBaseUrl() =>
        _config["Portal:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
}
