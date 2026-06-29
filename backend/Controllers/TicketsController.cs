using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Ticket di assistenza con autorizzazione per ruolo:
///   Admin/Agent (staff) -> vedono e lavorano tutti i ticket;
///   Client -> solo i ticket della propria azienda, può aprirli e chiuderli.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public TicketsController(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public record CreateTicketDto(string Title, string? Description, int? SoftwareId, int? SoftwareVersionId, TicketPriority Priority, int? CompanyId);
    public record ChangeStatusDto(TicketStatus Status);
    public record AssignDto(string AgentUserId);
    public record AddCommentDto(string Body, bool IsInternal);

    // --- lettura ---

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TicketStatus? status, CancellationToken ct)
    {
        var q = _db.Tickets.AsNoTracking().AsQueryable();
        if (User.IsAdmin())
        {
            // tutti
        }
        else if (User.IsAgent())
        {
            // L'Agent vede i ticket dei software che gli sono assegnati (o assegnati direttamente a lui).
            var uid = User.Id();
            var swIds = await AgentSoftwareIdsAsync(uid, ct);
            q = q.Where(t => t.AssignedAgentUserId == uid ||
                             (t.SoftwareId != null && swIds.Contains(t.SoftwareId.Value)));
        }
        else // Client
        {
            q = q.Where(t => t.CompanyId == (User.CompanyId() ?? 0));
        }
        if (status is not null)
            q = q.Where(t => t.Status == status);

        var items = await q.OrderByDescending(t => t.UpdatedAtUtc)
            .Select(t => new
            {
                t.Id, t.Title, t.Status, t.Priority, t.CompanyId, t.SoftwareId,
                t.AssignedAgentUserId, t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var t = await _db.Tickets.AsNoTracking()
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();

        var isClient = User.IsClient() && !User.IsStaff();
        return Ok(new
        {
            t.Id, t.Title, t.Description, t.Status, t.Priority, t.CompanyId, t.SoftwareId, t.SoftwareVersionId,
            t.CreatedByUserId, t.AssignedAgentUserId, t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc,
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
        else
        {
            return Forbid(); // gli Agent non aprono ticket per conto dei clienti
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct))
            return BadRequest(new { error = "Azienda inesistente." });
        if (dto.SoftwareId is not null && !await _db.Software.AnyAsync(s => s.Id == dto.SoftwareId, ct))
            return BadRequest(new { error = "Software inesistente." });

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

    // --- lavorazione (staff) ---

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Agent}")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        t.Status = dto.Status;
        if (dto.Status == TicketStatus.Closed) t.ClosedAtUtc = DateTime.UtcNow;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.Status });
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Agent}")]
    public async Task<IActionResult> Assign(int id, AssignDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        var agent = await _users.FindByIdAsync(dto.AgentUserId);
        if (agent is null) return BadRequest(new { error = "Utente inesistente." });
        var roles = await _users.GetRolesAsync(agent);
        if (!roles.Contains(Roles.Agent) && !roles.Contains(Roles.Admin))
            return BadRequest(new { error = "L'utente assegnato deve essere Agent o Admin." });

        t.AssignedAgentUserId = agent.Id;
        if (t.Status == TicketStatus.Open) t.Status = TicketStatus.InProgress;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.AssignedAgentUserId, t.Status });
    }

    // --- chiusura ---

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();

        t.Status = TicketStatus.Closed;
        t.ClosedAtUtc = DateTime.UtcNow;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.Status });
    }

    // --- commenti ---

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, AddCommentDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Body)) return BadRequest(new { error = "Testo obbligatorio." });

        var isClient = User.IsClient() && !User.IsStaff();
        var comment = new TicketComment
        {
            TicketId = t.Id,
            AuthorUserId = User.Id(),
            Body = dto.Body,
            IsInternal = dto.IsInternal && !isClient // i Client non creano note interne
        };
        _db.TicketComments.Add(comment);

        // Un commento del client su ticket Risolto lo riapre.
        if (isClient && t.Status == TicketStatus.Resolved) t.Status = TicketStatus.Open;
        t.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { comment.Id });
    }

    /// <summary>
    /// Admin: tutto. Agent: ticket dei software assegnati o assegnati a lui.
    /// Client: solo la propria azienda.
    /// </summary>
    private async Task<bool> CanAccessAsync(Ticket t, CancellationToken ct)
    {
        if (User.IsAdmin()) return true;
        if (User.IsAgent())
        {
            var uid = User.Id();
            if (t.AssignedAgentUserId == uid) return true;
            var swIds = await AgentSoftwareIdsAsync(uid, ct);
            return t.SoftwareId is not null && swIds.Contains(t.SoftwareId.Value);
        }
        return t.CompanyId == (User.CompanyId() ?? 0);
    }

    private Task<List<int>> AgentSoftwareIdsAsync(string userId, CancellationToken ct) =>
        _db.Users.Where(u => u.Id == userId).SelectMany(u => u.Softwares.Select(s => s.Id)).ToListAsync(ct);
}
