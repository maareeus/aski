using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Aski.ControlPlane.Data;

/// <summary>
/// DbContext del Control Plane (database del Super Admin).
/// Contiene impostazioni globali Stripe, piani, server e pool di container Postgres.
///
/// Riceve un <see cref="IDataProtectionProvider"/> per cifrare a riposo i segreti
/// Stripe tramite <see cref="EncryptedConverter"/>.
/// </summary>
public class ControlPlaneDbContext : DbContext
{
    private readonly IDataProtector _secretsProtector;

    public ControlPlaneDbContext(
        DbContextOptions<ControlPlaneDbContext> options,
        IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _secretsProtector = dataProtectionProvider.CreateProtector(EncryptedConverter.Purpose);
    }

    public DbSet<StripeSettings> StripeSettings => Set<StripeSettings>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<DbContainer> DbContainers => Set<DbContainer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var encrypted = new EncryptedConverter(_secretsProtector);

        modelBuilder.Entity<StripeSettings>(e =>
        {
            e.HasKey(x => x.Id);
            // Riga singola: nessun auto-increment, l'Id resta 1.
            e.Property(x => x.Id).ValueGeneratedNever();

            // Le sole chiavi segrete sono cifrate a riposo.
            e.Property(x => x.TestSecretKey).HasConversion(encrypted!);
            e.Property(x => x.TestWebhookSecret).HasConversion(encrypted!);
            e.Property(x => x.LiveSecretKey).HasConversion(encrypted!);
            e.Property(x => x.LiveWebhookSecret).HasConversion(encrypted!);
        });

        modelBuilder.Entity<Plan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.StripeProductId).HasMaxLength(120);
            e.Property(x => x.StripePriceId).HasMaxLength(120);
            e.HasIndex(x => x.StripePriceId).IsUnique().HasFilter(null);
        });

        modelBuilder.Entity<Server>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.Region).HasMaxLength(80);
            e.HasMany(x => x.DbContainers)
                .WithOne(c => c.Server)
                .HasForeignKey(c => c.ServerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DbContainer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ContainerName).HasMaxLength(200);
            // xmin di Postgres come concurrency token (gestito da Npgsql).
            e.Property(x => x.Version).IsRowVersion();
            e.HasIndex(x => new { x.ServerId, x.IsFull });
        });
    }
}
