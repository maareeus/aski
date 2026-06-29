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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(256);
            e.HasIndex(x => x.Name);
        });

        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasOne(x => x.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
