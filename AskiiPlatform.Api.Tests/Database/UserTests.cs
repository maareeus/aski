using Askii.Common;
using Askii.Common.Exceptions;
using Askii.Database.Entities;

namespace Askii.Tests.Database;

public class UserTests
{
    // --- Create ---

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Operator)]
    [InlineData(Roles.Client)]
    public void Create_accetta_tutti_i_ruoli_previsti(string role)
    {
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", role);

        Assert.Equal(role, user.Role);
    }

    [Fact]
    public void Create_valorizza_i_campi_e_genera_un_id()
    {
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

        Assert.Equal("mario@example.com", user.Email);
        Assert.Equal("Mario", user.Name);
        Assert.Equal("Rossi", user.LastName);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.NotEmpty(user.PasswordHash);
        Assert.Null(user.LastLoginUtc);
    }

    [Fact]
    public void Create_non_e_mai_superadmin_e_nasce_disattivato()
    {
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Admin);

        Assert.False(user.IsSuperAdmin);
        Assert.False(user.IsActive);
    }

    [Fact]
    public void Create_normalizza_nome_e_cognome_null_a_stringa_vuota()
    {
        var user = User.Create("mario@example.com", "Password123!", null, null, Roles.Client);

        Assert.Equal(string.Empty, user.Name);
        Assert.Equal(string.Empty, user.LastName);
    }

    [Theory]
    [InlineData("SuperUser")]
    [InlineData("admin")]      // case-sensitive: "admin" != "Admin"
    [InlineData("")]
    [InlineData(null)]
    public void Create_rifiuta_i_ruoli_non_previsti(string? role)
    {
        // InvalidUserRoleException è internal, quindi si asserisce sulla base pubblica.
        var ex = Assert.ThrowsAny<DomainException>(
            () => User.Create("mario@example.com", "Password123!", "Mario", "Rossi", role));

        Assert.Contains("non è valido", ex.Message);
        Assert.Contains("Admin, Operator, Client", ex.Message);
    }

    // --- CreateSuperAdmin ---

    [Fact]
    public void CreateSuperAdmin_e_admin_attivo_e_flaggato()
    {
        var admin = User.CreateSuperAdmin("admin@example.com", "Password123!", "Super", "Admin");

        Assert.Equal(Roles.Admin, admin.Role);
        Assert.True(admin.IsActive);
        Assert.True(admin.IsSuperAdmin);
    }

    // --- Password ---

    [Fact]
    public void La_password_non_viene_salvata_in_chiaro()
    {
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

        Assert.NotEqual("Password123!", user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash); // prefisso BCrypt
    }

    [Fact]
    public void VerifyPassword_true_solo_con_la_password_corretta()
    {
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

        Assert.True(user.VerifyPassword("Password123!"));
        Assert.False(user.VerifyPassword("password123!"));
        Assert.False(user.VerifyPassword("sbagliata"));
    }

    [Fact]
    public void Due_utenti_con_la_stessa_password_hanno_hash_diversi()
    {
        var a = User.Create("a@example.com", "Password123!", null, null, Roles.Client);
        var b = User.Create("b@example.com", "Password123!", null, null, Roles.Client);

        Assert.NotEqual(a.PasswordHash, b.PasswordHash);
    }

    [Fact]
    public void SetPassword_sostituisce_l_hash_precedente()
    {
        var user = User.Create("mario@example.com", "Password123!", null, null, Roles.Client);
        var vecchio = user.PasswordHash;

        user.SetPassword("NuovaPassword456!");

        Assert.NotEqual(vecchio, user.PasswordHash);
        Assert.True(user.VerifyPassword("NuovaPassword456!"));
        Assert.False(user.VerifyPassword("Password123!"));
    }

    // --- UpdateRole ---

    [Fact]
    public void UpdateRole_cambia_il_ruolo_se_valido()
    {
        var user = User.Create("mario@example.com", "Password123!", null, null, Roles.Client);

        user.UpdateRole(Roles.Operator);

        Assert.Equal(Roles.Operator, user.Role);
    }

    [Fact]
    public void UpdateRole_rifiuta_un_ruolo_non_previsto()
    {
        var user = User.Create("mario@example.com", "Password123!", null, null, Roles.Client);

        var ex = Assert.ThrowsAny<DomainException>(() => user.UpdateRole("Root"));
        Assert.Contains("Root", ex.Message);
        Assert.Equal(Roles.Client, user.Role); // il ruolo non è stato toccato
    }

    [Fact]
    public void UpdateRole_impedisce_di_declassare_il_superadmin()
    {
        var admin = User.CreateSuperAdmin("admin@example.com", "Password123!", "Super", "Admin");

        var ex = Assert.ThrowsAny<DomainException>(() => admin.UpdateRole(Roles.Client));
        Assert.Contains("admin@example.com", ex.Message);
        Assert.Equal(Roles.Admin, admin.Role);
    }

    [Fact]
    public void UpdateRole_su_superadmin_verso_Admin_e_consentito()
    {
        var admin = User.CreateSuperAdmin("admin@example.com", "Password123!", "Super", "Admin");

        admin.UpdateRole(Roles.Admin);

        Assert.Equal(Roles.Admin, admin.Role);
    }

    // --- Anagrafica ---

    [Fact]
    public void SetEmail_normalizza_l_email()
    {
        var user = User.Create("mario@example.com", "Password123!", null, null, Roles.Client);

        user.SetEmail("  MARIO@EXAMPLE.COM ");

        Assert.Equal("mario@example.com", user.Email);
    }

    [Fact]
    public void Create_normalizza_l_email_solo_se_gliela_si_passa_normalizzata()
    {
        // Create assegna il campo direttamente, senza passare da SetEmail:
        // la normalizzazione resta responsabilità del chiamante.
        var user = User.Create("  MARIO@EXAMPLE.COM ", "Password123!", null, null, Roles.Client);

        Assert.Equal("  MARIO@EXAMPLE.COM ", user.Email);
    }

    [Fact]
    public void FullName_concatena_nome_e_cognome()
    {
        var user = User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

        Assert.Equal("Mario Rossi", user.FullName);
    }

    // --- Login ---

    [Fact]
    public void RecordLogin_valorizza_LastLoginUtc_in_utc()
    {
        var user = User.Create("mario@example.com", "Password123!", null, null, Roles.Client);
        var prima = DateTime.UtcNow;

        user.RecordLogin();

        Assert.NotNull(user.LastLoginUtc);
        Assert.InRange(user.LastLoginUtc!.Value, prima.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }
}
