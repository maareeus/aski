using Aski.Ticketing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Aski.Ticketing.Api.Data;

/// <summary>
/// DbContext dell'istanza di ticketing. Ogni progetto/cliente ha il proprio
/// database isolato (single-tenant): non esistono colonne di tenant-id qui,
/// l'isolamento è a livello di database fisico nel pool Postgres del Control Plane.
/// </summary>
public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<SoftwareProduct> Software => Set<SoftwareProduct>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<DevAssignment> DevAssignments => Set<DevAssignment>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<SoftwareProduct>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DevAssignment>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.Assignments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Software).WithMany().HasForeignKey(x => x.SoftwareId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.CompanyId, x.SoftwareId }).IsUnique();
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300);
            e.HasOne(x => x.Company)
                .WithMany(c => c.Tickets)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Software)
                .WithMany(s => s.Tickets)
                .HasForeignKey(x => x.SoftwareId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedDevUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedDevUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.CompanyId, x.Status });
        });

        modelBuilder.Entity<TicketComment>(e =>
        {
            e.HasOne(x => x.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AuthorUser)
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
