using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>Configurazione Stripe del Super Admin (chiavi Test/Live + toggle sandbox).</summary>
[Authorize(Policy = "SuperAdmin")]
public sealed class StripeAdminController : Controller
{
    private readonly ControlPlaneDbContext _db;

    public StripeAdminController(ControlPlaneDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var s = await _db.StripeSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        ViewData["Title"] = "Stripe";
        ViewData["Subtitle"] = "Chiavi API e modalità";
        return View(ToViewModel(s));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(StripeSettingsViewModel form, CancellationToken ct)
    {
        var s = await _db.StripeSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null)
        {
            s = new StripeSettings { Id = 1 };
            _db.StripeSettings.Add(s);
        }

        s.IsTestMode = form.IsTestMode;
        s.TestPublishableKey = form.TestPublishableKey;
        s.LivePublishableKey = form.LivePublishableKey;

        // I segreti si aggiornano solo se reinseriti (i campi vuoti non sovrascrivono).
        if (!string.IsNullOrWhiteSpace(form.NewTestSecretKey)) s.TestSecretKey = form.NewTestSecretKey.Trim();
        if (!string.IsNullOrWhiteSpace(form.NewTestWebhookSecret)) s.TestWebhookSecret = form.NewTestWebhookSecret.Trim();
        if (!string.IsNullOrWhiteSpace(form.NewLiveSecretKey)) s.LiveSecretKey = form.NewLiveSecretKey.Trim();
        if (!string.IsNullOrWhiteSpace(form.NewLiveWebhookSecret)) s.LiveWebhookSecret = form.NewLiveWebhookSecret.Trim();

        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = $"Impostazioni salvate. Modalità attiva: {(s.IsTestMode ? "TEST (sandbox)" : "LIVE")}.";
        return RedirectToAction(nameof(Index));
    }

    private static StripeSettingsViewModel ToViewModel(StripeSettings? s) => new()
    {
        Configured = s is not null,
        IsTestMode = s?.IsTestMode ?? true,
        TestPublishableKey = s?.TestPublishableKey,
        TestSecretKeySet = !string.IsNullOrEmpty(s?.TestSecretKey),
        TestWebhookSecretSet = !string.IsNullOrEmpty(s?.TestWebhookSecret),
        LivePublishableKey = s?.LivePublishableKey,
        LiveSecretKeySet = !string.IsNullOrEmpty(s?.LiveSecretKey),
        LiveWebhookSecretSet = !string.IsNullOrEmpty(s?.LiveWebhookSecret)
    };
}
