using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>Gestione aziende clienti + software associati (solo Admin).</summary>
[ApiController]
[Route("api/companies")]
[Authorize(Roles = Roles.Admin)]
public sealed class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CompaniesController(AppDbContext db) => _db = db;

    public record CompanyDto(string Name, string? VatNumber, string? ContactEmail, string? Phone, string? Address);
    public record SoftwareIdsDto(List<int> SoftwareIds);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id, c.Name, c.VatNumber, c.ContactEmail, c.Phone, c.Address, c.IsActive, c.CreatedAtUtc,
                UsersCount = c.Users.Count,
                SoftwareIds = c.Softwares.Select(s => s.Id).ToList()
            })
            .ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var c = await _db.Companies.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.Name, x.VatNumber, x.ContactEmail, x.Phone, x.Address, x.IsActive,
                SoftwareIds = x.Softwares.Select(s => s.Id).ToList()
            })
            .FirstOrDefaultAsync(ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CompanyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        var c = new Company
        {
            Name = dto.Name.Trim(), VatNumber = dto.VatNumber, ContactEmail = dto.ContactEmail,
            Phone = dto.Phone, Address = dto.Address
        };
        _db.Companies.Add(c);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new { c.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CompanyDto dto, CancellationToken ct)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        c.Name = dto.Name.Trim(); c.VatNumber = dto.VatNumber; c.ContactEmail = dto.ContactEmail;
        c.Phone = dto.Phone; c.Address = dto.Address;
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

    /// <summary>Utenti (Client) appartenenti all'azienda.</summary>
    [HttpGet("{id:int}/users")]
    public async Task<IActionResult> Users(int id, CancellationToken ct) =>
        Ok(await _db.Users.AsNoTracking()
            .Where(u => u.CompanyId == id)
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.Phone, u.IsActive })
            .ToListAsync(ct));

    /// <summary>Imposta l'elenco dei software associati all'azienda (sostituisce il set).</summary>
    [HttpPut("{id:int}/software")]
    public async Task<IActionResult> SetSoftware(int id, SoftwareIdsDto dto, CancellationToken ct)
    {
        var company = await _db.Companies.Include(c => c.Softwares).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null) return NotFound();

        var software = await _db.Software.Where(s => dto.SoftwareIds.Contains(s.Id)).ToListAsync(ct);
        company.Softwares.Clear();
        company.Softwares.AddRange(software);
        await _db.SaveChangesAsync(ct);
        return Ok(new { id, softwareIds = software.Select(s => s.Id) });
    }
}
