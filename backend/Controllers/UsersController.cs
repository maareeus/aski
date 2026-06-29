using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>Gestione utenti (solo Admin): crea Agent/Client, attiva/disattiva, cambia ruolo.</summary>
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

    public record CreateUserDto(string Email, string Password, string Role, string? FullName, int? CompanyId);
    public record SetRoleDto(string Role);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.FullName, u.CompanyId, u.IsActive })
            .ToListAsync(ct);

        // Allega i ruoli (lookup tramite Identity).
        var result = new List<object>();
        foreach (var u in users)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            var roles = appUser is null ? Array.Empty<string>() : (await _users.GetRolesAsync(appUser)).ToArray();
            result.Add(new { u.Id, u.Email, u.FullName, u.CompanyId, u.IsActive, Roles = roles });
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
            FullName = dto.FullName,
            CompanyId = dto.Role == Roles.Client ? dto.CompanyId : null,
            IsActive = true
        };
        var created = await _users.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });

        await _users.AddToRoleAsync(user, dto.Role);
        return CreatedAtAction(nameof(List), new { id = user.Id }, new { user.Id });
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
        if (!Roles.All.Contains(dto.Role))
            return BadRequest(new { error = "Ruolo non valido." });
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var current = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, current);
        await _users.AddToRoleAsync(user, dto.Role);
        return NoContent();
    }
}
