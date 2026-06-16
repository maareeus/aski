using Aski.ControlPlane.Entities;
using Aski.Shared;

namespace Aski.ControlPlane.Services.Infrastructure;

/// <inheritdoc cref="IInfrastructureProviderFactory"/>
public sealed class InfrastructureProviderFactory : IInfrastructureProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public InfrastructureProviderFactory(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

    public IInfrastructureProvider Create(Server server) => server.Type switch
    {
        ServerType.VpsDocker => new VpsDockerProvider(_loggerFactory.CreateLogger<VpsDockerProvider>()),
        ServerType.AwsEcs => new AwsEcsProvider(_loggerFactory.CreateLogger<AwsEcsProvider>()),
        _ => throw new NotSupportedException($"ServerType non supportato: {server.Type}")
    };
}
