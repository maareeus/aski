using Askii.Common;
using Askii.Common.Validation;
using Askii.Features.Auth;
using Askii.Features.Auth.Login;
using Askii.Features.Auth.Tfa;
using Askii.Features.Settings.UpdateSettings;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.UpdateUser;

namespace Askii.Tests.Common;

public class ValidazioneTests
{
    // --- login ---

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("mario@example.com", null)]
    [InlineData("mario@example.com", "")]
    public void Il_login_rifiuta_credenziali_assenti(string? email, string? password)
    {
        var esito = new LoginRequestValidator().Validate(new LoginRequest(email!, password!));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void Il_login_non_impone_la_lunghezza_minima()
    {
        // Una password vecchia potrebbe non rispettare la policy: rifiutarla al
        // login impedirebbe di accedere per poi cambiarla.
        var esito = new LoginRequestValidator().Validate(new LoginRequest("mario@example.com", "corta"));

        Assert.True(esito.IsValid);
    }

    // --- creazione utente ---

    [Theory]
    [InlineData("senza-chiocciola")]
    [InlineData("")]
    [InlineData(null)]
    public void La_creazione_rifiuta_email_non_valide(string? email)
    {
        var esito = new CreateUserRequestValidator()
            .Validate(new CreateUserRequest(email!, "N", "U", Roles.Client, false));

        Assert.False(esito.IsValid);
        Assert.Contains(esito.Errors, e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Theory]
    [InlineData("Root")]
    [InlineData("admin")] // case-sensitive
    [InlineData("")]
    [InlineData(null)]
    public void La_creazione_rifiuta_ruoli_non_previsti(string? ruolo)
    {
        var esito = new CreateUserRequestValidator()
            .Validate(new CreateUserRequest("mario@example.com", "N", "U", ruolo!, false));

        Assert.False(esito.IsValid);
        Assert.Contains(esito.Errors, e => e.PropertyName == nameof(CreateUserRequest.Role));
    }

    [Fact]
    public void Una_creazione_corretta_passa()
    {
        var esito = new CreateUserRequestValidator()
            .Validate(new CreateUserRequest("mario@example.com", "Mario", "Rossi", Roles.Operator, true));

        Assert.True(esito.IsValid);
    }

    // --- modifica utente ---

    [Fact]
    public void La_modifica_richiede_l_identificativo()
    {
        var esito = new UpdateUserRequestValidator()
            .Validate(new UpdateUserRequest(Guid.Empty, null, null, null, null, null));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void I_campi_nulli_della_modifica_non_vengono_validati()
    {
        // null significa "non modificare", non "valore vuoto".
        var esito = new UpdateUserRequestValidator()
            .Validate(new UpdateUserRequest(Guid.NewGuid(), null, null, null, null, null));

        Assert.True(esito.IsValid);
    }

    [Fact]
    public void Un_email_valorizzata_nella_modifica_viene_validata()
    {
        var esito = new UpdateUserRequestValidator()
            .Validate(new UpdateUserRequest(Guid.NewGuid(), "non-una-email", null, null, null, null));

        Assert.False(esito.IsValid);
    }

    // --- policy sulle password ---

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("undicichar")]   // 10 caratteri
    public void Le_password_sotto_il_minimo_sono_rifiutate(string password)
    {
        var esito = new ChangePasswordRequestValidator()
            .Validate(new ChangePasswordRequest(Guid.NewGuid(), password, password, null));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void Una_password_di_dodici_caratteri_e_accettata()
    {
        const string password = "dodicicaratt";
        Assert.Equal(RegolePassword.LunghezzaMinima, password.Length);

        var esito = new ChangePasswordRequestValidator()
            .Validate(new ChangePasswordRequest(Guid.NewGuid(), password, password, null));

        Assert.True(esito.IsValid);
    }

    [Fact]
    public void La_policy_non_impone_classi_di_caratteri()
    {
        // Una passphrase lunga di sole minuscole va bene: NIST SP 800-63B
        // raccomanda la lunghezza, non la composizione.
        const string passphrase = "cavallo batteria graffetta";

        var esito = new ChangePasswordRequestValidator()
            .Validate(new ChangePasswordRequest(Guid.NewGuid(), passphrase, passphrase, null));

        Assert.True(esito.IsValid);
    }

    [Fact]
    public void Le_due_password_devono_coincidere()
    {
        var esito = new ChangePasswordRequestValidator()
            .Validate(new ChangePasswordRequest(Guid.NewGuid(), "passwordlunga", "diversalunga", null));

        Assert.False(esito.IsValid);
        Assert.Contains(esito.Errors, e => e.PropertyName == nameof(ChangePasswordRequest.RePassword));
    }

    // --- attivazione ---

    [Fact]
    public void L_attivazione_richiede_codice_e_password_conforme()
    {
        var validator = new ActivateUserRequestValidator();

        Assert.False(validator.Validate(new ActivateUserRequest("", "passwordlunga", "passwordlunga")).IsValid);
        Assert.False(validator.Validate(new ActivateUserRequest("codice", "corta", "corta")).IsValid);
        Assert.True(validator.Validate(new ActivateUserRequest("codice", "passwordlunga", "passwordlunga")).IsValid);
    }

    // --- 2FA ---

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12 34 56")]
    public void Il_codice_2FA_deve_essere_sei_cifre(string codice)
    {
        var esito = new TfaVerifyRequestValidator()
            .Validate(new TfaVerifyRequest("sfida", TFA_Available.EMAIL_OTP, codice));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void Un_codice_2FA_di_sei_cifre_passa()
    {
        var esito = new TfaVerifyRequestValidator()
            .Validate(new TfaVerifyRequest("sfida", TFA_Available.AUTHENTICATOR_APP, "123456"));

        Assert.True(esito.IsValid);
    }

    [Fact]
    public void Un_metodo_2FA_fuori_enum_e_rifiutato()
    {
        var esito = new TfaVerifyRequestValidator()
            .Validate(new TfaVerifyRequest("sfida", (TFA_Available)99, "123456"));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void La_sfida_e_obbligatoria()
    {
        var esito = new TfaSendOtpRequestValidator().Validate(new TfaSendOtpRequest(""));

        Assert.False(esito.IsValid);
    }

    // --- impostazioni ---

    [Fact]
    public void Le_impostazioni_richiedono_almeno_una_opzione()
    {
        var esito = new UpdateSettingsRequestValidator().Validate(new UpdateSettingsRequest([]));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void Un_valore_troppo_lungo_e_rifiutato()
    {
        var esito = new UpdateSettingsRequestValidator()
            .Validate(new UpdateSettingsRequest(new() { ["smtp_host"] = new string('x', 101) }));

        Assert.False(esito.IsValid);
    }

    [Fact]
    public void Impostazioni_valide_passano()
    {
        var esito = new UpdateSettingsRequestValidator()
            .Validate(new UpdateSettingsRequest(new() { ["smtp_host"] = "smtp.example.com" }));

        Assert.True(esito.IsValid);
    }
}
