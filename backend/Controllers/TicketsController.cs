using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Ticket di assistenza.
/// Visibilità staff: ticket assegnati via Unit (TicketAssignment), o dei software in carico,
/// o di cui si è assegnatari. Admin tutto. Client solo la propria azienda.
/// Assegnazione: il PM assegna (utente+unit); chi ha visibilità può "prendere in carico"
/// diventando assegnatario (scegliendo la Unit se ne ha più di una).
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TicketsController(AppDbContext db) => _db = db;

    public record CreateTicketDto(string Title, string? Description, int? SoftwareId, int? SoftwareVersionId, TicketPriority Priority, int? CompanyId);
    public record ChangeStatusDto(TicketStatus Status);
    public record AssignDto(string UserId, int UnitId);
    public record TakeDto(int? UnitId);
    public record AddCommentDto(string Body, bool IsInternal);

    // --- lettura ---

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TicketStatus? status, CancellationToken ct)
    {
        var q = _db.Tickets.AsNoTracking().AsQueryable();
        if (User.IsAdmin()) { /* tutti */ }
        else if (User.IsStaff())
        {
            var uid = User.Id();
            var swIds = await AgentSoftwareIdsAsync(uid, ct);
            q = q.Where(t => t.AssigneeUserId == uid
                          || t.Assignments.Any(a => a.UserId == uid)
                          || (t.SoftwareId != null && swIds.Contains(t.SoftwareId.Value)));
        }
        else q = q.Where(t => t.CompanyId == (User.CompanyId() ?? 0));

        if (status is not null) q = q.Where(t => t.Status == status);

        var items = await q.OrderByDescending(t => t.UpdatedAtUtc)
            .Select(t => new
            {
                t.Id, t.Title, t.Status, t.Priority, t.CompanyId, t.SoftwareId, t.SoftwareVersionId,
                t.AssigneeUserId, t.AssigneeUnitId, t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc
            }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var t = await _db.Tickets.AsNoTracking()
            .Include(x => x.Comments)
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();

        var isClient = !User.IsStaff();
        return Ok(new
        {
            t.Id, t.Title, t.Description, t.Status, t.Priority, t.CompanyId, t.SoftwareId, t.SoftwareVersionId,
            t.CreatedByUserId, t.AssigneeUserId, t.AssigneeUnitId, t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc,
            Assignments = t.Assignments.Select(a => new { a.Id, a.UnitId, a.UserId }),
            Comments = t.Comments
                .Where(c => !isClient || !c.IsInternal)
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new { c.Id, c.Body, c.IsInternal, c.AuthorUserId, c.CreatedAtUtc })
        });
    }

    // --- apertura ---

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { error = "Titolo obbligatorio." });

        int companyId;
        if (User.IsClient() && !User.IsStaff())
        {
            companyId = User.CompanyId() ?? 0;
            if (companyId == 0) return BadRequest(new { error = "Utente client senza azienda." });
        }
        else if (User.IsAdmin())
        {
            if (dto.CompanyId is null) return BadRequest(new { error = "CompanyId obbligatorio per Admin." });
            companyId = dto.CompanyId.Value;
        }
        else return Forbid();

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct))
            return BadRequest(new { error = "Azienda inesistente." });

        var ticket = new Ticket
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Priority = dto.Priority,
            CompanyId = companyId,
            SoftwareId = dto.SoftwareId,
            SoftwareVersionId = dto.SoftwareVersionId,
            CreatedByUserId = User.Id(),
            Status = TicketStatus.Open
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, new { ticket.Id });
    }

    // --- assegnazione (PM) ---

    /// <summary>Il PM assegna un utente tramite una sua Unit (dà visibilità).</summary>
    [HttpPost("{id:int}/assignments")]
    [Authorize(Roles = "Admin,PM")]
    public async Task<IActionResult> AddAssignment(int id, AssignDto dto, CancellationToken ct)
    {
        if (!await _db.Tickets.AnyAsync(t => t.Id == id, ct)) return NotFound();

        // PM deve gestire la unit; Admin può sempre.
        if (!User.IsAdmin() &&
            !await _db.UnitMemberships.AnyAsync(m => m.UnitId == dto.UnitId && m.UserId == User.Id() && m.IsManager, ct))
            return Forbid();

        // L'utente assegnato deve essere membro della unit.
        if (!await _db.UnitMemberships.AnyAsync(m => m.UnitId == dto.UnitId && m.UserId == dto.UserId, ct))
            return BadRequest(new { error = "L'utente non appartiene alla Unit." });

        if (!await _db.TicketAssignments.AnyAsync(a => a.TicketId == id && a.UnitId == dto.UnitId && a.UserId == dto.UserId, ct))
        {
            _db.TicketAssignments.Add(new TicketAssignment { TicketId = id, UnitId = dto.UnitId, UserId = dto.UserId });
            await TouchAsync(id, ct);
        }
        return NoContent();
    }

    [HttpDelete("{id:int}/assignments/{assignmentId:int}")]
    [Authorize(Roles = "Admin,PM")]
    public async Task<IActionResult> RemoveAssignment(int id, int assignmentId, CancellationToken ct)
    {
        var a = await _db.TicketAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId && x.TicketId == id, ct);
        if (a is null) return NotFound();
        if (!User.IsAdmin() &&
            !await _db.UnitMemberships.AnyAsync(m => m.UnitId == a.UnitId && m.UserId == User.Id() && m.IsManager, ct))
            return Forbid();
        _db.TicketAssignments.Remove(a);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Prende in carico il ticket: chi ha visibilità diventa assegnatario.
    /// Se l'utente appartiene a più Unit deve indicare con quale gestirlo.
    /// </summary>
    [HttpPost("{id:int}/take")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> Take(int id, TakeDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();

        var uid = User.Id();
        var myUnits = await _db.UnitMemberships.Where(m => m.UserId == uid).Select(m => m.UnitId).ToListAsync(ct);

        int? unitId = dto.UnitId;
        if (unitId is null)
        {
            if (myUnits.Count > 1)
                return BadRequest(new { needUnit = true, units = myUnits, error = "Specifica la Unit con cui gestire il ticket." });
            unitId = myUnits.Count == 1 ? myUnits[0] : (int?)null;
        }
        else if (!myUnits.Contains(unitId.Value))
            return BadRequest(new { error = "Non appartieni alla Unit indicata." });

        t.AssigneeUserId = uid;
        t.AssigneeUnitId = unitId;
        if (t.Status == TicketStatus.Open) t.Status = TicketStatus.InProgress;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.AssigneeUserId, t.AssigneeUnitId, t.Status });
    }

    // --- lavorazione / chiusura / commenti ---

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        t.Status = dto.Status;
        if (dto.Status == TicketStatus.Closed) t.ClosedAtUtc = DateTime.UtcNow;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.Status });
    }

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var t = await _db.Tickets.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        t.Status = TicketStatus.Closed;
        t.ClosedAtUtc = DateTime.UtcNow;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.Status });
    }

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, AddCommentDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Body)) return BadRequest(new { error = "Testo obbligatorio." });

        var isClient = !User.IsStaff();
        _db.TicketComments.Add(new TicketComment
        {
            TicketId = t.Id, AuthorUserId = User.Id(), Body = dto.Body, IsInternal = dto.IsInternal && !isClient
        });
        if (isClient && t.Status == TicketStatus.Resolved) t.Status = TicketStatus.Open;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { });
    }

    // --- helper ---

    private async Task<bool> CanAccessAsync(Ticket t, CancellationToken ct)
    {
        if (User.IsAdmin()) return true;
        if (User.IsStaff())
        {
            var uid = User.Id();
            if (t.AssigneeUserId == uid) return true;
            if (t.Assignments.Any(a => a.UserId == uid)) return true;
            if (t.SoftwareId is not null)
            {
                var swIds = await AgentSoftwareIdsAsync(uid, ct);
                if (swIds.Contains(t.SoftwareId.Value)) return true;
            }
            return false;
        }
        return t.CompanyId == (User.CompanyId() ?? 0);
    }

    private Task<List<int>> AgentSoftwareIdsAsync(string userId, CancellationToken ct) =>
        _db.Users.Where(u => u.Id == userId).SelectMany(u => u.Softwares.Select(s => s.Id)).ToListAsync(ct);

    private async Task TouchAsync(int ticketId, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstAsync(x => x.Id == ticketId, ct);
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
