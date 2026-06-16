using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Provisioning;
using Aski.ControlPlane.Services.Stripe;
using Aski.ControlPlane.Web;
using Aski.ControlPlane.Web.ViewModels;
using Aski.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>
/// Area cliente self-service (stile Supabase): il TenantOwner loggato gestisce
/// SOLO la propria org. Tutte le operazioni sono vincolate al tenantId del cookie,
/// mai a un id passato dall'esterno.
/// </summary>
[Authorize(Policy = "Tenant")]
public sealed class PortalController : Controller
{
    private readonly ControlPlaneDbContext _db;
    private readonly StripeService _stripe;
    private readonly IProvisioningCoordinator _coordinator;
    private readonly IConfiguration _config;
    private readonly Aski.ControlPlane.Services.Audit.IAuditLogger _audit;

    public PortalController(
        ControlPlaneDbContext db, StripeService stripe,
        IProvisioningCoordinator coordinator, IConfiguration config,
        Aski.ControlPlane.Services.Audit.IAuditLogger audit)
    {
        _db = db;
        _stripe = stripe;
        _coordinator = coordinator;
        _config = config;
        _audit = audit;
    }

    /// <summary>Id del tenant del cliente loggato (0 se assente → non dovrebbe accadere).</summary>
    private int CurrentTenantId => User.TenantId() ?? 0;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .Include(t => t.Projects).ThenInclude(p => p.Server)
            .Include(t => t.Subscriptions).ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Id == CurrentTenantId, ct);
        if (tenant is null) return RedirectToAction("Logout", "Account");

        var mode = (await _db.StripeSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct))?.Mode
                   ?? StripeMode.Simulated;
        // In modalità Stripe servono piani sincronizzati (con price); in Simulato basta che siano attivi.
        var plansQuery = _db.Plans.AsNoTracking().Where(p => p.IsActive);
        if (mode != StripeMode.Simulated)
            plansQuery = plansQuery.Where(p => p.StripePriceId != null);

        var vm = new TenantDetailViewModel
        {
            Tenant = tenant,
            AvailablePlans = await plansQuery.OrderBy(p => p.Amount).ToListAsync(ct),
            EnabledServers = await _db.Servers.AsNoTracking()
                .Where(s => s.IsEnabled).OrderBy(s => s.Name).ToListAsync(ct)
        };
        ViewData["Title"] = tenant.CompanyName;
        ViewData["Subtitle"] = "I tuoi progetti e abbonamenti";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProject(TenantDetailViewModel form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.ProjectName) || form.ServerId == 0)
        {
            TempData["Error"] = "Nome progetto e server obbligatori.";
            return RedirectToAction(nameof(Index));
        }
        // Verifica che il server sia abilitato (non fidarsi del valore dal form).
        if (!await _db.Servers.AnyAsync(s => s.Id == form.ServerId && s.IsEnabled, ct))
        {
            TempData["Error"] = "Server non valido.";
            return RedirectToAction(nameof(Index));
        }

        _db.Projects.Add(new Project
        {
            TenantId = CurrentTenantId,
            Name = form.ProjectName.Trim(),
            ServerId = form.ServerId,
            Subdomain = form.Subdomain,
            CustomDomain = form.CustomDomain,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("project.create", $"Tenant#{CurrentTenantId}", form.ProjectName.Trim(), ct);
        TempData["Success"] = $"Progetto '{form.ProjectName}' creato.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Attiva un piano per un progetto. La modalità (Simulato / Stripe Test / Live) è
    /// decisa dal Super Admin: il cliente preme sempre "Attiva", il sistema sceglie il flusso.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int projectId, int planId, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == CurrentTenantId, ct);
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == CurrentTenantId, ct);
        if (tenant is null || plan is null || project is null)
        {
            TempData["Error"] = "Piano o progetto non valido.";
            return RedirectToAction(nameof(Index));
        }

        var settings = await _db.StripeSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var mode = settings?.Mode ?? StripeMode.Simulated;

        // --- Modalità SIMULATO: nessuna chiamata a Stripe, attivazione immediata ---
        if (mode == StripeMode.Simulated)
        {
            var sub = new Subscription
            {
                TenantId = tenant.Id,
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StripeSubscriptionId = $"sim_{Guid.NewGuid():N}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.Subscriptions.Add(sub);
            await _db.SaveChangesAsync(ct);

            project.SubscriptionId = sub.Id;
            project.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _coordinator.ProvisionAndStartAsync(project.Id, ct);
            await _audit.LogAsync("billing.simulated.activate", $"Project#{project.Id}", $"plan={plan.Name}", ct);
            TempData["Success"] = "Piano attivato (modalità simulata) e progetto provisionato.";
            return RedirectToAction(nameof(Index));
        }

        // --- Modalità STRIPE (Test/Live): redirect a Stripe Checkout ---
        try
        {
            var baseUrl = _config["Portal:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
            var url = await _stripe.CreateCheckoutSessionAsync(
                tenant, plan, project.Id,
                successUrl: $"{baseUrl}/Portal?checkout=success",
                cancelUrl: $"{baseUrl}/Portal?checkout=cancel",
                ct);
            await _audit.LogAsync("billing.checkout.start", $"Project#{project.Id}", $"plan={plan.Name}", ct);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Checkout non disponibile: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Billing(CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == CurrentTenantId, ct);
        if (tenant is null) return RedirectToAction(nameof(Index));

        try
        {
            var baseUrl = _config["Portal:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
            var url = await _stripe.CreateCustomerPortalSessionAsync(tenant, $"{baseUrl}/Portal", ct);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Customer Portal non disponibile: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

}
