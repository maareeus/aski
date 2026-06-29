using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Gestione utenti (solo Admin). "Utenti" = staff (Admin/PM/Agent); i Client si
/// gestiscono come "Clienti" (endpoint dedicato) perché legati a un'azienda.
/// </summary>
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
        string? FirstName, string? LastName, string? JobTitle, string? Phone,
        int? CompanyId, List<int>? SoftwareIds);
    public record UpdateUserDto(string? FirstName, string? LastName, string? JobTitle, string? Phone, int? CompanyId);
    public record SetRoleDto(string Role);
    public record SoftwareIdsDto(List<int> SoftwareIds);

    /// <summary>Solo staff (esclude i Client).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await _db.Users.AsNoTracking().OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone, u.CompanyId, u.IsActive,
                SoftwareIds = u.Softwares.Select(s => s.Id).ToList() }).ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in rows)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            var roles = appUser is null ? Array.Empty<string>() : (await _users.GetRolesAsync(appUser)).ToArray();
            if (roles.Contains(Roles.Client)) continue; // esclude i client
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone, u.CompanyId, u.IsActive, u.SoftwareIds, Roles = roles });
        }
        return Ok(result);
    }

    /// <summary>Solo Client, con azienda.</summary>
    [HttpGet("clients")]
    public async Task<IActionResult> Clients(CancellationToken ct)
    {
        var rows = await _db.Users.AsNoTracking().Where(u => u.CompanyId != null).OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone, u.CompanyId, u.IsActive,
                CompanyName = u.Company!.Name, SoftwareIds = new List<int>() }).ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in rows)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            var roles = appUser is null ? Array.Empty<string>() : (await _users.GetRolesAsync(appUser)).ToArray();
            if (roles.Contains(Roles.Client))
                result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone, u.CompanyId, u.CompanyName, u.IsActive, Roles = roles });
        }
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Email, x.FirstName, x.LastName, x.JobTitle, x.Phone, x.CompanyId, x.IsActive,
                SoftwareIds = x.Softwares.Select(s => s.Id).ToList() }).FirstOrDefaultAsync(ct);
        if (u is null) return NotFound();
        var appUser = await _users.FindByIdAsync(id);
        var roles = (await _users.GetRolesAsync(appUser!)).ToArray();
        return Ok(new { u.Id, u.Email, u.FirstName, u.LastName, u.JobTitle, u.Phone, u.CompanyId, u.IsActive, u.SoftwareIds, Roles = roles });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto, CancellationToken ct)
    {
        if (!Roles.All.Contains(dto.Role))
            return BadRequest(new { error = $"Ruolo non valido. Ammessi: {string.Join(", ", Roles.All)}." });
        if (dto.Role == Roles.Client && dto.CompanyId is null)
            return BadRequest(new { error = "Azienda obbligatoria per i Client." });
        if (dto.CompanyId is not null && !await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId, ct))
            return BadRequest(new { error = "Azienda inesistente." });

        var user = new AppUser
        {
            UserName = dto.Email, Email = dto.Email, EmailConfirmed = true,
            FirstName = dto.FirstName, LastName = dto.LastName, JobTitle = dto.JobTitle, Phone = dto.Phone,
            CompanyId = dto.Role == Roles.Client ? dto.CompanyId : null, IsActive = true
        };
        var created = await _users.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });

        await _users.AddToRoleAsync(user, dto.Role);
        if (dto.SoftwareIds is { Count: > 0 }) await SetSoftwareInternal(user.Id, dto.SoftwareIds, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, new { user.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.FirstName = dto.FirstName; user.LastName = dto.LastName; user.JobTitle = dto.JobTitle; user.Phone = dto.Phone;
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

    public record ResetPwDto(string NewPassword);

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, ResetPwDto dto, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return BadRequest(new { error = "Password minimo 8 caratteri." });
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var res = await _users.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!res.Succeeded) return BadRequest(new { errors = res.Errors.Select(e => e.Description) });
        return NoContent();
    }

    /// <summary>Unit a cui appartiene l'utente.</summary>
    [HttpGet("{id}/units")]
    public async Task<IActionResult> Units(string id, CancellationToken ct) =>
        Ok(await _db.UnitMemberships.AsNoTracking().Where(m => m.UserId == id)
            .Select(m => new { m.UnitId, m.Unit.Name, m.IsManager }).ToListAsync(ct));

    /// <summary>Ticket assegnati all'utente.</summary>
    [HttpGet("{id}/tickets")]
    public async Task<IActionResult> AssignedTickets(string id, CancellationToken ct) =>
        Ok(await _db.Tickets.AsNoTracking().Where(t => t.AssignedUserId == id)
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Select(t => new { t.Id, t.Number, t.Title, t.Status, t.Priority, CompanyName = t.Company.Name }).ToListAsync(ct));

    /// <summary>Ticket che l'utente può vedere (stessa regola di visibilità).</summary>
    [HttpGet("{id}/visible-tickets")]
    public async Task<IActionResult> VisibleTickets(string id, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await _users.GetRolesAsync(user);

        IQueryable<Ticket> q = _db.Tickets.AsNoTracking();
        if (roles.Contains(Roles.Admin)) { /* tutti */ }
        else if (roles.Contains(Roles.Client))
            q = q.Where(t => t.CompanyId == user.CompanyId);
        else
        {
            var swIds = await _db.Users.Where(u => u.Id == id).SelectMany(u => u.Softwares.Select(s => s.Id)).ToListAsync(ct);
            var managed = await _db.UnitMemberships.Where(m => m.UserId == id && m.IsManager).Select(m => m.UnitId).ToListAsync(ct);
            q = q.Where(t => t.AssignedUserId == id
                          || (t.AssignedUnitId != null && managed.Contains(t.AssignedUnitId.Value))
                          || (t.SoftwareId != null && swIds.Contains(t.SoftwareId.Value)));
        }
        return Ok(await q.OrderByDescending(t => t.UpdatedAtUtc)
            .Select(t => new { t.Id, t.Number, t.Title, t.Status, CompanyName = t.Company.Name }).ToListAsync(ct));
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
