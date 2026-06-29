using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Rubrica di un'azienda. Lettura: staff oppure utenti dell'azienda (anche Client).
/// Scrittura: solo staff (Admin/PM/Agent). I Client non gestiscono la rubrica.
/// </summary>
[ApiController]
[Route("api/companies/{companyId:int}/contacts")]
[Authorize]
public sealed class ContactsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ContactsController(AppDbContext db) => _db = db;

    public record ContactDto(string Name, string? Title, string? Email, string? Phone, string? Notes);

    private bool CanRead(int companyId) => User.IsStaff() || User.CompanyId() == companyId;

    [HttpGet]
    public async Task<IActionResult> List(int companyId, CancellationToken ct)
    {
        if (!CanRead(companyId)) return Forbid();
        return Ok(await _db.Contacts.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Title, c.Email, c.Phone, c.Notes })
            .ToListAsync(ct));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> Create(int companyId, ContactDto dto, CancellationToken ct)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nome obbligatorio." });
        var c = new Contact { CompanyId = companyId, Name = dto.Name.Trim(), Title = dto.Title, Email = dto.Email, Phone = dto.Phone, Notes = dto.Notes };
        _db.Contacts.Add(c);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { companyId }, new { c.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> Update(int companyId, int id, ContactDto dto, CancellationToken ct)
    {
        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
        if (c is null) return NotFound();
        c.Name = dto.Name.Trim(); c.Title = dto.Title; c.Email = dto.Email; c.Phone = dto.Phone; c.Notes = dto.Notes;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> Delete(int companyId, int id, CancellationToken ct)
    {
        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
        if (c is null) return NotFound();
        _db.Contacts.Remove(c);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
