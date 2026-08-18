using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Askii.Database.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // L'email è la credenziale di login: l'unicità deve stare sul db, non solo
        // nel controllo applicativo di CreateUser, che due richieste concorrenti superano.
        // NOCASE rende confronto e indice case-insensitive: così "Mario@x.it" e
        // "mario@x.it" collidono anche se un percorso di scrittura non normalizza.
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320)          // 64 parte locale + @ + 255 dominio
            .UseCollation("NOCASE");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100);         // BCrypt produce 60 caratteri

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20);

        // Le liste utenti sono filtrate per ruolo e per stato di attivazione
        builder.HasIndex(u => u.Role);
        builder.HasIndex(u => u.IsActive);

        builder.Property(u => u.IsSuperAdmin)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.Property(u => u.TFA_Availables)
            .HasColumnType("TEXT");
    }
}
