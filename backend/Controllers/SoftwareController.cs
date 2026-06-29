using System.Security.Claims;
using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Gestione software (versionati). Scrittura: Admin. Lettura filtrata per ruolo:
///   Admin -> tutti; Agent -> i software a lui assegnati; Client -> i software della sua azienda.
/// </summary>
[ApiController]
[Route("api/software")]
[Authorize]
public sealed class SoftwareController : ControllerBase
{
    private readonly AppDbContext _db;
    public SoftwareController(AppDbContext db) => _db = db;

    public record SoftwareDto(string Name, string Version, string? Description);

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

        var items = await q.OrderBy(s => s.Name).ThenBy(s => s.Version)
            .Select(s => new { s.Id, s.Name, s.Version, s.Description })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(SoftwareDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        if (string.IsNullOrWhiteSpace(dto.Version)) return BadRequest(new { error = "Versione obbligatoria." });
        var s = new SoftwareProduct { Name = dto.Name.Trim(), Version = dto.Version.Trim(), Description = dto.Description };
        _db.Software.Add(s);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = s.Id }, new { s.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, SoftwareDto dto, CancellationToken ct)
    {
        var s = await _db.Software.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Version)) return BadRequest(new { error = "Versione obbligatoria." });
        s.Name = dto.Name.Trim();
        s.Version = dto.Version.Trim();
        s.Description = dto.Description;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/active/{enabled:bool}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SetActive(int id, bool enabled, CancellationToken ct)
    {
        var s = await _db.Software.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        s.IsActive = enabled;
        await _db.SaveChangesAsync(ct);
        return Ok(new { s.Id, s.IsActive });
    }
}
