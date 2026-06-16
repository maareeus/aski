using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aski.Ticketing.Api.Data;

/// <summary>Factory design-time per `dotnet ef` (migrations) dell'istanza ticketing.</summary>
public class TicketingDbContextFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    public TicketingDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ASKI_TENANT_DB")
                   ?? "Host=localhost;Port=5432;Database=aski_ticketing_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new TicketingDbContext(options);
    }
}
