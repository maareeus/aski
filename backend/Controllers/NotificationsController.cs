using Aski.Tickets.Api.Auth;
using Aski.Tickets.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>Notifiche dell'utente loggato (campanella).</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int take = 30, CancellationToken ct = default)
    {
        var uid = User.Id();
        var q = _db.Notifications.AsNoTracking().Where(n => n.UserId == uid);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        var items = await q.OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(n => new
            {
                n.Id, n.TicketId, n.Type, n.Message, n.IsRead, n.CreatedAtUtc,
                Number = n.Ticket.Number
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var uid = User.Id();
        var count = await _db.Notifications.CountAsync(n => n.UserId == uid && !n.IsRead, ct);
        return Ok(new { count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var uid = User.Id();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (n is null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var uid = User.Id();
        await _db.Notifications.Where(n => n.UserId == uid && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
        return NoContent();
    }
}
