using Aski.ControlPlane.Data;
using Aski.ControlPlane.Web.ViewModels;
using Aski.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>Dashboard Super Admin: panoramica della piattaforma.</summary>
public sealed class DashboardController : Controller
{
    private readonly ControlPlaneDbContext _db;

    public DashboardController(ControlPlaneDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settings = await _db.StripeSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var vm = new DashboardViewModel
        {
            StripeConfigured = settings is not null,
            IsTestMode = settings?.IsTestMode ?? true,
            PlanCount = await _db.Plans.CountAsync(ct),
            ServerCount = await _db.Servers.CountAsync(ct),
            TenantCount = await _db.Tenants.CountAsync(ct),
            ProjectCount = await _db.Projects.CountAsync(ct),
            ActiveSubscriptions = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active, ct),
            RecentProjects = await _db.Projects.AsNoTracking()
                .Include(p => p.Tenant)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Take(8)
                .ToListAsync(ct)
        };
        ViewData["Title"] = "Dashboard";
        return View(vm);
    }
}
