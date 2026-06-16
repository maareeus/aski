using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>Gestione server/regioni e del limite N di progetti per container Postgres.</summary>
[Authorize(Policy = "SuperAdmin")]
public sealed class ServerAdminController : Controller
{
    private readonly ControlPlaneDbContext _db;
    private readonly Aski.ControlPlane.Services.Audit.IAuditLogger _audit;

    public ServerAdminController(ControlPlaneDbContext db, Aski.ControlPlane.Services.Audit.IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new ServersViewModel
        {
            Servers = await _db.Servers.AsNoTracking()
                .Include(s => s.DbContainers)
                .OrderBy(s => s.Name).ToListAsync(ct)
        };
        ViewData["Title"] = "Server";
        ViewData["Subtitle"] = "Regioni e pool Postgres";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServersViewModel form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Name) || string.IsNullOrWhiteSpace(form.Region))
        {
            TempData["Error"] = "Nome e regione obbligatori.";
            return RedirectToAction(nameof(Index));
        }

        _db.Servers.Add(new Server
        {
            Name = form.Name.Trim(),
            Region = form.Region.Trim(),
            Type = form.Type,
            Hostname = form.Hostname,
            ConfigJson = form.ConfigJson,
            MaxProjectsPerDbContainer = form.MaxProjectsPerDbContainer <= 0 ? 10 : form.MaxProjectsPerDbContainer,
            IsEnabled = form.IsEnabled,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("server.create", null, $"{form.Name} ({form.Region}) N={form.MaxProjectsPerDbContainer}", ct);
        TempData["Success"] = $"Server '{form.Name}' creato.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var server = await _db.Servers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (server is not null)
        {
            server.IsEnabled = !server.IsEnabled;
            server.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("server.toggle", $"Server#{server.Id}", $"enabled={server.IsEnabled}", ct);
            TempData["Success"] = $"Server '{server.Name}' {(server.IsEnabled ? "abilitato" : "disabilitato")}.";
        }
        return RedirectToAction(nameof(Index));
    }
}
