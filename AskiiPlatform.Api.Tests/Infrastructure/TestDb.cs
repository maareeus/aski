using Askii.Database;
using Askii.Database.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Infrastructure;

/// <summary>
/// AppDbContext su SQLite in-memory, isolato per singolo test.
/// La connessione va tenuta aperta: chiudendola SQLite distrugge il database.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }

    /// <param name="useMigrations">
    /// false = schema generato dal modello (EnsureCreated).
    /// true  = schema generato applicando le migration reali, per verificare
    ///         che modello e migration siano allineati.
    /// </param>
    public TestDb(bool useMigrations = false)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        Db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options);

        if (useMigrations) Db.Database.Migrate();
        else Db.Database.EnsureCreated();
    }

    /// <summary>Inserisce un utente saltando gli endpoint, per preparare lo stato.</summary>
    public async Task<User> SeedUserAsync(
        string email = "mario.rossi@example.com",
        string password = "Password123!",
        string role = Askii.Common.Roles.Client,
        bool isActive = true,
        string? name = "Mario",
        string? lastName = "Rossi")
    {
        var user = User.Create(email, password, name, lastName, role);
        user.IsActive = isActive;
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    public async Task<User> SeedSuperAdminAsync(
        string email = "admin@example.com",
        string password = "Password123!")
    {
        var admin = User.CreateSuperAdmin(email, password, "Super", "Admin");
        Db.Users.Add(admin);
        await Db.SaveChangesAsync();
        return admin;
    }

    /// <summary>Svuota il change tracker per rileggere davvero dal db.</summary>
    public void Detach() => Db.ChangeTracker.Clear();

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
