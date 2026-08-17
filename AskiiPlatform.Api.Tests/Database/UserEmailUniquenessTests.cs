using Askii.Common;
using Askii.Database.Entities;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Database;

/// <summary>
/// Verifica il vincolo di unicità dell'email definito in
/// Database/Configuration/UserConfiguration.cs. Gira sulle migration reali,
/// così si testa lo schema che finisce in produzione.
/// </summary>
public class UserEmailUniquenessTests
{
    private static User Nuovo(string email, string nome = "A")
        => User.Create(email, "Password123!", nome, nome, Roles.Client);

    [Fact]
    public async Task Il_db_rifiuta_due_utenti_con_la_stessa_email()
    {
        using var ctx = new TestDb(useMigrations: true);

        ctx.Db.Users.Add(Nuovo("mario@example.com", "A"));
        ctx.Db.Users.Add(Nuovo("mario@example.com", "B"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Il_vincolo_e_case_insensitive_grazie_alla_collation_NOCASE()
    {
        using var ctx = new TestDb(useMigrations: true);

        ctx.Db.Users.Add(Nuovo("mario@example.com"));
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        // Anche se un percorso di scrittura dimenticasse di normalizzare,
        // il db non lascia entrare il doppione.
        ctx.Db.Users.Add(Nuovo("MARIO@EXAMPLE.COM"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task La_ricerca_per_email_e_case_insensitive()
    {
        using var ctx = new TestDb(useMigrations: true);
        ctx.Db.Users.Add(Nuovo("mario@example.com"));
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        Assert.NotNull(await ctx.Db.Users.SingleOrDefaultAsync(u => u.Email == "MARIO@EXAMPLE.COM"));
    }

    [Fact]
    public async Task Email_diverse_convivono_senza_problemi()
    {
        using var ctx = new TestDb(useMigrations: true);

        ctx.Db.Users.Add(Nuovo("a@example.com", "A"));
        ctx.Db.Users.Add(Nuovo("b@example.com", "B"));
        await ctx.Db.SaveChangesAsync();

        Assert.Equal(2, await ctx.Db.Users.CountAsync());
    }

    [Fact]
    public async Task Dopo_la_cancellazione_l_email_torna_riutilizzabile()
    {
        using var ctx = new TestDb(useMigrations: true);
        var user = Nuovo("mario@example.com");
        ctx.Db.Users.Add(user);
        await ctx.Db.SaveChangesAsync();

        ctx.Db.Users.Remove(user);
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        ctx.Db.Users.Add(Nuovo("mario@example.com", "B"));
        await ctx.Db.SaveChangesAsync();

        Assert.Equal(1, await ctx.Db.Users.CountAsync());
    }

    [Fact]
    public void Gli_indici_previsti_esistono_nel_modello()
    {
        using var ctx = new TestDb(useMigrations: true);
        var entity = ctx.Db.Model.FindEntityType(typeof(User))!;

        var emailIndex = entity.GetIndexes()
            .Single(i => i.Properties.Any(p => p.Name == nameof(User.Email)));

        Assert.True(emailIndex.IsUnique);
        Assert.Contains(entity.GetIndexes(), i => i.Properties.Any(p => p.Name == nameof(User.Role)));
        Assert.Contains(entity.GetIndexes(), i => i.Properties.Any(p => p.Name == nameof(User.IsActive)));
    }
}
