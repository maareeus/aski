using System.Reflection;
using Askii.Database.Entities;
using Askii.Database.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Askii.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

    public DbSet<User> Users => Set<User>();
    public DbSet<Option> Options => Set<Option>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /// Applico le configurazioni leggendo dall'assempbly corrente
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Per ogni elemento in salvataggio che usa BaseEntity
        foreach(var e in ChangeTracker.Entries<BaseEntity>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    e.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;

        // Per ogni elemento in salvataggio che usa BaseEntity
        foreach(var e in ChangeTracker.Entries<BaseEntity>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    e.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
        return base.SaveChanges();
    }
}