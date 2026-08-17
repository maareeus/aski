using Askii.Common;
using Askii.Database.Entities;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Database;

public class AppDbContextAuditTests
{
    [Fact]
    public async Task SaveChangesAsync_valorizza_CreatedAtUtc_in_insert()
    {
        using var ctx = new TestDb();
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);
        Assert.Equal(default, user.CreatedAtUtc);

        var prima = DateTime.UtcNow;
        ctx.Db.Users.Add(user);
        await ctx.Db.SaveChangesAsync();

        Assert.InRange(user.CreatedAtUtc, prima.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        Assert.Null(user.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_valorizza_UpdatedAtUtc_in_update()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();
        Assert.Null(user.UpdatedAtUtc);

        user.Name = "Cambiato";
        await ctx.Db.SaveChangesAsync();

        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_non_altera_CreatedAtUtc_in_update()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();
        var creazione = user.CreatedAtUtc;

        user.Name = "Cambiato";
        await ctx.Db.SaveChangesAsync();

        Assert.Equal(creazione, user.CreatedAtUtc);
    }

    [Fact]
    public void SaveChanges_sincrono_applica_lo_stesso_audit()
    {
        using var ctx = new TestDb();
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

        ctx.Db.Users.Add(user);
        ctx.Db.SaveChanges();

        Assert.NotEqual(default, user.CreatedAtUtc);
    }

    [Fact]
    public async Task Le_date_di_audit_sono_in_utc()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        // Confronto con UtcNow: se fosse Now locale la differenza sarebbe pari all'offset.
        Assert.InRange(user.CreatedAtUtc, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public void Il_modello_e_allineato_alle_migration()
    {
        // Se il modello divergesse dalle migration, Migrate() creerebbe uno schema
        // su cui le query del modello fallirebbero.
        using var ctx = new TestDb(useMigrations: true);

        Assert.Empty(ctx.Db.Users.ToList());
        Assert.Empty(ctx.Db.Database.GetPendingMigrations());
    }
}
