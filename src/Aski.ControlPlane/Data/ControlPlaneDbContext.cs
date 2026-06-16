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
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();
    public DbSet<PortalUser> PortalUsers => Set<PortalUser>();

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

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CompanyName).HasMaxLength(200);
            e.Property(x => x.BillingEmail).HasMaxLength(256);
            e.Property(x => x.StripeCustomerId).HasMaxLength(120);
            e.HasIndex(x => x.StripeCustomerId);
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StripeSubscriptionId).HasMaxLength(120);
            e.Property(x => x.StripeCustomerId).HasMaxLength(120);
            e.HasIndex(x => x.StripeSubscriptionId).IsUnique();
            e.HasOne(x => x.Tenant)
                .WithMany(t => t.Subscriptions)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Subdomain).HasMaxLength(120);
            e.Property(x => x.CustomDomain).HasMaxLength(253);
            e.Property(x => x.DatabaseName).HasMaxLength(120);
            e.HasIndex(x => x.Subdomain).IsUnique();
            e.HasOne(x => x.Tenant)
                .WithMany(t => t.Projects)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Subscription)
                .WithOne(s => s.Project)
                .HasForeignKey<Project>(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.DbContainer)
                .WithMany()
                .HasForeignKey(x => x.DbContainerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProcessedStripeEvent>(e =>
        {
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).HasMaxLength(120);
            e.Property(x => x.EventType).HasMaxLength(120);
        });

        modelBuilder.Entity<PortalUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
