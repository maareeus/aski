using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Controllers;

/// <summary>
/// Gestione server/regioni del Super Admin: definisce dove i tenant possono
/// provisionare i progetti e il limite N di progetti per container Postgres.
/// </summary>
[ApiController]
[Route("api/admin/servers")]
public sealed class AdminServersController : ControllerBase
{
    private readonly ControlPlaneDbContext _db;

    public AdminServersController(ControlPlaneDbContext db) => _db = db;

    public record ServerDto(
        string Name, string Region, ServerType Type, string? Hostname,
        string? ConfigJson, int MaxProjectsPerDbContainer, bool IsEnabled);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.Servers.AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.Region, s.Type, s.MaxProjectsPerDbContainer, s.IsEnabled })
            .ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(ServerDto dto, CancellationToken ct)
    {
        var server = new Server
        {
            Name = dto.Name,
            Region = dto.Region,
            Type = dto.Type,
            Hostname = dto.Hostname,
            ConfigJson = dto.ConfigJson,
            MaxProjectsPerDbContainer = dto.MaxProjectsPerDbContainer <= 0 ? 10 : dto.MaxProjectsPerDbContainer,
            IsEnabled = dto.IsEnabled,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Servers.Add(server);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = server.Id }, new { server.Id });
    }
}
