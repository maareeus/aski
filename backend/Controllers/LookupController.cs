using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>Elenchi di supporto per i selettori (accessibili a staff Admin/PM).</summary>
[ApiController]
[Route("api/lookup")]
[Authorize]
public sealed class LookupController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    public LookupController(AppDbContext db, UserManager<AppUser> users) { _db = db; _users = users; }

    /// <summary>Le Unit a cui appartiene l'utente corrente (per la presa in carico).</summary>
    [HttpGet("my-units")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> MyUnits(CancellationToken ct)
    {
        var uid = Auth.CurrentUser.Id(User);
        return Ok(await _db.UnitMemberships.AsNoTracking()
            .Where(m => m.UserId == uid)
            .Select(m => new { m.UnitId, m.Unit.Name, m.IsManager })
            .ToListAsync(ct));
    }

    /// <summary>Utenti staff (Admin/PM/Agent) con i loro ruoli — per i selettori di Unit.</summary>
    [HttpGet("staff")]
    [Authorize(Roles = "Admin,PM")]
    public async Task<IActionResult> Staff(CancellationToken ct)
    {
        var rows = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in rows)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            var roles = appUser is null ? Array.Empty<string>() : (await _users.GetRolesAsync(appUser)).ToArray();
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.PM) || roles.Contains(Roles.Agent))
                result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, Roles = roles });
        }
        return Ok(result);
    }
}
