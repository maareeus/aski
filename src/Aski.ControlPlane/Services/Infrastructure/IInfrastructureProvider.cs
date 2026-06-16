using Aski.ControlPlane.Entities;

namespace Aski.ControlPlane.Services.Infrastructure;

/// <summary>
/// Astrazione sul backend infrastrutturale di un <see cref="Server"/>.
/// Implementata da VpsDockerProvider (Docker.DotNet + Traefik) e AwsEcsProvider
/// (AWS SDK). Istanziata da <see cref="IInfrastructureProviderFactory"/> in base
/// al <see cref="ServerType"/> del server scelto dal tenant.
///
/// Le operazioni di Suspend/Stop non distruggono mai i dati: fermano i container.
/// Solo Remove con removeVolumes=true cancella, e va usato unicamente per la
/// deprovisioning definitiva post-retention.
/// </summary>
public interface IInfrastructureProvider
{
    /// <summary>Crea e avvia un nuovo container Postgres del pool sul server.</summary>
    Task<PostgresContainerInfo> CreatePostgresContainerAsync(
        Server server, string containerName, CancellationToken ct = default);

    /// <summary>
    /// Crea un database logico isolato dentro un container Postgres esistente, con un
    /// ruolo di login dedicato (owner del DB, privilegi limitati al solo database).
    /// </summary>
    Task CreateDatabaseAsync(
        Server server, PostgresEndpoint pg,
        string databaseName, string dbUser, string dbPassword, CancellationToken ct = default);

    /// <summary>Crea e avvia il container applicativo (ticketing) con label Traefik.</summary>
    Task<AppContainerInfo> ProvisionAppContainerAsync(
        Server server, AppProvisionRequest request, CancellationToken ct = default);

    /// <summary>Avvia un container già esistente (resume).</summary>
    Task StartContainerAsync(Server server, string runtimeContainerId, CancellationToken ct = default);

    /// <summary>Ferma un container senza rimuoverlo (suspend): i dati restano.</summary>
    Task StopContainerAsync(Server server, string runtimeContainerId, CancellationToken ct = default);

    /// <summary>Rimuove un container. removeVolumes=true cancella anche i dati (irreversibile).</summary>
    Task RemoveContainerAsync(
        Server server, string runtimeContainerId, bool removeVolumes, CancellationToken ct = default);
}

/// <summary>Crea l'IInfrastructureProvider corretto per un dato server.</summary>
public interface IInfrastructureProviderFactory
{
    IInfrastructureProvider Create(Server server);
}
