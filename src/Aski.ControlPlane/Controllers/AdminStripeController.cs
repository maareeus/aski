using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Stripe;
using Aski.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Controllers;

/// <summary>
/// Endpoint del Super Admin per configurare Stripe e i piani.
/// NB: la protezione con autenticazione/ruolo SuperAdmin va aggiunta in fase di
/// hardening; qui sono esposti per pilotare i test end-to-end del billing.
/// </summary>
[ApiController]
[Route("api/admin")]
public sealed class AdminStripeController : ControllerBase
{
    private readonly ControlPlaneDbContext _db;
    private readonly StripeService _stripe;

    public AdminStripeController(ControlPlaneDbContext db, StripeService stripe)
    {
        _db = db;
        _stripe = stripe;
    }

    // --- Impostazioni Stripe globali (riga singola Id=1) ---

    public record StripeSettingsDto(
        bool IsTestMode,
        string? TestPublishableKey, string? TestSecretKey, string? TestWebhookSecret,
        string? LivePublishableKey, string? LiveSecretKey, string? LiveWebhookSecret);

    /// <summary>Vista mascherata delle impostazioni (i segreti non vengono mai restituiti in chiaro).</summary>
    [HttpGet("stripe-settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var s = await _db.StripeSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null) return Ok(new { configured = false });

        return Ok(new
        {
            configured = true,
            s.IsTestMode,
            testPublishableKey = s.TestPublishableKey,
            testSecretKeySet = !string.IsNullOrEmpty(s.TestSecretKey),
            testWebhookSecretSet = !string.IsNullOrEmpty(s.TestWebhookSecret),
            livePublishableKey = s.LivePublishableKey,
            liveSecretKeySet = !string.IsNullOrEmpty(s.LiveSecretKey),
            liveWebhookSecretSet = !string.IsNullOrEmpty(s.LiveWebhookSecret)
        });
    }

    /// <summary>Crea/aggiorna le chiavi Stripe. Campi null lasciano invariato il valore esistente.</summary>
    [HttpPut("stripe-settings")]
    public async Task<IActionResult> UpsertSettings(StripeSettingsDto dto, CancellationToken ct)
    {
        var s = await _db.StripeSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null)
        {
            s = new StripeSettings { Id = 1 };
            _db.StripeSettings.Add(s);
        }

        s.IsTestMode = dto.IsTestMode;
        s.TestPublishableKey = dto.TestPublishableKey ?? s.TestPublishableKey;
        s.TestSecretKey = dto.TestSecretKey ?? s.TestSecretKey;
        s.TestWebhookSecret = dto.TestWebhookSecret ?? s.TestWebhookSecret;
        s.LivePublishableKey = dto.LivePublishableKey ?? s.LivePublishableKey;
        s.LiveSecretKey = dto.LiveSecretKey ?? s.LiveSecretKey;
        s.LiveWebhookSecret = dto.LiveWebhookSecret ?? s.LiveWebhookSecret;
        s.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Toggle rapido della modalità Test/Live.</summary>
    [HttpPost("stripe-settings/test-mode/{enabled:bool}")]
    public async Task<IActionResult> SetTestMode(bool enabled, CancellationToken ct)
    {
        var s = await _db.StripeSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null) return NotFound("Impostazioni Stripe non configurate");
        s.IsTestMode = enabled;
        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { s.IsTestMode });
    }

    // --- Piani ---

    public record PlanDto(string Name, string? Description, long Amount, string Currency, BillingPeriod Period);

    /// <summary>Crea un piano e lo sincronizza con Stripe (Product + Price).</summary>
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan(PlanDto dto, CancellationToken ct)
    {
        var plan = new Plan
        {
            Name = dto.Name,
            Description = dto.Description,
            Amount = dto.Amount,
            Currency = dto.Currency.ToLowerInvariant(),
            Period = dto.Period,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);

        await _stripe.SyncPlanAsync(plan, ct);
        return CreatedAtAction(nameof(CreatePlan), new { id = plan.Id },
            new { plan.Id, plan.StripeProductId, plan.StripePriceId });
    }

    /// <summary>Ri-sincronizza un piano esistente con Stripe.</summary>
    [HttpPost("plans/{id:int}/sync")]
    public async Task<IActionResult> SyncPlan(int id, CancellationToken ct)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();
        await _stripe.SyncPlanAsync(plan, ct);
        return Ok(new { plan.Id, plan.StripeProductId, plan.StripePriceId });
    }
}
