using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Endpoint dedicati al Customer Portal: accessibili solo ai Client.
/// Espongono i software della loro azienda (con note di rilascio) e i contatti
/// degli operatori che assistono quei software. I ticket usano /api/tickets
/// (già limitato all'azienda del client).
/// </summary>
[ApiController]
[Route("api/portal")]
[Authorize(Roles = Roles.Client)]
public sealed class PortalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public PortalController(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    /// <summary>Profilo dell'utente loggato.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = User.Id();
        var u = await _db.Users.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Email, x.FirstName, x.LastName, x.JobTitle, x.Phone, x.CompanyId,
                CompanyName = x.Company!.Name })
            .FirstOrDefaultAsync(ct);
        return u is null ? NotFound() : Ok(u);
    }

    /// <summary>Software in uso dall'azienda, con lo storico versioni e note di rilascio.</summary>
    [HttpGet("software")]
    public async Task<IActionResult> Software(CancellationToken ct)
    {
        var companyId = User.CompanyId();
        if (companyId is null) return BadRequest(new { error = "Utente senza azienda." });

        var data = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .SelectMany(c => c.Softwares)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id, s.Name, s.Description,
                Versions = s.Versions.OrderByDescending(v => v.CreatedAtUtc)
                    .Select(v => new { v.Id, v.Version, v.ReleaseNotes, v.ReleasedAtUtc, v.IsActive, v.CreatedAtUtc })
                    .ToList()
            })
            .ToListAsync(ct);
        return Ok(data);
    }

    /// <summary>
    /// Operatori che assistono l'azienda: staff con competenza su almeno un software
    /// usato dall'azienda. Restituisce i recapiti (mail, telefono, ruolo).
    /// </summary>
    [HttpGet("operators")]
    public async Task<IActionResult> Operators(CancellationToken ct)
    {
        var companyId = User.CompanyId();
        if (companyId is null) return BadRequest(new { error = "Utente senza azienda." });

        var swIds = await _db.Companies.AsNoTracking().Where(c => c.Id == companyId)
            .SelectMany(c => c.Softwares.Select(s => s.Id)).ToListAsync(ct);

        var candidates = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.CompanyId == null && u.Softwares.Any(s => swIds.Contains(s.Id)))
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone,
                Softwares = u.Softwares.Where(s => swIds.Contains(s.Id)).Select(s => s.Name).ToList() })
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in candidates)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            if (appUser is null) continue;
            var roles = await _users.GetRolesAsync(appUser);
            if (!roles.Any(r => r is Roles.Admin or Roles.PM or Roles.Agent)) continue;
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone, u.Softwares, Roles = roles });
        }
        return Ok(result);
    }
}
