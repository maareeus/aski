using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Provisioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Controllers;

/// <summary>
/// Customer Portal lato tenant: registrazione azienda e gestione progetti
/// (istanze di ticketing). La scelta del server/regione avviene tra quelli
/// abilitati nel Super Admin; l'assegnazione del container Postgres è automatica.
/// </summary>
[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ControlPlaneDbContext _db;
    private readonly IProvisioningCoordinator _coordinator;

    public TenantsController(ControlPlaneDbContext db, IProvisioningCoordinator coordinator)
    {
        _db = db;
        _coordinator = coordinator;
    }

    public record CreateTenantDto(string CompanyName, string BillingEmail);
    public record CreateProjectDto(string Name, int ServerId, string? Subdomain, string? CustomDomain, int? SubscriptionId);

    /// <summary>Registra una nuova azienda (tenant).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantDto dto, CancellationToken ct)
    {
        var tenant = new Tenant
        {
            CompanyName = dto.CompanyName,
            BillingEmail = dto.BillingEmail,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = tenant.Id }, new { tenant.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .Include(t => t.Projects)
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>Crea un progetto (istanza ticketing) per il tenant su un server abilitato.</summary>
    [HttpPost("{id:int}/projects")]
    public async Task<IActionResult> CreateProject(int id, CreateProjectDto dto, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return NotFound("Tenant non trovato");

        var server = await _db.Servers.FirstOrDefaultAsync(s => s.Id == dto.ServerId && s.IsEnabled, ct);
        if (server is null) return BadRequest("Server non valido o non abilitato");

        var project = new Project
        {
            TenantId = tenant.Id,
            Name = dto.Name,
            ServerId = server.Id,
            Subdomain = dto.Subdomain,
            CustomDomain = dto.CustomDomain,
            SubscriptionId = dto.SubscriptionId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = tenant.Id }, new { project.Id });
    }

    /// <summary>
    /// Trigger manuale di provisioning (utile per test senza attendere il webhook).
    /// In produzione il provisioning è pilotato dagli eventi Stripe.
    /// </summary>
    [HttpPost("projects/{projectId:int}/provision")]
    public async Task<IActionResult> Provision(int projectId, CancellationToken ct)
    {
        await _coordinator.ProvisionAndStartAsync(projectId, ct);
        return Accepted();
    }
}
