using System.Security.Claims;
using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Services.Audit;

/// <summary>Registra le operazioni sensibili nel registro di audit.</summary>
public interface IAuditLogger
{
    Task LogAsync(string action, string? target = null, string? details = null, CancellationToken ct = default);
}

/// <inheritdoc cref="IAuditLogger"/>
public sealed class AuditLogger : IAuditLogger
{
    private readonly ControlPlaneDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(ControlPlaneDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string? target = null, string? details = null, CancellationToken ct = default)
    {
        var user = _http.HttpContext?.User;
        int? actorId = int.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        var email = user?.FindFirstValue(ClaimTypes.Email);
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            ActorEmail = email,
            Action = action,
            Target = target,
            Details = details,
            IpAddress = ip,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
