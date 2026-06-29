using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aski.Tickets.Api.Data;

/// <summary>
/// DbContext applicativo basato su Identity (utenti/ruoli) + entità di dominio.
/// Store: SQLite via EF Core.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SoftwareProduct> Software => Set<SoftwareProduct>();
    public DbSet<SoftwareVersion> SoftwareVersions => Set<SoftwareVersion>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitMembership> UnitMemberships => Set<UnitMembership>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.VatNumber).HasMaxLength(32);
            e.Property(x => x.ContactEmail).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Property(x => x.Address).HasMaxLength(300);
            e.HasIndex(x => x.Name);
        });

        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.JobTitle).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Ignore(x => x.FullName); // proprietà calcolata
            e.HasOne(x => x.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Molti-a-molti: Azienda <-> Software
        builder.Entity<Company>()
            .HasMany(c => c.Softwares)
            .WithMany(s => s.Companies)
            .UsingEntity("CompanySoftware");

        // Molti-a-molti: Utente <-> Software (ambito assistenza)
        builder.Entity<AppUser>()
            .HasMany(u => u.Softwares)
            .WithMany(s => s.Users)
            .UsingEntity("UserSoftware");

        builder.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SoftwareProduct>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Name);
            e.HasMany(x => x.Versions)
                .WithOne(v => v.Software)
                .HasForeignKey(v => v.SoftwareId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SoftwareVersion>(e =>
        {
            e.Property(x => x.Version).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.SoftwareId, x.Version });
        });

        builder.Entity<Ticket>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Number).HasMaxLength(20);
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.HasIndex(x => x.AssignedUserId);
            e.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Software)
                .WithMany(s => s.Tickets)
                .HasForeignKey(x => x.SoftwareId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SoftwareVersion)
                .WithMany()
                .HasForeignKey(x => x.SoftwareVersionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedUnit)
                .WithMany()
                .HasForeignKey(x => x.AssignedUnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TicketAttachment>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120);
            e.Property(x => x.StoredPath).HasMaxLength(400).IsRequired();
            e.HasOne(x => x.Ticket).WithMany(t => t.Attachments).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TicketComment>(e =>
        {
            e.Property(x => x.Body).IsRequired();
            e.HasOne(x => x.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AuthorUser)
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Contact>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Title).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CompanyId);
        });

        builder.Entity<Unit>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.HasMany(x => x.Memberships).WithOne(m => m.Unit).HasForeignKey(m => m.UnitId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UnitMembership>(e =>
        {
            e.HasIndex(x => new { x.UnitId, x.UserId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(e =>
        {
            e.Property(x => x.Type).HasMaxLength(40);
            e.Property(x => x.Message).HasMaxLength(400);
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Ticket).WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

    }
}
