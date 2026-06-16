using Aski.ControlPlane.Data;
using Aski.Shared;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Services.Provisioning;

/// <summary>
/// Implementazione placeholder per la Fase 2: aggiorna lo stato di provisioning
/// del progetto e logga l'intenzione, senza toccare container reali.
/// Sostituita in Fase 3 da un coordinatore che usa IInfrastructureProvider.
/// </summary>
public sealed class LoggingProvisioningCoordinator : IProvisioningCoordinator
{
    private readonly ControlPlaneDbContext _db;
    private readonly ILogger<LoggingProvisioningCoordinator> _log;

    public LoggingProvisioningCoordinator(ControlPlaneDbContext db, ILogger<LoggingProvisioningCoordinator> log)
    {
        _db = db;
        _log = log;
    }

    public Task ProvisionAndStartAsync(int projectId, CancellationToken ct = default)
        => SetStatusAsync(projectId, ProvisioningStatus.Running, "PROVISION+START", ct);

    public Task SuspendAsync(int projectId, CancellationToken ct = default)
        => SetStatusAsync(projectId, ProvisioningStatus.Stopped, "SUSPEND", ct);

    public Task ResumeAsync(int projectId, CancellationToken ct = default)
        => SetStatusAsync(projectId, ProvisioningStatus.Running, "RESUME", ct);

    public Task StopAsync(int projectId, CancellationToken ct = default)
        => SetStatusAsync(projectId, ProvisioningStatus.Stopped, "STOP", ct);

    private async Task SetStatusAsync(int projectId, ProvisioningStatus status, string action, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is null)
        {
            _log.LogWarning("[{Action}] progetto {ProjectId} non trovato", action, projectId);
            return;
        }

        _log.LogInformation("[{Action}] progetto {ProjectId} ({Name}) -> {Status} (placeholder Fase 2)",
            action, projectId, project.Name, status);

        project.ProvisioningStatus = status;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
