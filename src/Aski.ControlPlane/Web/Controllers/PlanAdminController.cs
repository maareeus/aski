using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Stripe;
using Aski.ControlPlane.Web.ViewModels;
using Aski.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>Gestione piani d'abbonamento e sincronizzazione con Stripe.</summary>
[Authorize(Policy = "SuperAdmin")]
public sealed class PlanAdminController : Controller
{
    private readonly ControlPlaneDbContext _db;
    private readonly StripeService _stripe;
    private readonly Aski.ControlPlane.Services.Audit.IAuditLogger _audit;

    public PlanAdminController(ControlPlaneDbContext db, StripeService stripe, Aski.ControlPlane.Services.Audit.IAuditLogger audit)
    {
        _db = db;
        _stripe = stripe;
        _audit = audit;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new PlansViewModel
        {
            Plans = await _db.Plans.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct),
            StripeConfigured = await _db.StripeSettings.AnyAsync(x => x.Id == 1, ct)
        };
        ViewData["Title"] = "Piani";
        ViewData["Subtitle"] = "Listini sincronizzati con Stripe";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlansViewModel form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Name) || form.Price <= 0)
        {
            TempData["Error"] = "Nome e prezzo (> 0) obbligatori.";
            return RedirectToAction(nameof(Index));
        }

        var plan = new Plan
        {
            Name = form.Name.Trim(),
            Description = form.Description,
            // Prezzo inserito in unità intere (es. 19,99) -> centesimi.
            Amount = (long)Math.Round(form.Price * 100m),
            Currency = (form.Currency ?? "eur").ToLowerInvariant(),
            Period = form.Period,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("plan.create", $"Plan#{plan.Id}", $"{plan.Name} {plan.Amount} {plan.Currency}", ct);

        try
        {
            await _stripe.SyncPlanAsync(plan, ct);
            TempData["Success"] = $"Piano '{plan.Name}' creato e sincronizzato (price {plan.StripePriceId}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Piano creato ma sincronizzazione Stripe fallita: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(int id, CancellationToken ct)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return RedirectToAction(nameof(Index));
        try
        {
            await _stripe.SyncPlanAsync(plan, ct);
            await _audit.LogAsync("plan.sync", $"Plan#{plan.Id}", plan.Name, ct);
            TempData["Success"] = $"Piano '{plan.Name}' ri-sincronizzato.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Sincronizzazione fallita: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
