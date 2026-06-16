using Aski.ControlPlane.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>
/// Panoramica clienti per il Super Admin (sola lettura). La creazione dei clienti
/// è self-service: qui si osservano solamente org, progetti e abbonamenti.
/// </summary>
[Authorize(Policy = "SuperAdmin")]
public sealed class AdminCustomersController : Controller
{
    private readonly ControlPlaneDbContext _db;

    public AdminCustomersController(ControlPlaneDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tenants = await _db.Tenants.AsNoTracking()
            .Include(t => t.Projects)
            .Include(t => t.Subscriptions)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);
        ViewData["Title"] = "Clienti";
        ViewData["Subtitle"] = "Org registrate (sola lettura)";
        return View(tenants);
    }
}
