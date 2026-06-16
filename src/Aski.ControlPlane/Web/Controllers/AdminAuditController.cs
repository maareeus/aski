using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>Registro di audit (sola lettura) per il Super Admin.</summary>
[Authorize(Policy = "SuperAdmin")]
public sealed class AdminAuditController : Controller
{
    private readonly ControlPlaneDbContext _db;

    public AdminAuditController(ControlPlaneDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var entries = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);
        ViewData["Title"] = "Audit";
        ViewData["Subtitle"] = "Ultime 200 operazioni";
        return View(entries);
    }
}
