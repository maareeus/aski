using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Infrastructure;
using Aski.Shared;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Services.Provisioning;

/// <summary>
/// Coordinatore di provisioning reale (Fase 3): traduce le transizioni di stato
/// dell'abbonamento in operazioni sull'infrastruttura tramite
/// <see cref="IInfrastructureProviderFactory"/>.
///
/// Gestisce il pool Postgres: assegna ogni progetto a un container condiviso fino
/// al limite N del server; raggiunto N crea automaticamente un nuovo container.
/// L'allocazione usa il concurrency token (xmin) di <see cref="DbContainer"/> per
/// evitare che provisioning concorrenti superino N sullo stesso container.
///
/// Suspend/Stop fermano solo il container APPLICATIVO del progetto: il container
/// Postgres è condiviso e non va mai fermato per non impattare gli altri progetti.
/// I dati restano sempre intatti.
/// </summary>
public sealed class DockerProvisioningCoordinator : IProvisioningCoordinator
{
    private readonly ControlPlaneDbContext _db;
    private readonly IInfrastructureProviderFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<DockerProvisioningCoordinator> _log;

    private const int MaxAllocationRetries = 5;

    public DockerProvisioningCoordinator(
        ControlPlaneDbContext db,
        IInfrastructureProviderFactory factory,
        IConfiguration config,
        ILogger<DockerProvisioningCoordinator> log)
    {
        _db = db;
        _factory = factory;
        _config = config;
        _log = log;
    }

    public async Task ProvisionAndStartAsync(int projectId, CancellationToken ct = default)
    {
        var project = await LoadProjectAsync(projectId, ct);
        if (project is null) return;

        // Idempotenza: se già in esecuzione e con container, riavvia soltanto.
        if (project.ProvisioningStatus == ProvisioningStatus.Running && project.AppContainerId is not null)
        {
            await ResumeAsync(projectId, ct);
            return;
        }

        if (project.ServerId is null || project.Server is null)
        {
            _log.LogError("Progetto {Id} senza server: impossibile provisionare", projectId);
            await SetStatusAsync(project, ProvisioningStatus.Failed, ct);
            return;
        }

        var server = project.Server;
        var provider = _factory.Create(server);
        var cfg = ServerDockerConfig.From(server);

        try
        {
            await SetStatusAsync(project, ProvisioningStatus.Provisioning, ct);

            // 1. Assegna (o crea) un container Postgres del pool rispettando N.
            var dbContainer = await AllocateDbContainerAsync(server, provider, cfg, ct);

            // 2. Crea il database logico isolato del progetto con un utente dedicato
            //    (l'app NON usa più le credenziali admin del pool).
            //    Le operazioni admin avvengono dal Control Plane (host) via la porta
            //    pubblicata su localhost; i container app useranno il nome di rete.
            var dbName = $"proj_{project.Id}";
            var dbUser = $"proj_{project.Id}_u";
            var dbPassword = project.DbPassword ?? GeneratePassword();
            var adminHost = dbContainer.HostPort > 0 ? "127.0.0.1" : dbContainer.Host!;
            var adminPort = dbContainer.HostPort > 0 ? dbContainer.HostPort : dbContainer.Port;
            var pg = new PostgresEndpoint(adminHost, adminPort, cfg.PgAdminUser, cfg.PgAdminPassword);
            await provider.CreateDatabaseAsync(server, pg, dbName, dbUser, dbPassword, ct);

            // 3. Provisiona il container applicativo con label Traefik.
            //    La connection string dell'app usa il nome del container sulla rete Docker.
            var subdomain = project.Subdomain ?? $"p{project.Id}";
            var primaryHost = $"{subdomain}.{cfg.DomainSuffix}";
            var connString =
                $"Host={dbContainer.Host};Port={dbContainer.Port};Database={dbName};" +
                $"Username={dbUser};Password={dbPassword}";

            var env = new Dictionary<string, string>
            {
                ["ConnectionStrings__Tenant"] = connString,
                ["Aski__ProjectId"] = project.Id.ToString(),
                // Env per far avviare l'istanza ticketing (altrimenti i guard di produzione la bloccano).
                ["ASPNETCORE_ENVIRONMENT"] = _config["TicketingDefaults:Environment"] ?? "Development",
                ["Seed__AdminEmail"] = _config["TicketingDefaults:AdminEmail"] ?? "admin@aski.local",
                ["Seed__AdminPassword"] = _config["TicketingDefaults:AdminPassword"] ?? "ChangeMe123!"
            };
            var jwtKey = _config["TicketingDefaults:JwtKey"];
            if (!string.IsNullOrWhiteSpace(jwtKey))
                env["Jwt__Key"] = jwtKey;

            var request = new AppProvisionRequest(
                ContainerName: $"aski-app-{project.Id}",
                Image: cfg.AppImage,
                PrimaryHost: primaryHost,
                CustomHost: project.CustomDomain,
                InternalPort: 8080,
                Environment: env);

            var app = await provider.ProvisionAppContainerAsync(server, request, ct);

            // 4. Persiste il risultato sul progetto (password cifrata a riposo dall'EF converter).
            project.DbContainerId = dbContainer.Id;
            project.DatabaseName = dbName;
            project.DbUser = dbUser;
            project.DbPassword = dbPassword;
            project.AppContainerId = app.RuntimeContainerId;
            project.Subdomain = subdomain;
            await SetStatusAsync(project, ProvisioningStatus.Running, ct);

            _log.LogInformation("Progetto {Id} provisionato: host={Host}, db={Db}", project.Id, primaryHost, dbName);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Provisioning fallito per il progetto {Id}", projectId);
            await SetStatusAsync(project, ProvisioningStatus.Failed, ct);
            throw;
        }
    }

    public async Task SuspendAsync(int projectId, CancellationToken ct = default)
    {
        var project = await LoadProjectAsync(projectId, ct);
        if (project?.Server is null || project.AppContainerId is null)
        {
            _log.LogWarning("Suspend ignorato per progetto {Id}: nessun container app", projectId);
            return;
        }

        var provider = _factory.Create(project.Server);
        await provider.StopContainerAsync(project.Server, project.AppContainerId, ct);
        await SetStatusAsync(project, ProvisioningStatus.Stopped, ct);
        _log.LogInformation("Progetto {Id} sospeso (container app fermato, dati conservati)", projectId);
    }

    public async Task ResumeAsync(int projectId, CancellationToken ct = default)
    {
        var project = await LoadProjectAsync(projectId, ct);
        if (project?.Server is null) return;

        // Se non è mai stato provisionato, esegui il provisioning completo.
        if (project.AppContainerId is null)
        {
            await ProvisionAndStartAsync(projectId, ct);
            return;
        }

        var provider = _factory.Create(project.Server);
        await provider.StartContainerAsync(project.Server, project.AppContainerId, ct);
        await SetStatusAsync(project, ProvisioningStatus.Running, ct);
        _log.LogInformation("Progetto {Id} riavviato", projectId);
    }

    public async Task StopAsync(int projectId, CancellationToken ct = default)
    {
        // Cancellazione abbonamento: ferma il container app, conserva i dati.
        await SuspendAsync(projectId, ct);
    }

    // --- pool Postgres ---

    /// <summary>
    /// Assegna un container Postgres del pool al progetto rispettando il limite N,
    /// con retry ottimistico sul concurrency token. Se nessun container ha capienza,
    /// ne crea uno nuovo sul server.
    /// </summary>
    private async Task<DbContainer> AllocateDbContainerAsync(
        Server server, IInfrastructureProvider provider, ServerDockerConfig cfg, CancellationToken ct)
    {
        var limit = server.MaxProjectsPerDbContainer;

        for (var attempt = 0; attempt < MaxAllocationRetries; attempt++)
        {
            var candidate = await _db.DbContainers
                .Where(c => c.ServerId == server.Id && !c.IsFull && c.CurrentProjectCount < limit)
                .OrderBy(c => c.CurrentProjectCount)
                .FirstOrDefaultAsync(ct);

            if (candidate is null)
                break; // nessuna capienza: si crea un nuovo container fuori dal loop.

            candidate.CurrentProjectCount++;
            if (candidate.CurrentProjectCount >= limit)
                candidate.IsFull = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Progetto assegnato a container Postgres {Id} ({Count}/{N})",
                    candidate.Id, candidate.CurrentProjectCount, limit);
                return candidate;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Un altro provisioning ha modificato la riga: ricarica e ritenta.
                _db.Entry(candidate).State = EntityState.Detached;
                _log.LogDebug("Conflitto di concorrenza sul container {Id}, retry", candidate.Id);
            }
        }

        // Nessun container con capienza -> crearne uno nuovo.
        return await CreateNewDbContainerAsync(server, provider, ct);
    }

    private async Task<DbContainer> CreateNewDbContainerAsync(
        Server server, IInfrastructureProvider provider, CancellationToken ct)
    {
        var seq = await _db.DbContainers.CountAsync(c => c.ServerId == server.Id, ct) + 1;
        var name = $"aski-pg-{server.Id}-{seq}";

        var info = await provider.CreatePostgresContainerAsync(server, name, ct);

        var container = new DbContainer
        {
            ServerId = server.Id,
            ContainerName = name,
            RuntimeContainerId = info.RuntimeContainerId,
            Host = info.Host,
            Port = info.Port,
            HostPort = info.HostPort,
            CurrentProjectCount = 1,
            IsFull = server.MaxProjectsPerDbContainer <= 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.DbContainers.Add(container);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Nuovo container Postgres creato nel pool: {Name} ({Id})", name, container.Id);
        return container;
    }

    // --- helper ---

    /// <summary>Genera una password robusta (alfanumerica, niente caratteri da escapare in SQL).</summary>
    private static string GeneratePassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(28);
        var chars = new char[28];
        for (var i = 0; i < bytes.Length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }

    private Task<Project?> LoadProjectAsync(int projectId, CancellationToken ct) =>
        _db.Projects.Include(p => p.Server).FirstOrDefaultAsync(p => p.Id == projectId, ct);

    private async Task SetStatusAsync(Project project, ProvisioningStatus status, CancellationToken ct)
    {
        project.ProvisioningStatus = status;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
