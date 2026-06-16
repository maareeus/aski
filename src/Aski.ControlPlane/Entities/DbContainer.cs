namespace Aski.ControlPlane.Entities;

/// <summary>
/// Container Postgres condiviso su un <see cref="Server"/>.
///
/// Più progetti condividono lo stesso container fino al limite N
/// (<see cref="Server.MaxProjectsPerDbContainer"/>). Ogni progetto ottiene un
/// database logico isolato all'interno del container.
///
/// <see cref="CurrentProjectCount"/> + <see cref="Version"/> (xmin di Postgres usato
/// come concurrency token) prevengono la race condition per cui due provisioning
/// concorrenti supererebbero il limite N sullo stesso container.
/// </summary>
public class DbContainer
{
    public int Id { get; set; }

    public int ServerId { get; set; }
    public Server Server { get; set; } = null!;

    /// <summary>Nome del container (es. "aski-pg-<serverId>-<seq>").</summary>
    public required string ContainerName { get; set; }

    /// <summary>Id runtime del container Docker/ECS, valorizzato dopo l'avvio.</summary>
    public string? RuntimeContainerId { get; set; }

    /// <summary>Host raggiungibile per le connessioni (rete interna o hostname).</summary>
    public string? Host { get; set; }

    /// <summary>Porta esposta dal container Postgres.</summary>
    public int Port { get; set; } = 5432;

    /// <summary>Numero di progetti attualmente ospitati. Confrontato con N.</summary>
    public int CurrentProjectCount { get; set; }

    /// <summary>True quando ha raggiunto N e non accetta nuovi progetti.</summary>
    public bool IsFull { get; set; }

    /// <summary>
    /// Concurrency token. Mappato su xmin di Postgres in OnModelCreating:
    /// EF solleva DbUpdateConcurrencyException se la riga cambia tra lettura e save.
    /// </summary>
    public uint Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
