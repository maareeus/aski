using Aski.ControlPlane.Entities;

namespace Aski.ControlPlane.Services.Infrastructure;

/// <summary>
/// Provider per AWS ECS (AWS SDK for .NET). Stub: l'implementazione completa
/// (RegisterTaskDefinition, CreateService/RunTask, Application Load Balancer +
/// target group per il routing, RDS/Postgres su container Fargate per il pool)
/// è prevista come estensione. La factory lo istanzia per i server ServerType.AwsEcs.
/// </summary>
public sealed class AwsEcsProvider : IInfrastructureProvider
{
    private readonly ILogger<AwsEcsProvider> _log;

    public AwsEcsProvider(ILogger<AwsEcsProvider> log) => _log = log;

    private const string NotImpl =
        "AwsEcsProvider non ancora implementato. Usare un server VpsDocker oppure completare l'integrazione AWS SDK.";

    public Task<PostgresContainerInfo> CreatePostgresContainerAsync(Server server, string containerName, CancellationToken ct = default)
        => throw new NotImplementedException(NotImpl);

    public Task CreateDatabaseAsync(Server server, PostgresEndpoint pg, string databaseName, string dbUser, string dbPassword, CancellationToken ct = default)
        => throw new NotImplementedException(NotImpl);

    public Task<AppContainerInfo> ProvisionAppContainerAsync(Server server, AppProvisionRequest request, CancellationToken ct = default)
        => throw new NotImplementedException(NotImpl);

    public Task StartContainerAsync(Server server, string runtimeContainerId, CancellationToken ct = default)
        => throw new NotImplementedException(NotImpl);

    public Task StopContainerAsync(Server server, string runtimeContainerId, CancellationToken ct = default)
        => throw new NotImplementedException(NotImpl);

    public Task RemoveContainerAsync(Server server, string runtimeContainerId, bool removeVolumes, CancellationToken ct = default)
        => throw new NotImplementedException(NotImpl);
}
