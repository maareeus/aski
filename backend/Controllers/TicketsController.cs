using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Ticket di assistenza. Assegnazione singola (utente+unit). Numero TXXXXX.
/// Visibilità: Admin tutto; staff = assegnato a sé, o (PM) della unit che gestisce,
/// o software in carico; Client = propria azienda.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public TicketsController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    public record CreateTicketDto(string Title, string? Description, int? SoftwareId, int? SoftwareVersionId, TicketPriority Priority, int? CompanyId);
    public record ChangeStatusDto(TicketStatus Status);
    public record AssignDto(string UserId, int UnitId);
    public record TakeDto(int? UnitId);
    public record AddCommentDto(string Body, bool IsInternal);

    // --- lettura (filtri + paginazione) ---

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] TicketStatus? status, [FromQuery] TicketPriority? priority,
        [FromQuery] int? companyId, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = await ScopedAsync(_db.Tickets.AsNoTracking(), ct);

        if (status is not null) query = query.Where(t => t.Status == status);
        if (priority is not null) query = query.Where(t => t.Priority == priority);
        if (companyId is not null) query = query.Where(t => t.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Title.Contains(q) || (t.Number != null && t.Number.Contains(q)));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.UpdatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new
            {
                t.Id, t.Number, t.Title, t.Status, t.Priority,
                t.CompanyId, CompanyName = t.Company.Name,
                t.SoftwareId, SoftwareName = t.Software != null ? t.Software.Name : null,
                t.AssignedUserId,
                AssignedUserName = t.AssignedUser != null
                    ? ((t.AssignedUser.FirstName ?? "") + " " + (t.AssignedUser.LastName ?? ""))
                    : null,
                AssignedUnitName = t.AssignedUnit != null ? t.AssignedUnit.Name : null,
                t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc
            }).ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var t = await _db.Tickets.AsNoTracking()
            .Include(x => x.Comments).ThenInclude(c => c.AuthorUser)
            .Include(x => x.Attachments)
            .Include(x => x.Company)
            .Include(x => x.AssignedUser)
            .Include(x => x.AssignedUnit)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();

        var isClient = !User.IsStaff();
        return Ok(new
        {
            t.Id, t.Number, t.Title, t.Description, t.Status, t.Priority,
            t.CompanyId, CompanyName = t.Company.Name,
            t.SoftwareId, t.SoftwareVersionId, t.CreatedByUserId,
            t.AssignedUserId,
            AssignedUserName = t.AssignedUser != null ? ((t.AssignedUser.FirstName ?? "") + " " + (t.AssignedUser.LastName ?? "")).Trim() : null,
            AssignedUserEmail = t.AssignedUser != null ? t.AssignedUser.Email : null,
            t.AssignedUnitId, AssignedUnitName = t.AssignedUnit != null ? t.AssignedUnit.Name : null,
            t.CreatedAtUtc, t.UpdatedAtUtc, t.ClosedAtUtc,
            Attachments = t.Attachments.OrderBy(a => a.CreatedAtUtc).Select(a => new { a.Id, a.FileName, a.ContentType, a.Size, a.CreatedAtUtc }),
            Comments = t.Comments.Where(c => !isClient || !c.IsInternal).OrderBy(c => c.CreatedAtUtc).Select(c => new
            {
                c.Id, c.Body, c.IsInternal, c.AuthorUserId, c.CreatedAtUtc,
                AuthorFirst = c.AuthorUser.FirstName, AuthorLast = c.AuthorUser.LastName,
                AuthorEmail = c.AuthorUser.Email, AuthorIsStaff = c.AuthorUser.CompanyId == null
            })
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
        else if (User.IsStaff())
        {
            if (dto.CompanyId is null) return BadRequest(new { error = "Azienda obbligatoria." });
            companyId = dto.CompanyId.Value;
        }
        else return Forbid();

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, ct)) return BadRequest(new { error = "Azienda inesistente." });
        // La versione, se indicata, deve essere attiva (non obsoleta).
        if (dto.SoftwareVersionId is not null &&
            !await _db.SoftwareVersions.AnyAsync(v => v.Id == dto.SoftwareVersionId && v.IsActive, ct))
            return BadRequest(new { error = "Versione non valida o obsoleta." });

        var ticket = new Ticket
        {
            Title = dto.Title.Trim(), Description = dto.Description, Priority = dto.Priority,
            CompanyId = companyId, SoftwareId = dto.SoftwareId, SoftwareVersionId = dto.SoftwareVersionId,
            CreatedByUserId = User.Id(), Status = TicketStatus.Open
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        ticket.Number = $"T{ticket.Id:D5}";
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, new { ticket.Id, ticket.Number });
    }

    // --- assegnazione (singola) ---

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin,PM")]
    public async Task<IActionResult> Assign(int id, AssignDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!User.IsAdmin() &&
            !await _db.UnitMemberships.AnyAsync(m => m.UnitId == dto.UnitId && m.UserId == User.Id() && m.IsManager, ct))
            return Forbid();
        if (!await _db.UnitMemberships.AnyAsync(m => m.UnitId == dto.UnitId && m.UserId == dto.UserId, ct))
            return BadRequest(new { error = "L'utente non appartiene alla Unit." });

        t.AssignedUserId = dto.UserId; t.AssignedUnitId = dto.UnitId;
        if (t.Status == TicketStatus.Open) t.Status = TicketStatus.InProgress;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.AssignedUserId, t.AssignedUnitId, t.Status });
    }

    [HttpPost("{id:int}/take")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> Take(int id, TakeDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();

        var uid = User.Id();
        var myUnits = await _db.UnitMemberships.Where(m => m.UserId == uid).Select(m => m.UnitId).ToListAsync(ct);
        int? unitId = dto.UnitId;
        if (unitId is null)
        {
            if (myUnits.Count > 1) return BadRequest(new { needUnit = true, units = myUnits });
            unitId = myUnits.Count == 1 ? myUnits[0] : (int?)null;
        }
        else if (!myUnits.Contains(unitId.Value)) return BadRequest(new { error = "Non appartieni alla Unit." });

        t.AssignedUserId = uid; t.AssignedUnitId = unitId;
        if (t.Status == TicketStatus.Open) t.Status = TicketStatus.InProgress;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.AssignedUserId, t.AssignedUnitId, t.Status });
    }

    [HttpPost("{id:int}/unassign")]
    [Authorize(Roles = "Admin,PM")]
    public async Task<IActionResult> Unassign(int id, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        t.AssignedUserId = null; t.AssignedUnitId = null;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // --- stato / chiusura / commenti ---

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,PM,Agent")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
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
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        t.Status = TicketStatus.Closed; t.ClosedAtUtc = DateTime.UtcNow; t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { t.Id, t.Status });
    }

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, AddCommentDto dto, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Body)) return BadRequest(new { error = "Testo obbligatorio." });

        var isClient = !User.IsStaff();
        _db.TicketComments.Add(new TicketComment { TicketId = t.Id, AuthorUserId = User.Id(), Body = dto.Body, IsInternal = dto.IsInternal && !isClient });
        if (isClient && t.Status == TicketStatus.Resolved) t.Status = TicketStatus.Open;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { });
    }

    // --- allegati ---

    [HttpPost("{id:int}/attachments")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(int id, [FromForm] IFormFile file, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new { error = "File mancante." });

        var dir = Path.Combine(_env.ContentRootPath, "uploads", id.ToString());
        Directory.CreateDirectory(dir);
        var safe = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
        var full = Path.Combine(dir, safe);
        await using (var fs = System.IO.File.Create(full)) await file.CopyToAsync(fs, ct);

        var att = new TicketAttachment
        {
            TicketId = id, FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Size = file.Length, StoredPath = Path.Combine(id.ToString(), safe), UploadedByUserId = User.Id()
        };
        _db.TicketAttachments.Add(att);
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { att.Id, att.FileName });
    }

    [HttpGet("{id:int}/attachments/{attId:int}")]
    public async Task<IActionResult> Download(int id, int attId, CancellationToken ct)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (!await CanAccessAsync(t, ct)) return Forbid();
        var att = await _db.TicketAttachments.FirstOrDefaultAsync(a => a.Id == attId && a.TicketId == id, ct);
        if (att is null) return NotFound();
        var full = Path.Combine(_env.ContentRootPath, "uploads", att.StoredPath);
        if (!System.IO.File.Exists(full)) return NotFound();
        return PhysicalFile(full, att.ContentType, att.FileName);
    }

    // --- helper ---

    private async Task<IQueryable<Ticket>> ScopedAsync(IQueryable<Ticket> q, CancellationToken ct)
    {
        if (User.IsAdmin()) return q;
        if (User.IsStaff())
        {
            var uid = User.Id();
            var swIds = await AgentSoftwareIdsAsync(uid, ct);
            var managedUnits = await _db.UnitMemberships.Where(m => m.UserId == uid && m.IsManager).Select(m => m.UnitId).ToListAsync(ct);
            return q.Where(t => t.AssignedUserId == uid
                             || (t.AssignedUnitId != null && managedUnits.Contains(t.AssignedUnitId.Value))
                             || (t.SoftwareId != null && swIds.Contains(t.SoftwareId.Value)));
        }
        var companyId = User.CompanyId() ?? 0;
        return q.Where(t => t.CompanyId == companyId);
    }

    private async Task<bool> CanAccessAsync(Ticket t, CancellationToken ct)
    {
        if (User.IsAdmin()) return true;
        if (User.IsStaff())
        {
            var uid = User.Id();
            if (t.AssignedUserId == uid) return true;
            if (t.AssignedUnitId is not null &&
                await _db.UnitMemberships.AnyAsync(m => m.UnitId == t.AssignedUnitId && m.UserId == uid && m.IsManager, ct))
                return true;
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
}
