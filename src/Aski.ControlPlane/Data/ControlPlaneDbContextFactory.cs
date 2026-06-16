using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aski.ControlPlane.Data;

/// <summary>
/// Factory usata solo a design-time da `dotnet ef` (migrations / scaffolding).
/// Il ctor del DbContext richiede un IDataProtectionProvider: qui ne forniamo
/// uno effimero perché la generazione delle migration non cifra dati reali.
/// La connection string è quella di sviluppo (override via env ASKI_DB).
/// </summary>
public class ControlPlaneDbContextFactory : IDesignTimeDbContextFactory<ControlPlaneDbContext>
{
    public ControlPlaneDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ASKI_DB")
                   ?? "Host=localhost;Port=5432;Database=aski_controlplane;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseNpgsql(conn)
            .Options;

        // Provider effimero: sufficiente a costruire il context per le migration.
        var protectionProvider = DataProtectionProvider.Create("Aski.DesignTime");

        return new ControlPlaneDbContext(options, protectionProvider);
    }
}
