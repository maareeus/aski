using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>Gestione aziende clienti (solo Admin).</summary>
[ApiController]
[Route("api/companies")]
[Authorize(Roles = Roles.Admin)]
public sealed class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CompaniesController(AppDbContext db) => _db = db;

    public record CompanyDto(string Name, string? ContactEmail);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.ContactEmail, c.IsActive, c.CreatedAtUtc })
            .ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var c = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CompanyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        var c = new Company { Name = dto.Name.Trim(), ContactEmail = dto.ContactEmail };
        _db.Companies.Add(c);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new { c.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CompanyDto dto, CancellationToken ct)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        c.Name = dto.Name.Trim();
        c.ContactEmail = dto.ContactEmail;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/active/{enabled:bool}")]
    public async Task<IActionResult> SetActive(int id, bool enabled, CancellationToken ct)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        c.IsActive = enabled;
        await _db.SaveChangesAsync(ct);
        return Ok(new { c.Id, c.IsActive });
    }
}
