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
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

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
            e.Property(x => x.Version).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.Name, x.Version });
        });

        builder.Entity<Ticket>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.HasIndex(x => x.AssignedAgentUserId);
            e.HasOne(x => x.Company)
                .WithMany()
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
            e.HasOne(x => x.AssignedAgentUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedAgentUserId)
                .OnDelete(DeleteBehavior.SetNull);
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
    }
}
