using System.IdentityModel.Tokens.Jwt;
using Askii.Common;
using Askii.Common.Security;
using Askii.ExternalServices;
using Askii.Features.Auth;
using Askii.Features.Auth.Login;
using Askii.Features.Auth.Tfa;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Auth;

/// <summary>Mailer che non invia nulla e conserva l'ultimo messaggio.</summary>
public class EmailSenderFinto(bool configurato = true) : IEmailSender
{
    public bool Configurato { get; } = configurato;
    public string? UltimoDestinatario { get; private set; }
    public string? UltimoCorpo { get; private set; }
    public int Invii { get; private set; }

    public Task<EsitoInvio> InviaAsync(string destinatario, string oggetto, string corpoTesto, CancellationToken ct = default)
    {
        Invii++;
        UltimoDestinatario = destinatario;
        UltimoCorpo = corpoTesto;
        return Task.FromResult(Configurato ? EsitoInvio.Ok() : EsitoInvio.Ko("SMTP non configurato"));
    }
}

public class TfaLoginTests
{
    private static Task<IResult> Login(TestDb ctx, string email, string password)
        => LoginEndpoint.Impl(new LoginRequest(email, password), ctx.Db,
            TestFactory.TokenService(), CancellationToken.None);

    private static LoginResult EstraiLogin(IResult r)
        => Assert.IsType<Ok<LoginResult>>(r).Value!;

    // --- login senza 2FA ---

    [Fact]
    public async Task Senza_2FA_il_login_restituisce_subito_il_token()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com", "Password123!");

        var esito = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!"));

        Assert.Equal(AuthStatus.OK, esito.Status);
        Assert.NotNull(esito.Token);
        Assert.Null(esito.ChallengeToken);
    }

    // --- login con 2FA ---

    private static async Task<(Guid id, string segreto)> ConTotp(TestDb ctx, string email = "mario@example.com")
    {
        var user = await ctx.SeedUserAsync(email, "Password123!");
        var segreto = user.StartTotpEnrollment();
        user.ConfirmTotp(Totp.Codice(segreto));
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();
        return (user.Id, segreto);
    }

    [Fact]
    public async Task Con_2FA_attiva_il_login_non_restituisce_il_token()
    {
        using var ctx = new TestDb();
        await ConTotp(ctx);

        var esito = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!"));

        Assert.Equal(AuthStatus.TFA_REQUIRED, esito.Status);
        Assert.Null(esito.Token);
        Assert.NotNull(esito.ChallengeToken);
        Assert.Equal(new[] { TFA_Available.AUTHENTICATOR_APP }, esito.TfaMethods);
        // Nessun dato dell'utente prima del secondo fattore.
        Assert.Null(esito.Email);
        Assert.Null(esito.Role);
    }

    [Fact]
    public async Task Con_password_errata_la_sfida_non_viene_emessa()
    {
        using var ctx = new TestDb();
        await ConTotp(ctx);

        var problem = Assert.IsType<ProblemHttpResult>(await Login(ctx, "mario@example.com", "sbagliata"));

        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Il_login_con_2FA_non_aggiorna_l_ultimo_accesso()
    {
        using var ctx = new TestDb();
        var (id, _) = await ConTotp(ctx);

        await Login(ctx, "mario@example.com", "Password123!");
        ctx.Detach();

        // L'accesso è registrato solo a verifica completata.
        Assert.Null((await ctx.Db.Users.SingleAsync(u => u.Id == id)).LastLoginUtc);
    }

    // --- la sfida non è un token d'accesso ---

    [Fact]
    public void La_sfida_ha_un_audience_diversa_dal_token_di_accesso()
    {
        var user = Askii.Database.Entities.User.Create(
            "mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);
        var service = TestFactory.TokenService();

        var accesso = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(user));
        var sfida = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateTfaChallenge(user));

        Assert.NotEqual(accesso.Audiences.Single(), sfida.Audiences.Single());
        Assert.EndsWith(":tfa", sfida.Audiences.Single());
    }

    [Fact]
    public void La_sfida_non_porta_il_ruolo()
    {
        var user = Askii.Database.Entities.User.Create(
            "mario@example.com", "Password123!", "Mario", "Rossi", Roles.Admin);

        var sfida = new JwtSecurityTokenHandler()
            .ReadJwtToken(TestFactory.TokenService().GenerateTfaChallenge(user));

        Assert.DoesNotContain(sfida.Claims, c => c.Value == Roles.Admin);
    }

    [Fact]
    public void Un_token_di_accesso_non_e_accettato_come_sfida()
    {
        var user = Askii.Database.Entities.User.Create(
            "mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);
        var service = TestFactory.TokenService();

        Assert.Null(service.ReadTfaChallenge(service.GenerateToken(user)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("non-un-token")]
    [InlineData("aaa.bbb.ccc")]
    public void Una_sfida_malformata_non_viene_letta(string? token)
        => Assert.Null(TestFactory.TokenService().ReadTfaChallenge(token));

    [Fact]
    public void Una_sfida_firmata_con_un_altra_chiave_non_viene_letta()
    {
        var user = Askii.Database.Entities.User.Create(
            "mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);

        var altrove = TestFactory.TokenService(key: "una-chiave-completamente-diversa-32-byte");
        var sfida = altrove.GenerateTfaChallenge(user);

        Assert.Null(TestFactory.TokenService().ReadTfaChallenge(sfida));
    }

    [Fact]
    public void La_sfida_e_leggibile_e_indica_l_utente()
    {
        var user = Askii.Database.Entities.User.Create(
            "mario@example.com", "Password123!", "Mario", "Rossi", Roles.Client);
        var service = TestFactory.TokenService();

        Assert.Equal(user.Id, service.ReadTfaChallenge(service.GenerateTfaChallenge(user)));
    }

    // --- verifica con app di authenticator ---

    private static Task<IResult> Verifica(TestDb ctx, string sfida, TFA_Available metodo, string codice)
        => TfaVerifyEndpoint.Impl(new TfaVerifyRequest(sfida, metodo, codice), ctx.Db,
            TestFactory.TokenService(), CancellationToken.None);

    [Fact]
    public async Task Verifica_con_codice_TOTP_corretto_restituisce_il_token()
    {
        using var ctx = new TestDb();
        var (id, segreto) = await ConTotp(ctx);
        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var esito = EstraiLogin(await Verifica(ctx, sfida, TFA_Available.AUTHENTICATOR_APP, Totp.Codice(segreto)));

        Assert.Equal(AuthStatus.OK, esito.Status);
        Assert.NotNull(esito.Token);
        Assert.Equal(id, esito.UserId);

        ctx.Detach();
        Assert.NotNull((await ctx.Db.Users.SingleAsync(u => u.Id == id)).LastLoginUtc);
    }

    [Fact]
    public async Task Verifica_con_codice_TOTP_errato_da_401()
    {
        using var ctx = new TestDb();
        await ConTotp(ctx);
        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(
            await Verifica(ctx, sfida, TFA_Available.AUTHENTICATOR_APP, "000000"));

        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Verifica_con_una_sfida_non_valida_da_401()
    {
        using var ctx = new TestDb();
        var (_, segreto) = await ConTotp(ctx);

        var problem = Assert.IsType<ProblemHttpResult>(
            await Verifica(ctx, "sfida-inventata", TFA_Available.AUTHENTICATOR_APP, Totp.Codice(segreto)));

        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Non_si_puo_verificare_con_un_metodo_non_attivo()
    {
        using var ctx = new TestDb();
        await ConTotp(ctx); // solo authenticator
        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(
            await Verifica(ctx, sfida, TFA_Available.EMAIL_OTP, "123456"));

        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    // --- verifica con OTP via email ---

    private static Task<IResult> InviaOtp(TestDb ctx, string sfida, IEmailSender mailer)
        => TfaSendOtpEndpoint.Impl(new TfaSendOtpRequest(sfida), ctx.Db,
            TestFactory.TokenService(), mailer, CancellationToken.None);

    [Fact]
    public async Task Il_ciclo_completo_con_OTP_via_email_funziona()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!");
        user.EnableEmailOtp();
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var mailer = new EmailSenderFinto();
        Assert.IsType<Ok<TfaSendOtpResponse>>(await InviaOtp(ctx, sfida, mailer));
        Assert.Equal(1, mailer.Invii);
        Assert.Equal("mario@example.com", mailer.UltimoDestinatario);

        // Il codice si legge dal corpo della mail, come farebbe l'utente.
        var codice = new string(mailer.UltimoCorpo!.Where(char.IsAsciiDigit).ToArray())[..6];
        ctx.Detach();

        var esito = EstraiLogin(await Verifica(ctx, sfida, TFA_Available.EMAIL_OTP, codice));
        Assert.Equal(AuthStatus.OK, esito.Status);
        Assert.NotNull(esito.Token);
    }

    [Fact]
    public async Task L_email_del_destinatario_viene_mascherata_nella_risposta()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario.rossi@example.com", "Password123!");
        user.EnableEmailOtp();
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var sfida = EstraiLogin(await Login(ctx, "mario.rossi@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var ok = Assert.IsType<Ok<TfaSendOtpResponse>>(await InviaOtp(ctx, sfida, new EmailSenderFinto()));

        Assert.DoesNotContain("mario.rossi@", ok.Value!.msg);
        Assert.Contains("ma", ok.Value.msg);
        Assert.Contains("@example.com", ok.Value.msg);
    }

    [Fact]
    public async Task Chiedere_l_OTP_senza_avere_il_metodo_attivo_da_400()
    {
        using var ctx = new TestDb();
        await ConTotp(ctx); // solo authenticator
        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(await InviaOtp(ctx, sfida, new EmailSenderFinto()));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task Se_l_invio_fallisce_l_utente_riceve_un_400_esplicativo()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!");
        user.EnableEmailOtp();
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(
            await InviaOtp(ctx, sfida, new EmailSenderFinto(configurato: false)));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("SMTP", problem.ProblemDetails.Detail!);
    }

    [Fact]
    public async Task I_tentativi_falliti_sull_OTP_vengono_persistiti()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!");
        user.EnableEmailOtp();
        await ctx.Db.SaveChangesAsync();
        var id = user.Id;
        ctx.Detach();

        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;
        ctx.Detach();
        await InviaOtp(ctx, sfida, new EmailSenderFinto());
        ctx.Detach();

        await Verifica(ctx, sfida, TFA_Available.EMAIL_OTP, "000000");
        ctx.Detach();

        Assert.Equal(1, (await ctx.Db.Users.SingleAsync(u => u.Id == id)).EmailOtpAttempts);
    }

    [Fact]
    public async Task Un_utente_disattivato_non_completa_la_verifica()
    {
        using var ctx = new TestDb();
        var (id, segreto) = await ConTotp(ctx);
        var sfida = EstraiLogin(await Login(ctx, "mario@example.com", "Password123!")).ChallengeToken!;

        var user = await ctx.Db.Users.SingleAsync(u => u.Id == id);
        user.IsActive = false;
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(
            await Verifica(ctx, sfida, TFA_Available.AUTHENTICATOR_APP, Totp.Codice(segreto)));

        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }
}
