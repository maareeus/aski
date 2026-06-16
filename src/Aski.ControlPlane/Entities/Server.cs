using Aski.Shared;

namespace Aski.ControlPlane.Entities;

/// <summary>
/// Server/regione su cui provisionare i progetti dei tenant.
/// Abilitato dal Super Admin; l'utente finale ne sceglie solo il nome/regione.
///
/// Il <see cref="Type"/> decide quale IInfrastructureProvider la factory istanzia
/// (VpsDockerProvider o AwsEcsProvider). <see cref="ConfigJson"/> contiene la
/// configurazione opaca specifica del provider (endpoint Docker, cluster ARN, ecc.).
/// </summary>
public class Server
{
    public int Id { get; set; }

    /// <summary>Nome mostrato all'utente (es. "Europe West - Milano").</summary>
    public required string Name { get; set; }

    /// <summary>Identificativo regione (es. "eu-west-1", "it-mil-1").</summary>
    public required string Region { get; set; }

    /// <summary>Tipo di infrastruttura: seleziona il provider concreto.</summary>
    public ServerType Type { get; set; }

    /// <summary>Hostname/IP del nodo (VPS) o endpoint di servizio.</summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// Configurazione opaca del provider in JSON (credenziali endpoint Docker,
    /// cluster/subnet AWS, ecc.). Non interpretata qui: la legge il provider concreto.
    /// </summary>
    public string? ConfigJson { get; set; }

    /// <summary>
    /// Limite N: numero massimo di progetti che condividono un singolo container
    /// Postgres su questo server. Raggiunto N, si crea un nuovo DbContainer.
    /// </summary>
    public int MaxProjectsPerDbContainer { get; set; } = 10;

    /// <summary>Se false il server non è selezionabile dai tenant.</summary>
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Container Postgres attivi su questo server.</summary>
    public List<DbContainer> DbContainers { get; set; } = new();
}
