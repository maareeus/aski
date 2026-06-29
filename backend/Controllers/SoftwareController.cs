using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Gestione software assistiti. Lettura: tutti gli utenti autenticati (serve per
/// aprire i ticket). Scrittura: solo Admin.
/// </summary>
[ApiController]
[Route("api/software")]
[Authorize]
public sealed class SoftwareController : ControllerBase
{
    private readonly AppDbContext _db;
    public SoftwareController(AppDbContext db) => _db = db;

    public record SoftwareDto(string Name, string? Description, string? Version);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.Software.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Description, s.Version })
            .ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(SoftwareDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        var s = new SoftwareProduct { Name = dto.Name.Trim(), Description = dto.Description, Version = dto.Version };
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
        s.Name = dto.Name.Trim();
        s.Description = dto.Description;
        s.Version = dto.Version;
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
