using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Provisioning;
using Aski.ControlPlane.Services.Stripe;
using Aski.ControlPlane.Web.ViewModels;
using Aski.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>
/// Customer Portal: registrazione tenant, gestione progetti, acquisto piano
/// (Stripe Checkout) e gestione fatturazione (Stripe Customer Portal).
/// </summary>
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

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new PortalIndexViewModel
        {
            Tenants = await _db.Tenants.AsNoTracking()
                .Include(t => t.Projects)
                .Include(t => t.Subscriptions)
                .OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct)
        };
        ViewData["Title"] = "Tenant";
        ViewData["Subtitle"] = "Aziende clienti";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(PortalIndexViewModel form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.CompanyName) || string.IsNullOrWhiteSpace(form.BillingEmail))
        {
            TempData["Error"] = "Ragione sociale ed email obbligatorie.";
            return RedirectToAction(nameof(Index));
        }

        _db.Tenants.Add(new Tenant
        {
            CompanyName = form.CompanyName.Trim(),
            BillingEmail = form.BillingEmail.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = $"Tenant '{form.CompanyName}' registrato.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Tenant(int id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .Include(t => t.Projects).ThenInclude(p => p.Server)
            .Include(t => t.Subscriptions).ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return NotFound();

        var vm = new TenantDetailViewModel
        {
            Tenant = tenant,
            AvailablePlans = await _db.Plans.AsNoTracking()
                .Where(p => p.IsActive && p.StripePriceId != null).OrderBy(p => p.Amount).ToListAsync(ct),
            EnabledServers = await _db.Servers.AsNoTracking()
                .Where(s => s.IsEnabled).OrderBy(s => s.Name).ToListAsync(ct)
        };
        ViewData["Title"] = tenant.CompanyName;
        ViewData["Subtitle"] = "Progetti e abbonamenti";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProject(int id, TenantDetailViewModel form, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return NotFound();

        if (string.IsNullOrWhiteSpace(form.ProjectName) || form.ServerId == 0)
        {
            TempData["Error"] = "Nome progetto e server obbligatori.";
            return RedirectToAction(nameof(Tenant), new { id });
        }

        _db.Projects.Add(new Project
        {
            TenantId = id,
            Name = form.ProjectName.Trim(),
            ServerId = form.ServerId,
            Subdomain = form.Subdomain,
            CustomDomain = form.CustomDomain,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = $"Progetto '{form.ProjectName}' creato.";
        return RedirectToAction(nameof(Tenant), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int id, int planId, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, ct);
        if (tenant is null || plan is null)
        {
            TempData["Error"] = "Tenant o piano non valido.";
            return RedirectToAction(nameof(Tenant), new { id });
        }

        try
        {
            var baseUrl = _config["Portal:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
            var url = await _stripe.CreateCheckoutSessionAsync(
                tenant, plan,
                successUrl: $"{baseUrl}/Portal/Tenant/{id}?checkout=success",
                cancelUrl: $"{baseUrl}/Portal/Tenant/{id}?checkout=cancel",
                ct);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Checkout non disponibile: {ex.Message}";
            return RedirectToAction(nameof(Tenant), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Billing(int id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return NotFound();

        try
        {
            var baseUrl = _config["Portal:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
            var url = await _stripe.CreateCustomerPortalSessionAsync(tenant, $"{baseUrl}/Portal/Tenant/{id}", ct);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Customer Portal non disponibile: {ex.Message}";
            return RedirectToAction(nameof(Tenant), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Provision(int id, int projectId, CancellationToken ct)
    {
        try
        {
            await _coordinator.ProvisionAndStartAsync(projectId, ct);
            TempData["Success"] = "Provisioning avviato.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Provisioning fallito: {ex.Message}";
        }
        return RedirectToAction(nameof(Tenant), new { id });
    }
}
