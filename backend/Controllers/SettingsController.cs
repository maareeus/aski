using Aski.Tickets.Api.Data;
using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Controllers;

/// <summary>
/// Impostazioni di brand (nome, logo, favicon). Lettura pubblica (servono prima
/// del login e per la favicon); modifica solo Admin.
/// </summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SettingsController(AppDbContext db) => _db = db;

    public record BrandDto(string BrandName);

    private async Task<AppSetting> GetOrCreateAsync(CancellationToken ct)
    {
        var s = await _db.AppSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null)
        {
            s = new AppSetting { Id = 1, BrandName = "Aski" };
            _db.AppSettings.Add(s);
            await _db.SaveChangesAsync(ct);
        }
        return s;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var s = await GetOrCreateAsync(ct);
        return Ok(new
        {
            s.BrandName,
            HasLogo = s.LogoData != null,
            HasFavicon = s.FaviconData != null,
            Version = s.UpdatedAtUtc.Ticks
        });
    }

    [HttpGet("logo")]
    [AllowAnonymous]
    public async Task<IActionResult> Logo(CancellationToken ct)
    {
        var s = await _db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s?.LogoData is null) return NotFound();
        return File(s.LogoData, s.LogoContentType ?? "image/png");
    }

    [HttpGet("favicon")]
    [AllowAnonymous]
    public async Task<IActionResult> Favicon(CancellationToken ct)
    {
        var s = await _db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s?.FaviconData is null) return NotFound();
        return File(s.FaviconData, s.FaviconContentType ?? "image/png");
    }

    [HttpPut]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(BrandDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.BrandName)) return BadRequest(new { error = "Nome obbligatorio." });
        var s = await GetOrCreateAsync(ct);
        s.BrandName = dto.BrandName.Trim();
        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("logo")]
    [Authorize(Roles = Roles.Admin)]
    [RequestSizeLimit(5_000_000)]
    public Task<IActionResult> UploadLogo([FromForm] IFormFile file, CancellationToken ct) => SaveImageAsync(file, isLogo: true, ct);

    [HttpPost("favicon")]
    [Authorize(Roles = Roles.Admin)]
    [RequestSizeLimit(2_000_000)]
    public Task<IActionResult> UploadFavicon([FromForm] IFormFile file, CancellationToken ct) => SaveImageAsync(file, isLogo: false, ct);

    [HttpDelete("logo")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteLogo(CancellationToken ct)
    {
        var s = await GetOrCreateAsync(ct);
        s.LogoData = null; s.LogoContentType = null; s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("favicon")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteFavicon(CancellationToken ct)
    {
        var s = await GetOrCreateAsync(ct);
        s.FaviconData = null; s.FaviconContentType = null; s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<IActionResult> SaveImageAsync(IFormFile file, bool isLogo, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "File mancante." });
        if (!file.ContentType.StartsWith("image/")) return BadRequest(new { error = "Formato non valido (serve un'immagine)." });
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var data = ms.ToArray();

        var s = await GetOrCreateAsync(ct);
        if (isLogo) { s.LogoData = data; s.LogoContentType = file.ContentType; }
        else { s.FaviconData = data; s.FaviconContentType = file.ContentType; }
        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { size = data.Length });
    }
}
