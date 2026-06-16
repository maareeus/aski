using System.Text.Json;
using Aski.ControlPlane.Entities;

namespace Aski.ControlPlane.Services.Infrastructure;

/// <summary>
/// Dati di un container Postgres del pool appena creato/avviato.
/// Host/Port = endpoint sulla rete Docker (usato dai container app).
/// HostPort = porta pubblicata su localhost (usata dal Control Plane su host per
/// le operazioni admin tipo CREATE DATABASE). 0 se non pubblicata.
/// </summary>
public sealed record PostgresContainerInfo(string RuntimeContainerId, string Host, int Port, int HostPort);

/// <summary>Endpoint di un Postgres esistente verso cui creare un database logico.</summary>
public sealed record PostgresEndpoint(string Host, int Port, string AdminUser, string AdminPassword);

/// <summary>
/// Richiesta di provisioning del container applicativo (istanza ticketing).
/// PrimaryHost = sottodominio instradato da Traefik; CustomHost = dominio
/// personalizzato opzionale aggiuntivo; InternalPort = porta su cui ascolta l'app
/// nel container; Environment = variabili (es. connection string del DB progetto).
/// </summary>
public sealed record AppProvisionRequest(
    string ContainerName,
    string Image,
    string PrimaryHost,
    string? CustomHost,
    int InternalPort,
    IReadOnlyDictionary<string, string> Environment);

/// <summary>Dati del container applicativo provisionato.</summary>
public sealed record AppContainerInfo(string RuntimeContainerId);

/// <summary>
/// Configurazione specifica VPS/Docker deserializzata da Server.ConfigJson.
/// Esempio:
/// { "dockerHost": "tcp://10.0.0.5:2376", "network": "traefik",
///   "appImage": "registry/aski-ticketing:latest", "certResolver": "le",
///   "pgAdminUser": "postgres", "pgAdminPassword": "..." }
/// </summary>
public sealed class ServerDockerConfig
{
    /// <summary>Endpoint del demone Docker (es. "unix:///var/run/docker.sock" o "tcp://host:2376").</summary>
    public string DockerHost { get; set; } = "unix:///var/run/docker.sock";

    /// <summary>Rete Docker condivisa con Traefik su cui agganciare i container.</summary>
    public string Network { get; set; } = "traefik";

    /// <summary>Immagine del backend ticketing da avviare per ogni progetto.</summary>
    public string AppImage { get; set; } = "aski/ticketing-api:latest";

    /// <summary>Immagine Postgres per i container del pool.</summary>
    public string PostgresImage { get; set; } = "postgres:16-alpine";

    /// <summary>certresolver Traefik per TLS automatico (Let's Encrypt).</summary>
    public string CertResolver { get; set; } = "le";

    /// <summary>Entrypoint Traefik HTTPS.</summary>
    public string Entrypoint { get; set; } = "websecure";

    /// <summary>Credenziali admin dei container Postgres del pool.</summary>
    public string PgAdminUser { get; set; } = "postgres";
    public string PgAdminPassword { get; set; } = "postgres";

    /// <summary>Suffisso di dominio per i sottodomini dei progetti (es. "aski.app").</summary>
    public string DomainSuffix { get; set; } = "aski.app";

    /// <summary>Deserializza la configurazione da Server.ConfigJson (default se assente).</summary>
    public static ServerDockerConfig From(Server server)
    {
        if (string.IsNullOrWhiteSpace(server.ConfigJson))
            return new ServerDockerConfig();
        return JsonSerializer.Deserialize<ServerDockerConfig>(server.ConfigJson,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ServerDockerConfig();
    }
}
