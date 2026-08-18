using Askii.Common;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.CreateUser;
using Askii.Tests.Features.Auth;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Users;

public class ActivateUserEndpointTests
{
    private const string NuovaPassword = "PasswordScelta123!";

    private static Task<IResult> Attiva(TestDb ctx, string codice, string password = NuovaPassword, string? ripeti = null)
        => ActivateUserEndpoint.Impl(
            new ActivateUserRequest(codice, password, ripeti ?? password),
            ctx.Db, CancellationToken.None);

    /// <summary>Crea un utente non attivo e restituisce id e codice.</summary>
    private static async Task<(Guid id, string codice)> CreaDaAttivare(
        TestDb ctx, string email = "nuovo@example.com")
    {
        var result = await CreateUserEndpoint.Impl(
            new CreateUserRequest(email, "Nuovo", "Utente", Roles.Client, IsActive: false),
            ctx.Db, new EmailSenderFinto(), CancellationToken.None);

        var creato = Assert.IsType<Ok<CreateUserResult>>(result).Value!;
        Assert.NotNull(creato.ActivationCode);
        ctx.Detach();

        return (creato.Id, creato.ActivationCode!);
    }

    // --- creazione ---

    [Fact]
    public async Task La_creazione_di_un_utente_non_attivo_emette_un_codice()
    {
        using var ctx = new TestDb();

        var (id, codice) = await CreaDaAttivare(ctx);

        Assert.NotEmpty(codice);
        var salvato = await ctx.Db.Users.SingleAsync(u => u.Id == id);
        Assert.True(salvato.HasPendingActivation);
        // Solo l'hash a database.
        Assert.NotEqual(codice, salvato.ActivationCodeHash);
        Assert.StartsWith("$2", salvato.ActivationCodeHash);
    }

    [Fact]
    public async Task Un_utente_creato_gia_attivo_non_riceve_il_codice()
    {
        using var ctx = new TestDb();

        var result = await CreateUserEndpoint.Impl(
            new CreateUserRequest("attivo@example.com", "A", "A", Roles.Client, IsActive: true),
            ctx.Db, new EmailSenderFinto(), CancellationToken.None);

        var creato = Assert.IsType<Ok<CreateUserResult>>(result).Value!;
        Assert.Null(creato.ActivationCode);
    }

    [Fact]
    public async Task Il_codice_viene_inviato_per_email()
    {
        using var ctx = new TestDb();
        var mailer = new EmailSenderFinto();

        await CreateUserEndpoint.Impl(
            new CreateUserRequest("nuovo@example.com", "N", "U", Roles.Client, IsActive: false),
            ctx.Db, mailer, CancellationToken.None);

        Assert.Equal(1, mailer.Invii);
        Assert.Equal("nuovo@example.com", mailer.UltimoDestinatario);
    }

    // --- attivazione ---

    [Fact]
    public async Task Il_codice_corretto_attiva_e_imposta_la_password_scelta()
    {
        using var ctx = new TestDb();
        var (id, codice) = await CreaDaAttivare(ctx);

        var ok = Assert.IsType<Ok<ActivateUserResponse>>(await Attiva(ctx, codice));
        Assert.True(ok.Value!.result);

        ctx.Detach();
        var salvato = await ctx.Db.Users.SingleAsync(u => u.Id == id);
        Assert.True(salvato.IsActive);
        Assert.True(salvato.VerifyPassword(NuovaPassword));
        Assert.False(salvato.HasPendingActivation);
    }

    [Fact]
    public async Task Dopo_l_attivazione_l_utente_puo_accedere_con_la_propria_password()
    {
        using var ctx = new TestDb();
        var (_, codice) = await CreaDaAttivare(ctx);
        await Attiva(ctx, codice);
        ctx.Detach();

        var result = await Askii.Features.Auth.Login.LoginEndpoint.Impl(
            new Askii.Features.Auth.Login.LoginRequest("nuovo@example.com", NuovaPassword),
            ctx.Db, TestFactory.TokenService(), CancellationToken.None);

        Assert.IsType<Ok<Askii.Features.Auth.Login.LoginResult>>(result);
    }

    [Fact]
    public async Task Il_codice_e_monouso()
    {
        using var ctx = new TestDb();
        var (_, codice) = await CreaDaAttivare(ctx);

        Assert.IsType<Ok<ActivateUserResponse>>(await Attiva(ctx, codice));
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(await Attiva(ctx, codice, "AltraPassword123!"));
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Theory]
    [InlineData("codice-inventato")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_codice_non_valido_da_400(string codice)
    {
        using var ctx = new TestDb();
        await CreaDaAttivare(ctx);

        var problem = Assert.IsType<ProblemHttpResult>(await Attiva(ctx, codice));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("non valido", problem.ProblemDetails.Detail!);
    }

    [Fact]
    public async Task Il_messaggio_di_errore_non_distingue_i_casi()
    {
        using var ctx = new TestDb();
        var (_, codice) = await CreaDaAttivare(ctx);

        var inesistente = Assert.IsType<ProblemHttpResult>(await Attiva(ctx, "mai-emesso"));
        Assert.IsType<Ok<ActivateUserResponse>>(await Attiva(ctx, codice));
        ctx.Detach();
        var giaUsato = Assert.IsType<ProblemHttpResult>(await Attiva(ctx, codice));

        // Distinguere "inesistente" da "già usato" permetterebbe di sondare i codici.
        Assert.Equal(inesistente.ProblemDetails.Detail, giaUsato.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Password_diverse_danno_400_senza_consumare_il_codice()
    {
        using var ctx = new TestDb();
        var (_, codice) = await CreaDaAttivare(ctx);

        var problem = Assert.IsType<ProblemHttpResult>(
            await Attiva(ctx, codice, "Una123!", "Altra123!"));
        Assert.Contains("non corrispondono", problem.ProblemDetails.Detail!);

        ctx.Detach();
        // Il codice è ancora valido.
        Assert.IsType<Ok<ActivateUserResponse>>(await Attiva(ctx, codice));
    }

    [Fact]
    public async Task Un_codice_scaduto_non_attiva()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("scaduto@example.com", isActive: false);
        var codice = user.IssueActivationCode(validitaGiorni: 7, adesso: DateTime.UtcNow.AddDays(-10));
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var problem = Assert.IsType<ProblemHttpResult>(await Attiva(ctx, codice));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        ctx.Detach();
        Assert.False((await ctx.Db.Users.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Il_codice_di_un_utente_non_attiva_un_altro()
    {
        using var ctx = new TestDb();
        var (idA, codiceA) = await CreaDaAttivare(ctx, "a@example.com");
        var (idB, _) = await CreaDaAttivare(ctx, "b@example.com");

        await Attiva(ctx, codiceA);
        ctx.Detach();

        Assert.True((await ctx.Db.Users.SingleAsync(u => u.Id == idA)).IsActive);
        Assert.False((await ctx.Db.Users.SingleAsync(u => u.Id == idB)).IsActive);
    }

    // --- reinvio ---

    [Fact]
    public async Task Il_reinvio_genera_un_codice_nuovo_e_invalida_il_precedente()
    {
        using var ctx = new TestDb();
        var (id, primo) = await CreaDaAttivare(ctx);

        var mailer = new EmailSenderFinto();
        var ok = Assert.IsType<Ok<ResendActivationResponse>>(
            await ResendActivationEndpoint.Impl(new ResendActivationRequest(id), ctx.Db, mailer, CancellationToken.None));

        var secondo = ok.Value!.code;
        Assert.NotEqual(primo, secondo);
        Assert.True(ok.Value.emailSent);
        ctx.Detach();

        Assert.IsType<ProblemHttpResult>(await Attiva(ctx, primo));
        ctx.Detach();
        Assert.IsType<Ok<ActivateUserResponse>>(await Attiva(ctx, secondo));
    }

    [Fact]
    public async Task Il_reinvio_su_un_utente_gia_attivo_da_409()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(isActive: true);

        var problem = Assert.IsType<ProblemHttpResult>(await ResendActivationEndpoint.Impl(
            new ResendActivationRequest(user.Id), ctx.Db, new EmailSenderFinto(), CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task Il_reinvio_su_un_id_inesistente_da_404()
    {
        using var ctx = new TestDb();

        var problem = Assert.IsType<ProblemHttpResult>(await ResendActivationEndpoint.Impl(
            new ResendActivationRequest(Guid.NewGuid()), ctx.Db, new EmailSenderFinto(), CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task Se_l_email_non_parte_il_codice_viene_restituito_comunque()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(isActive: false);

        var ok = Assert.IsType<Ok<ResendActivationResponse>>(await ResendActivationEndpoint.Impl(
            new ResendActivationRequest(user.Id), ctx.Db,
            new EmailSenderFinto(configurato: false), CancellationToken.None));

        Assert.False(ok.Value!.emailSent);
        Assert.NotEmpty(ok.Value.code);
        Assert.Contains("manualmente", ok.Value.msg);
    }
}
