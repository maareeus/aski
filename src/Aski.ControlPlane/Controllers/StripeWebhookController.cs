using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stripe;
// Disambigua dall'omonimo Stripe.StripeContext.
using StripeContext = Aski.ControlPlane.Services.Stripe.StripeContext;

namespace Aski.ControlPlane.Controllers;

/// <summary>
/// Endpoint pubblico che riceve i webhook di Stripe.
///
/// Sicurezza: la firma <c>Stripe-Signature</c> è verificata con il webhook secret
/// attivo (Test/Live) tramite EventUtility.ConstructEvent. Senza firma valida la
/// richiesta è respinta con 400.
///
/// Idempotenza: ogni evento è registrato in ProcessedStripeEvents; un evento già
/// visto restituisce subito 200 senza rielaborazione (Stripe ritrasmette).
/// </summary>
[ApiController]
[Route("api/stripe/webhook")]
[AllowAnonymous] // Endpoint pubblico per Stripe: l'autenticità è garantita dalla firma del webhook.
[EnableRateLimiting("webhook")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly IStripeContextProvider _ctx;
    private readonly StripeWebhookHandler _handler;
    private readonly ControlPlaneDbContext _db;
    private readonly ILogger<StripeWebhookController> _log;

    public StripeWebhookController(
        IStripeContextProvider ctx,
        StripeWebhookHandler handler,
        ControlPlaneDbContext db,
        ILogger<StripeWebhookController> log)
    {
        _ctx = ctx;
        _handler = handler;
        _db = db;
        _log = log;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        StripeContext ctx;
        try
        {
            ctx = await _ctx.GetAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Stripe non configurato: impossibile verificare il webhook");
            return StatusCode(500, "Stripe non configurato");
        }

        Event stripeEvent;
        try
        {
            // Verifica della firma: throttle anche su payload manomessi.
            stripeEvent = EventUtility.ConstructEvent(json, signature, ctx.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Firma webhook Stripe non valida");
            return BadRequest("Firma non valida");
        }

        // Idempotenza: già processato -> 200 immediato.
        var already = await _db.ProcessedStripeEvents.FindAsync([stripeEvent.Id], ct);
        if (already is not null)
        {
            _log.LogDebug("Evento {Id} già processato, skip", stripeEvent.Id);
            return Ok();
        }

        try
        {
            await _handler.HandleAsync(stripeEvent, ct);
        }
        catch (Exception ex)
        {
            // Non marchiamo come processato: Stripe ritrasmetterà.
            _log.LogError(ex, "Errore nel gestire l'evento {Type} {Id}", stripeEvent.Type, stripeEvent.Id);
            return StatusCode(500, "Errore di elaborazione");
        }

        _db.ProcessedStripeEvents.Add(new ProcessedStripeEvent
        {
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Ok();
    }
}
