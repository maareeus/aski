using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Unit (gruppi di utenti). Admin: crea le Unit e nomina i PM (manager).
/// PM: gestisce i membri delle Unit che gli sono affidate.
/// </summary>
[ApiController]
[Route("api/units")]
[Authorize(Roles = "Admin,PM")]
public sealed class UnitsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    public UnitsController(AppDbContext db, UserManager<AppUser> users) { _db = db; _users = users; }

    public record UnitDto(string Name, string? Description);
    public record UserIdsDto(List<string> UserIds);
    public record UserIdDto(string UserId);

    private Task<bool> ManagesAsync(int unitId, CancellationToken ct) =>
        _db.UnitMemberships.AnyAsync(m => m.UnitId == unitId && m.UserId == User.Id() && m.IsManager, ct);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var q = _db.Units.AsNoTracking().AsQueryable();
        if (!User.IsAdmin())
        {
            var uid = User.Id();
            q = q.Where(u => u.Memberships.Any(m => m.UserId == uid && m.IsManager));
        }
        return Ok(await q.OrderBy(u => u.Name).Select(u => new
        {
            u.Id, u.Name, u.Description,
            MembersCount = u.Memberships.Count(m => !m.IsManager),
            Managers = u.Memberships.Where(m => m.IsManager).Select(m => m.User.Email).ToList()
        }).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        if (!User.IsAdmin() && !await ManagesAsync(id, ct)) return Forbid();
        var unit = await _db.Units.AsNoTracking().Where(u => u.Id == id).Select(u => new
        {
            u.Id, u.Name, u.Description,
            Members = u.Memberships.Select(m => new
            {
                m.UserId, m.IsManager, m.User.Email, m.User.FirstName, m.User.LastName
            }).ToList()
        }).FirstOrDefaultAsync(ct);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(UnitDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        var u = new Unit { Name = dto.Name.Trim(), Description = dto.Description };
        _db.Units.Add(u);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = u.Id }, new { u.Id });
    }

    /// <summary>Admin: imposta i PM (manager) della Unit. Devono avere ruolo PM.</summary>
    [HttpPut("{id:int}/managers")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SetManagers(int id, UserIdsDto dto, CancellationToken ct)
    {
        var unit = await _db.Units.Include(u => u.Memberships).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (unit is null) return NotFound();

        foreach (var uid in dto.UserIds.Distinct())
        {
            var user = await _users.FindByIdAsync(uid);
            if (user is null || !await _users.IsInRoleAsync(user, Roles.PM))
                return BadRequest(new { error = $"L'utente {uid} deve avere ruolo PM." });
        }

        // Reset flag manager, poi imposta sui selezionati (creando membership se assente).
        foreach (var m in unit.Memberships) m.IsManager = false;
        foreach (var uid in dto.UserIds.Distinct())
        {
            var m = unit.Memberships.FirstOrDefault(x => x.UserId == uid);
            if (m is null) unit.Memberships.Add(new UnitMembership { UnitId = id, UserId = uid, IsManager = true });
            else m.IsManager = true;
        }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Aggiunge un membro alla Unit (Admin o PM della Unit).</summary>
    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, UserIdDto dto, CancellationToken ct)
    {
        if (!User.IsAdmin() && !await ManagesAsync(id, ct)) return Forbid();
        if (await _db.Units.FindAsync(new object[] { id }, ct) is null) return NotFound();
        if (await _users.FindByIdAsync(dto.UserId) is null) return BadRequest(new { error = "Utente inesistente." });

        if (!await _db.UnitMemberships.AnyAsync(m => m.UnitId == id && m.UserId == dto.UserId, ct))
        {
            _db.UnitMemberships.Add(new UnitMembership { UnitId = id, UserId = dto.UserId, IsManager = false });
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    [HttpDelete("{id:int}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, string userId, CancellationToken ct)
    {
        if (!User.IsAdmin() && !await ManagesAsync(id, ct)) return Forbid();
        var m = await _db.UnitMemberships.FirstOrDefaultAsync(x => x.UnitId == id && x.UserId == userId && !x.IsManager, ct);
        if (m is not null) { _db.UnitMemberships.Remove(m); await _db.SaveChangesAsync(ct); }
        return NoContent();
    }
}
