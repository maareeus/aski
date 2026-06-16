using Aski.ControlPlane.Entities;
using Docker.DotNet;
using Docker.DotNet.Models;
using Npgsql;

namespace Aski.ControlPlane.Services.Infrastructure;

/// <summary>
/// Provider per VPS basate su Docker, con reverse proxy Traefik.
///
/// I container applicativi vengono creati con le label Traefik necessarie a
/// instradare il traffico HTTPS verso il sottodominio (e l'eventuale dominio
/// personalizzato) del progetto, con TLS automatico via certresolver.
///
/// I container Postgres del pool ospitano più database logici (uno per progetto)
/// fino al limite N del server. La creazione del database logico avviene via
/// connessione Npgsql con le credenziali admin del container.
/// </summary>
public sealed class VpsDockerProvider : IInfrastructureProvider
{
    private readonly ILogger<VpsDockerProvider> _log;

    public VpsDockerProvider(ILogger<VpsDockerProvider> log) => _log = log;

    public async Task<PostgresContainerInfo> CreatePostgresContainerAsync(
        Server server, string containerName, CancellationToken ct = default)
    {
        var cfg = ReadConfig(server);
        using var client = CreateClient(cfg);

        await EnsureImageAsync(client, cfg.PostgresImage, ct);

        var create = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = cfg.PostgresImage,
            Name = containerName,
            Env = new List<string>
            {
                $"POSTGRES_USER={cfg.PgAdminUser}",
                $"POSTGRES_PASSWORD={cfg.PgAdminPassword}",
                "POSTGRES_DB=postgres"
            },
            Labels = new Dictionary<string, string> { ["aski.role"] = "postgres-pool" },
            HostConfig = new HostConfig
            {
                NetworkMode = cfg.Network,
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
            }
        }, ct);

        await client.Containers.StartContainerAsync(create.ID, new ContainerStartParameters(), ct);
        _log.LogInformation("Postgres pool container avviato: {Name} ({Id})", containerName, create.ID);

        // Sulla rete Docker il container è raggiungibile per nome host.
        return new PostgresContainerInfo(create.ID, containerName, 5432);
    }

    public async Task CreateDatabaseAsync(
        Server server, PostgresEndpoint pg,
        string databaseName, string dbUser, string dbPassword, CancellationToken ct = default)
    {
        // Connessione al db amministrativo "postgres" per creare ruolo e database.
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = pg.Host,
            Port = pg.Port,
            Username = pg.AdminUser,
            Password = pg.AdminPassword,
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct);

        // Identificatori validati/quotati; la password va in literal con escaping degli apici.
        var dbIdent = QuoteIdentifier(databaseName);
        var userIdent = QuoteIdentifier(dbUser);
        var pwdLiteral = "'" + dbPassword.Replace("'", "''") + "'";

        // 1. Ruolo di login dedicato del progetto.
        await using (var roleCmd = new NpgsqlCommand(
            $"CREATE ROLE {userIdent} LOGIN PASSWORD {pwdLiteral}", conn))
            await roleCmd.ExecuteNonQueryAsync(ct);

        // 2. Database di proprietà del ruolo dedicato.
        await using (var dbCmd = new NpgsqlCommand(
            $"CREATE DATABASE {dbIdent} OWNER {userIdent}", conn))
            await dbCmd.ExecuteNonQueryAsync(ct);

        // 3. Hardening: nessun accesso al DB per PUBLIC; solo il ruolo dedicato.
        await using (var revoke = new NpgsqlCommand(
            $"REVOKE ALL ON DATABASE {dbIdent} FROM PUBLIC", conn))
            await revoke.ExecuteNonQueryAsync(ct);

        _log.LogInformation("Database {Db} + ruolo {User} creati su {Host}:{Port}",
            databaseName, dbUser, pg.Host, pg.Port);
    }

    public async Task<AppContainerInfo> ProvisionAppContainerAsync(
        Server server, AppProvisionRequest request, CancellationToken ct = default)
    {
        var cfg = ReadConfig(server);
        using var client = CreateClient(cfg);

        await EnsureImageAsync(client, request.Image, ct);

        var labels = BuildTraefikLabels(request, cfg);
        var env = request.Environment.Select(kv => $"{kv.Key}={kv.Value}").ToList();

        var create = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = request.Image,
            Name = request.ContainerName,
            Env = env,
            Labels = labels,
            HostConfig = new HostConfig
            {
                NetworkMode = cfg.Network,
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
            }
        }, ct);

        await client.Containers.StartContainerAsync(create.ID, new ContainerStartParameters(), ct);
        _log.LogInformation("App container avviato: {Name} ({Id}) host={Host}",
            request.ContainerName, create.ID, request.PrimaryHost);

        return new AppContainerInfo(create.ID);
    }

    public async Task StartContainerAsync(Server server, string runtimeContainerId, CancellationToken ct = default)
    {
        using var client = CreateClient(ReadConfig(server));
        await client.Containers.StartContainerAsync(runtimeContainerId, new ContainerStartParameters(), ct);
        _log.LogInformation("Container avviato: {Id}", runtimeContainerId);
    }

    public async Task StopContainerAsync(Server server, string runtimeContainerId, CancellationToken ct = default)
    {
        using var client = CreateClient(ReadConfig(server));
        // Stop, NON remove: i dati nel volume restano intatti.
        await client.Containers.StopContainerAsync(runtimeContainerId,
            new ContainerStopParameters { WaitBeforeKillSeconds = 30 }, ct);
        _log.LogInformation("Container fermato (dati conservati): {Id}", runtimeContainerId);
    }

    public async Task RemoveContainerAsync(
        Server server, string runtimeContainerId, bool removeVolumes, CancellationToken ct = default)
    {
        using var client = CreateClient(ReadConfig(server));
        await client.Containers.RemoveContainerAsync(runtimeContainerId,
            new ContainerRemoveParameters { Force = true, RemoveVolumes = removeVolumes }, ct);
        _log.LogWarning("Container rimosso: {Id} (removeVolumes={Rm})", runtimeContainerId, removeVolumes);
    }

    // --- helper ---

    private static ServerDockerConfig ReadConfig(Server server) => ServerDockerConfig.From(server);

    private static DockerClient CreateClient(ServerDockerConfig cfg) =>
        new DockerClientConfiguration(new Uri(cfg.DockerHost)).CreateClient();

    /// <summary>Effettua il pull dell'immagine se non già presente.</summary>
    private async Task EnsureImageAsync(DockerClient client, string image, CancellationToken ct)
    {
        var (fromImage, tag) = SplitImage(image);
        try
        {
            await client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = fromImage, Tag = tag },
                authConfig: null,
                new Progress<JSONMessage>(), ct);
        }
        catch (DockerApiException ex)
        {
            _log.LogWarning(ex, "Pull immagine {Image} fallito (procedo se già locale)", image);
        }
    }

    private static (string fromImage, string tag) SplitImage(string image)
    {
        var idx = image.LastIndexOf(':');
        // Attenzione alle porte del registry (host:port/img): ':' valido solo dopo l'ultimo '/'.
        if (idx > 0 && image.IndexOf('/', idx) < 0)
            return (image[..idx], image[(idx + 1)..]);
        return (image, "latest");
    }

    /// <summary>
    /// Costruisce le label Traefik per instradare HTTPS verso il container.
    /// Router su PrimaryHost (+ CustomHost se presente), TLS via certresolver.
    /// </summary>
    private static Dictionary<string, string> BuildTraefikLabels(AppProvisionRequest req, ServerDockerConfig cfg)
    {
        var router = SanitizeName(req.ContainerName);

        var hostRule = $"Host(`{req.PrimaryHost}`)";
        if (!string.IsNullOrWhiteSpace(req.CustomHost))
            hostRule += $" || Host(`{req.CustomHost}`)";

        return new Dictionary<string, string>
        {
            ["traefik.enable"] = "true",
            ["traefik.docker.network"] = cfg.Network,
            [$"traefik.http.routers.{router}.rule"] = hostRule,
            [$"traefik.http.routers.{router}.entrypoints"] = cfg.Entrypoint,
            [$"traefik.http.routers.{router}.tls"] = "true",
            [$"traefik.http.routers.{router}.tls.certresolver"] = cfg.CertResolver,
            [$"traefik.http.services.{router}.loadbalancer.server.port"] = req.InternalPort.ToString(),
            ["aski.role"] = "ticketing-app"
        };
    }

    private static string SanitizeName(string name) =>
        new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-').ToLowerInvariant();

    /// <summary>Quota un identificatore Postgres rifiutando i nomi non sicuri.</summary>
    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 63)
            throw new ArgumentException("Nome database non valido", nameof(identifier));
        // Consenti solo set sicuro; poi quota raddoppiando eventuali virgolette.
        if (!identifier.All(c => char.IsLetterOrDigit(c) || c == '_'))
            throw new ArgumentException("Nome database con caratteri non ammessi", nameof(identifier));
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
