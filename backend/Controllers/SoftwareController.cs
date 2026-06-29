using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Software e relativo storico versioni. Scrittura: Admin. Lettura filtrata per ruolo
/// (Admin: tutti; Agent: assegnati; Client: della propria azienda).
/// </summary>
[ApiController]
[Route("api/software")]
[Authorize]
public sealed class SoftwareController : ControllerBase
{
    private readonly AppDbContext _db;
    public SoftwareController(AppDbContext db) => _db = db;

    public record SoftwareDto(string Name, string? Description);
    public record VersionDto(string Version, string? Notes, DateTime? ReleasedAtUtc);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        IQueryable<SoftwareProduct> q = _db.Software.AsNoTracking().Where(s => s.IsActive);
        if (User.IsClient() && !User.IsStaff())
        {
            var companyId = User.CompanyId() ?? 0;
            q = q.Where(s => s.Companies.Any(c => c.Id == companyId));
        }
        else if (User.IsAgent() && !User.IsAdmin())
        {
            var uid = User.Id();
            q = q.Where(s => s.Users.Any(u => u.Id == uid));
        }

        var items = await q.OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id, s.Name, s.Description,
                VersionsCount = s.Versions.Count,
                LatestVersion = s.Versions.OrderByDescending(v => v.CreatedAtUtc).Select(v => v.Version).FirstOrDefault()
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var s = await _db.Software.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Name, x.Description, x.IsActive })
            .FirstOrDefaultAsync(ct);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(SoftwareDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        var s = new SoftwareProduct { Name = dto.Name.Trim(), Description = dto.Description };
        _db.Software.Add(s);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = s.Id }, new { s.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, SoftwareDto dto, CancellationToken ct)
    {
        var s = await _db.Software.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        s.Name = dto.Name.Trim();
        s.Description = dto.Description;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // --- Versioni (storico) ---

    [HttpGet("{id:int}/versions")]
    public async Task<IActionResult> Versions(int id, CancellationToken ct)
    {
        if (!await _db.Software.AnyAsync(s => s.Id == id, ct)) return NotFound();
        var versions = await _db.SoftwareVersions.AsNoTracking()
            .Where(v => v.SoftwareId == id)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new { v.Id, v.Version, v.Notes, v.ReleasedAtUtc, v.IsActive, v.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(versions);
    }

    [HttpPost("{id:int}/versions")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddVersion(int id, VersionDto dto, CancellationToken ct)
    {
        if (!await _db.Software.AnyAsync(s => s.Id == id, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Version)) return BadRequest(new { error = "Versione obbligatoria." });
        var v = new SoftwareVersion { SoftwareId = id, Version = dto.Version.Trim(), Notes = dto.Notes, ReleasedAtUtc = dto.ReleasedAtUtc };
        _db.SoftwareVersions.Add(v);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Versions), new { id }, new { v.Id });
    }

    [HttpPost("{id:int}/versions/{versionId:int}/active/{enabled:bool}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SetVersionActive(int id, int versionId, bool enabled, CancellationToken ct)
    {
        var v = await _db.SoftwareVersions.FirstOrDefaultAsync(x => x.Id == versionId && x.SoftwareId == id, ct);
        if (v is null) return NotFound();
        v.IsActive = enabled;
        await _db.SaveChangesAsync(ct);
        return Ok(new { v.Id, v.IsActive });
    }
}
