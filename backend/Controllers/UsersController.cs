using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>Gestione utenti (solo Admin): anagrafica, ruoli, software assegnati.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;

    public UsersController(UserManager<AppUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
    }

    public record CreateUserDto(
        string Email, string Password, string Role,
        string? FirstName, string? LastName, string? Phone,
        int? CompanyId, List<int>? SoftwareIds);
    public record UpdateUserDto(string? FirstName, string? LastName, string? Phone, int? CompanyId);
    public record SetRoleDto(string Role);
    public record SoftwareIdsDto(List<int> SoftwareIds);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id, u.Email, u.FirstName, u.LastName, u.Phone, u.CompanyId, u.IsActive,
                SoftwareIds = u.Softwares.Select(s => s.Id).ToList()
            })
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in rows)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            var roles = appUser is null ? Array.Empty<string>() : (await _users.GetRolesAsync(appUser)).ToArray();
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, u.Phone, u.CompanyId, u.IsActive, u.SoftwareIds, Roles = roles });
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto, CancellationToken ct)
    {
        if (!Roles.All.Contains(dto.Role))
            return BadRequest(new { error = $"Ruolo non valido. Ammessi: {string.Join(", ", Roles.All)}." });
        if (dto.Role == Roles.Client && dto.CompanyId is null)
            return BadRequest(new { error = "CompanyId obbligatorio per i Client." });
        if (dto.CompanyId is not null && !await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId, ct))
            return BadRequest(new { error = "Azienda inesistente." });

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            CompanyId = dto.Role == Roles.Client ? dto.CompanyId : null,
            IsActive = true
        };
        var created = await _users.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });

        await _users.AddToRoleAsync(user, dto.Role);

        if (dto.SoftwareIds is { Count: > 0 })
            await SetSoftwareInternal(user.Id, dto.SoftwareIds, ct);

        return CreatedAtAction(nameof(List), new { id = user.Id }, new { user.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        if (dto.CompanyId is not null) user.CompanyId = dto.CompanyId;
        await _users.UpdateAsync(user);
        return NoContent();
    }

    [HttpPut("{id}/software")]
    public async Task<IActionResult> SetSoftware(string id, SoftwareIdsDto dto, CancellationToken ct)
    {
        if (await _db.Users.FindAsync(new object[] { id }, ct) is null) return NotFound();
        await SetSoftwareInternal(id, dto.SoftwareIds, ct);
        return Ok(new { id, softwareIds = dto.SoftwareIds });
    }

    [HttpPost("{id}/active/{enabled:bool}")]
    public async Task<IActionResult> SetActive(string id, bool enabled, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.IsActive = enabled;
        await _users.UpdateAsync(user);
        return Ok(new { user.Id, user.IsActive });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> SetRole(string id, SetRoleDto dto, CancellationToken ct)
    {
        if (!Roles.All.Contains(dto.Role)) return BadRequest(new { error = "Ruolo non valido." });
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var current = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, current);
        await _users.AddToRoleAsync(user, dto.Role);
        return NoContent();
    }

    private async Task SetSoftwareInternal(string userId, List<int> softwareIds, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.Softwares).FirstAsync(u => u.Id == userId, ct);
        var software = await _db.Software.Where(s => softwareIds.Contains(s.Id)).ToListAsync(ct);
        user.Softwares.Clear();
        user.Softwares.AddRange(software);
        await _db.SaveChangesAsync(ct);
    }
}
