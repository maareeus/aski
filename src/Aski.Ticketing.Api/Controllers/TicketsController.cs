using Aski.Ticketing.Api.Auth;
using Aski.Ticketing.Api.Data;
using Aski.Ticketing.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Ticketing.Api.Controllers;

/// <summary>
/// Gestione ticket con autorizzazione per ruolo:
///   Admin  -> vede e gestisce tutto.
///   Dev    -> vede/lavora i ticket delle aziende/software a lui assegnati (o assegnati a lui).
///   Client -> vede solo la propria azienda, apre ticket e può chiudere i propri.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly TicketingDbContext _db;

    public TicketsController(TicketingDbContext db) => _db = db;

    public record CreateTicketDto(string Title, string? Description, int? SoftwareId, TicketPriority Priority, int? CompanyId);
    public record ChangeStatusDto(TicketStatus Status, int? AssignedDevUserId);
    public record AddCommentDto(string Body, bool IsInternal);

    // --- lettura ---

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var query = await ApplyScopeAsync(_db.Tickets.AsNoTracking(), ct);
        var items = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Select(t => new
            {
                t.Id, t.Title, t.Status, t.Priority,
                t.CompanyId, t.SoftwareId, t.AssignedDevUserId,
                t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var ticket = await _db.Tickets.AsNoTracking()
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return NotFound();
        if (!await CanAccessAsync(ticket, ct)) return Forbid();

        var isClient = User.Role() == TicketRole.Client;
        return Ok(new
        {
            ticket.Id, ticket.Title, ticket.Description, ticket.Status, ticket.Priority,
            ticket.CompanyId, ticket.SoftwareId, ticket.CreatedByUserId, ticket.AssignedDevUserId,
            ticket.CreatedAtUtc, ticket.UpdatedAtUtc, ticket.ClosedAtUtc,
            // I Client non vedono i commenti interni.
            Comments = ticket.Comments
                .Where(c => !isClient || !c.IsInternal)
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new { c.Id, c.Body, c.IsInternal, c.AuthorUserId, c.CreatedAtUtc })
        });
    }

    // --- apertura ---

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketDto dto, CancellationToken ct)
    {
        var role = User.Role();
        int companyId;

        if (role == TicketRole.Client)
        {
            // Il client apre sempre e solo per la propria azienda.
            companyId = User.CompanyId() ?? 0;
            if (companyId == 0) return BadRequest("Utente client senza azienda associata");
        }
        else if (role == TicketRole.Admin)
        {
            if (dto.CompanyId is null) return BadRequest("CompanyId obbligatorio per Admin");
            companyId = dto.CompanyId.Value;
        }
        else
        {
            // I Dev non aprono ticket per conto dei clienti.
            return Forbid();
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct))
            return BadRequest("Azienda inesistente");

        var ticket = new Ticket
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            CompanyId = companyId,
            SoftwareId = dto.SoftwareId,
            CreatedByUserId = User.Id(),
            Status = TicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, new { ticket.Id });
    }

    // --- lavorazione (Dev/Admin) ---

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Dev")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto dto, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return NotFound();
        if (!await CanAccessAsync(ticket, ct)) return Forbid();

        ticket.Status = dto.Status;
        if (dto.AssignedDevUserId is not null) ticket.AssignedDevUserId = dto.AssignedDevUserId;
        if (dto.Status == TicketStatus.Closed) ticket.ClosedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ticket.Id, ticket.Status, ticket.AssignedDevUserId });
    }

    // --- chiusura autonoma (Client sul proprio ticket; anche Admin/Dev) ---

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return NotFound();

        var role = User.Role();
        if (role == TicketRole.Client)
        {
            // Un client può chiudere solo ticket della propria azienda.
            if (ticket.CompanyId != User.CompanyId()) return Forbid();
        }
        else if (!await CanAccessAsync(ticket, ct))
        {
            return Forbid();
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { ticket.Id, ticket.Status });
    }

    // --- commenti ---

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, AddCommentDto dto, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return NotFound();
        if (!await CanAccessAsync(ticket, ct)) return Forbid();

        // Solo Dev/Admin possono creare note interne.
        var isInternal = dto.IsInternal && User.Role() != TicketRole.Client;

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            AuthorUserId = User.Id(),
            Body = dto.Body,
            IsInternal = isInternal,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.TicketComments.Add(comment);

        // Un commento del client riapre implicitamente un ticket risolto.
        if (User.Role() == TicketRole.Client && ticket.Status == TicketStatus.Resolved)
            ticket.Status = TicketStatus.Open;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, new { comment.Id });
    }

    // --- autorizzazione/scope ---

    /// <summary>Applica il filtro di visibilità alla query in base al ruolo.</summary>
    private async Task<IQueryable<Ticket>> ApplyScopeAsync(IQueryable<Ticket> query, CancellationToken ct)
    {
        var role = User.Role();
        if (role == TicketRole.Admin)
            return query;

        if (role == TicketRole.Client)
        {
            var companyId = User.CompanyId() ?? 0;
            return query.Where(t => t.CompanyId == companyId);
        }

        // Dev: ambito definito dalle assegnazioni.
        var userId = User.Id();
        var assignments = await _db.DevAssignments.AsNoTracking()
            .Where(a => a.UserId == userId).ToListAsync(ct);

        // Un'assegnazione senza vincoli (company e software nulli) = accesso totale.
        if (assignments.Any(a => a.CompanyId is null && a.SoftwareId is null))
            return query;

        var companyIds = assignments.Where(a => a.CompanyId is not null).Select(a => a.CompanyId!.Value).ToHashSet();
        var softwareIds = assignments.Where(a => a.SoftwareId is not null).Select(a => a.SoftwareId!.Value).ToHashSet();

        return query.Where(t =>
            t.AssignedDevUserId == userId
            || companyIds.Contains(t.CompanyId)
            || (t.SoftwareId != null && softwareIds.Contains(t.SoftwareId.Value)));
    }

    /// <summary>True se l'utente corrente può accedere allo specifico ticket.</summary>
    private async Task<bool> CanAccessAsync(Ticket ticket, CancellationToken ct)
    {
        var role = User.Role();
        if (role == TicketRole.Admin) return true;
        if (role == TicketRole.Client) return ticket.CompanyId == User.CompanyId();

        var userId = User.Id();
        if (ticket.AssignedDevUserId == userId) return true;

        var assignments = await _db.DevAssignments.AsNoTracking()
            .Where(a => a.UserId == userId).ToListAsync(ct);
        if (assignments.Any(a => a.CompanyId is null && a.SoftwareId is null)) return true;

        return assignments.Any(a =>
            (a.CompanyId is not null && a.CompanyId == ticket.CompanyId)
            || (a.SoftwareId is not null && a.SoftwareId == ticket.SoftwareId));
    }
}
