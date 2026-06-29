using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aski.Tickets.Api.Data;

/// <summary>Factory per `dotnet ef` (migrations) a design-time. Usa SQLite locale.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ASKI_TICKETS_DB") ?? "Data Source=aski-tickets.db";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        return new AppDbContext(options);
    }
}
