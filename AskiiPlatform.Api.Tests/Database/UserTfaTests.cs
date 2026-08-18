using Askii.Common;
using Askii.Common.Security;
using Askii.Database.Entities;
using Askii.Features.Auth;

namespace Askii.Tests.Database;

public class UserTfaTests
{
    private static User Nuovo() =>
        User.Create("mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

    // --- stato iniziale ---

    [Fact]
    public void Un_utente_nuovo_non_ha_la_2FA()
    {
        var user = Nuovo();

        Assert.False(user.TfaEnabled);
        Assert.Empty(user.TFA_Availables);
        Assert.Null(user.TotpSecret);
        Assert.False(user.HasPendingTotp);
    }

    // --- app di authenticator ---

    [Fact]
    public void StartTotpEnrollment_crea_un_segreto_in_attesa_di_conferma()
    {
        var user = Nuovo();

        var segreto = user.StartTotpEnrollment();

        Assert.NotNull(segreto);
        Assert.Equal(segreto, user.TotpSecret);
        Assert.True(user.HasPendingTotp);
        // Non attivo finché non arriva un codice valido.
        Assert.False(user.TfaEnabled);
        Assert.DoesNotContain(TFA_Available.AUTHENTICATOR_APP, user.TFA_Availables);
    }

    [Fact]
    public void ConfirmTotp_con_un_codice_valido_attiva_il_metodo()
    {
        var user = Nuovo();
        var segreto = user.StartTotpEnrollment();
        var codice = Totp.Codice(segreto);

        Assert.True(user.ConfirmTotp(codice));
        Assert.True(user.TfaEnabled);
        Assert.Contains(TFA_Available.AUTHENTICATOR_APP, user.TFA_Availables);
        Assert.False(user.HasPendingTotp);
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("abcdef")]
    [InlineData("")]
    [InlineData(null)]
    public void ConfirmTotp_con_un_codice_non_valido_non_attiva_nulla(string? codice)
    {
        var user = Nuovo();
        user.StartTotpEnrollment();

        Assert.False(user.ConfirmTotp(codice));
        Assert.False(user.TfaEnabled);
    }

    [Fact]
    public void ConfirmTotp_senza_associazione_avviata_fallisce()
    {
        var user = Nuovo();

        Assert.False(user.ConfirmTotp("123456"));
    }

    [Fact]
    public void VerifyTotp_funziona_solo_dopo_la_conferma()
    {
        var user = Nuovo();
        var segreto = user.StartTotpEnrollment();

        // Segreto presente ma metodo non attivo: la verifica di login deve dire no.
        Assert.False(user.VerifyTotp(Totp.Codice(segreto)));

        user.ConfirmTotp(Totp.Codice(segreto));
        Assert.True(user.VerifyTotp(Totp.Codice(segreto)));
    }

    [Fact]
    public void Riavviare_l_associazione_invalida_il_segreto_precedente()
    {
        var user = Nuovo();
        var primo = user.StartTotpEnrollment();
        user.ConfirmTotp(Totp.Codice(primo));

        var secondo = user.StartTotpEnrollment();

        Assert.NotEqual(primo, secondo);
        Assert.False(user.TfaEnabled);
        Assert.False(user.VerifyTotp(Totp.Codice(primo)));
    }

    [Fact]
    public void DisableTotp_cancella_segreto_e_metodo()
    {
        var user = Nuovo();
        var segreto = user.StartTotpEnrollment();
        user.ConfirmTotp(Totp.Codice(segreto));

        user.DisableTotp();

        Assert.Null(user.TotpSecret);
        Assert.False(user.TfaEnabled);
        Assert.False(user.VerifyTotp(Totp.Codice(segreto)));
    }

    // --- OTP via email ---

    [Fact]
    public void EnableEmailOtp_attiva_il_metodo_senza_emettere_codici()
    {
        var user = Nuovo();

        user.EnableEmailOtp();

        Assert.Contains(TFA_Available.EMAIL_OTP, user.TFA_Availables);
        Assert.Null(user.EmailOtpHash);
    }

    [Fact]
    public void EnableEmailOtp_e_idempotente()
    {
        var user = Nuovo();

        user.EnableEmailOtp();
        user.EnableEmailOtp();

        Assert.Single(user.TFA_Availables);
    }

    [Fact]
    public void IssueEmailOtp_restituisce_sei_cifre_e_salva_solo_l_hash()
    {
        var user = Nuovo();
        user.EnableEmailOtp();

        var codice = user.IssueEmailOtp();

        Assert.Equal(6, codice.Length);
        Assert.All(codice, c => Assert.True(char.IsAsciiDigit(c)));
        Assert.NotNull(user.EmailOtpHash);
        Assert.NotEqual(codice, user.EmailOtpHash);
        Assert.StartsWith("$2", user.EmailOtpHash);
        Assert.NotNull(user.EmailOtpExpiresUtc);
    }

    [Fact]
    public void VerifyEmailOtp_accetta_il_codice_emesso()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var codice = user.IssueEmailOtp();

        Assert.True(user.VerifyEmailOtp(codice));
    }

    [Fact]
    public void Un_codice_e_usabile_una_volta_sola()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var codice = user.IssueEmailOtp();

        Assert.True(user.VerifyEmailOtp(codice));
        Assert.False(user.VerifyEmailOtp(codice));
        Assert.Null(user.EmailOtpHash);
    }

    [Fact]
    public void Un_codice_scaduto_viene_rifiutato_e_cancellato()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var adesso = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var codice = user.IssueEmailOtp(validitaMinuti: 5, adesso: adesso);

        Assert.False(user.VerifyEmailOtp(codice, adesso.AddMinutes(6)));
        Assert.Null(user.EmailOtpHash);
    }

    [Fact]
    public void Un_codice_resta_valido_entro_la_scadenza()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var adesso = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var codice = user.IssueEmailOtp(validitaMinuti: 5, adesso: adesso);

        Assert.True(user.VerifyEmailOtp(codice, adesso.AddMinutes(4)));
    }

    [Fact]
    public void Dopo_troppi_tentativi_il_codice_viene_invalidato()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var codice = user.IssueEmailOtp();

        for (var i = 0; i < User.MaxEmailOtpAttempts; i++)
        {
            Assert.False(user.VerifyEmailOtp("000000"));
        }

        // Anche il codice giusto ora non vale più: va richiesto un nuovo invio.
        Assert.False(user.VerifyEmailOtp(codice));
        Assert.Null(user.EmailOtpHash);
    }

    [Fact]
    public void I_tentativi_si_azzerano_a_ogni_nuovo_invio()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        user.IssueEmailOtp();
        user.VerifyEmailOtp("000000");
        user.VerifyEmailOtp("000000");

        var nuovo = user.IssueEmailOtp();

        Assert.Equal(0, user.EmailOtpAttempts);
        Assert.True(user.VerifyEmailOtp(nuovo));
    }

    [Fact]
    public void Un_nuovo_invio_invalida_il_codice_precedente()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var primo = user.IssueEmailOtp();
        var secondo = user.IssueEmailOtp();

        Assert.False(user.VerifyEmailOtp(primo));
        // Il primo tentativo fallito non consuma il secondo codice.
        user.IssueEmailOtp();
        Assert.NotEqual(primo, secondo);
    }

    [Fact]
    public void VerifyEmailOtp_fallisce_se_il_metodo_non_e_attivo()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        var codice = user.IssueEmailOtp();

        user.DisableEmailOtp();

        Assert.False(user.VerifyEmailOtp(codice));
    }

    [Fact]
    public void DisableEmailOtp_cancella_anche_il_codice_pendente()
    {
        var user = Nuovo();
        user.EnableEmailOtp();
        user.IssueEmailOtp();

        user.DisableEmailOtp();

        Assert.Null(user.EmailOtpHash);
        Assert.Null(user.EmailOtpExpiresUtc);
        Assert.False(user.TfaEnabled);
    }

    // --- recupero ---

    [Fact]
    public void DisableAllTfa_riporta_l_utente_senza_secondo_fattore()
    {
        var user = Nuovo();
        var segreto = user.StartTotpEnrollment();
        user.ConfirmTotp(Totp.Codice(segreto));
        user.EnableEmailOtp();
        user.IssueEmailOtp();

        user.DisableAllTfa();

        Assert.False(user.TfaEnabled);
        Assert.Empty(user.TFA_Availables);
        Assert.Null(user.TotpSecret);
        Assert.Null(user.EmailOtpHash);
    }

    [Fact]
    public void I_due_metodi_convivono()
    {
        var user = Nuovo();
        var segreto = user.StartTotpEnrollment();
        user.ConfirmTotp(Totp.Codice(segreto));
        user.EnableEmailOtp();

        Assert.Equal(2, user.TFA_Availables.Count);

        // Disattivarne uno non tocca l'altro.
        user.DisableTotp();
        Assert.Equal(new[] { TFA_Available.EMAIL_OTP }, user.TFA_Availables);
    }
}
