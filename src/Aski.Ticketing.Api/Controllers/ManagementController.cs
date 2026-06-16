using Aski.Ticketing.Api.Data;
using Aski.Ticketing.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Ticketing.Api.Controllers;

/// <summary>
/// Gestione dell'istanza riservata all'Admin: aziende, software, utenti
/// (Admin/Dev/Client) e assegnazioni dei Dev a aziende/software.
/// </summary>
[ApiController]
[Route("api/manage")]
[Authorize(Roles = "Admin")]
public sealed class ManagementController : ControllerBase
{
    private readonly TicketingDbContext _db;

    public ManagementController(TicketingDbContext db) => _db = db;

    // --- Aziende ---

    public record CompanyDto(string Name);

    [HttpGet("companies")]
    public async Task<IActionResult> Companies(CancellationToken ct) =>
        Ok(await _db.Companies.AsNoTracking().Select(c => new { c.Id, c.Name }).ToListAsync(ct));

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany(CompanyDto dto, CancellationToken ct)
    {
        var company = new Company { Name = dto.Name, CreatedAtUtc = DateTime.UtcNow };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        return Ok(new { company.Id });
    }

    // --- Software ---

    public record SoftwareDto(string Name, string? Description);

    [HttpGet("software")]
    public async Task<IActionResult> SoftwareList(CancellationToken ct) =>
        Ok(await _db.Software.AsNoTracking().Select(s => new { s.Id, s.Name, s.Description }).ToListAsync(ct));

    [HttpPost("software")]
    public async Task<IActionResult> CreateSoftware(SoftwareDto dto, CancellationToken ct)
    {
        var sw = new SoftwareProduct { Name = dto.Name, Description = dto.Description, CreatedAtUtc = DateTime.UtcNow };
        _db.Software.Add(sw);
        await _db.SaveChangesAsync(ct);
        return Ok(new { sw.Id });
    }

    // --- Utenti ---

    public record CreateUserDto(string Email, string Password, TicketRole Role, string? FullName, int? CompanyId);

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) =>
        Ok(await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Email, u.Role, u.FullName, u.CompanyId, u.IsActive })
            .ToListAsync(ct));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            return Conflict("Email già registrata");

        // I Client devono avere un'azienda associata.
        if (dto.Role == TicketRole.Client && dto.CompanyId is null)
            return BadRequest("CompanyId obbligatorio per i Client");

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            FullName = dto.FullName,
            CompanyId = dto.CompanyId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return Ok(new { user.Id });
    }

    // --- Assegnazioni Dev ---

    public record AssignmentDto(int UserId, int? CompanyId, int? SoftwareId);

    [HttpPost("assignments")]
    public async Task<IActionResult> Assign(AssignmentDto dto, CancellationToken ct)
    {
        var dev = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
        if (dev is null || dev.Role != TicketRole.Dev) return BadRequest("Utente non valido o non Dev");

        var assignment = new DevAssignment
        {
            UserId = dto.UserId,
            CompanyId = dto.CompanyId,
            SoftwareId = dto.SoftwareId
        };
        _db.DevAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return Ok(new { assignment.Id });
    }
}
