using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Provisioning;
using Aski.ControlPlane.Services.Stripe;
using Aski.ControlPlane.Web;
using Aski.ControlPlane.Web.ViewModels;
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

    public PortalController(
        ControlPlaneDbContext db, StripeService stripe,
        IProvisioningCoordinator coordinator, IConfiguration config)
    {
        _db = db;
        _stripe = stripe;
        _coordinator = coordinator;
        _config = config;
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

        var vm = new TenantDetailViewModel
        {
            Tenant = tenant,
            AvailablePlans = await _db.Plans.AsNoTracking()
                .Where(p => p.IsActive && p.StripePriceId != null).OrderBy(p => p.Amount).ToListAsync(ct),
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
        TempData["Success"] = $"Progetto '{form.ProjectName}' creato.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int planId, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == CurrentTenantId, ct);
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, ct);
        if (tenant is null || plan is null)
        {
            TempData["Error"] = "Piano non valido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var baseUrl = _config["Portal:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
            var url = await _stripe.CreateCheckoutSessionAsync(
                tenant, plan,
                successUrl: $"{baseUrl}/Portal?checkout=success",
                cancelUrl: $"{baseUrl}/Portal?checkout=cancel",
                ct);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Provision(int projectId, CancellationToken ct)
    {
        // Il progetto deve appartenere al tenant loggato.
        var owns = await _db.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == CurrentTenantId, ct);
        if (!owns) return Forbid();

        try
        {
            await _coordinator.ProvisionAndStartAsync(projectId, ct);
            TempData["Success"] = "Provisioning avviato.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Provisioning fallito: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
