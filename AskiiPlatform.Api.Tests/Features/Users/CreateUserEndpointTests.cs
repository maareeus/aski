using Askii.Common;
using Askii.Common.Exceptions;
using Askii.Features.Users.CreateUser;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Users;

public class CreateUserEndpointTests
{
    private static Task<IResult> Create(
        TestDb ctx,
        string email = "nuovo@example.com",
        string? name = "Nuovo",
        string? lastName = "Utente",
        string role = Roles.Client,
        bool isActive = false)
        => CreateUserEndpoint.Impl(
            new CreateUserRequest(email, name, lastName, role, isActive),
            ctx.Db,
            CancellationToken.None);

    /// <summary>
    /// Create genera una password casuale che non restituisce: per poter testare
    /// il login si reimposta un valore noto direttamente sull'entità.
    /// </summary>
    private static async Task ImpostaPasswordNotaAsync(TestDb ctx, string password)
    {
        var salvato = await ctx.Db.Users.SingleAsync();
        salvato.SetPassword(password);
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();
    }

    [Fact]
    public async Task Create_con_dati_validi_ritorna_200_e_persiste_l_utente()
    {
        using var ctx = new TestDb();

        var result = await Create(ctx, "nuovo@example.com", role: Roles.Operator);

        var ok = Assert.IsType<Ok<CreateUserResult>>(result);
        Assert.True(ok.Value!.Result);
        Assert.Equal("nuovo@example.com", ok.Value.Email);
        Assert.Equal("Nuovo Utente", ok.Value.FullName);
        Assert.Equal(Roles.Operator, ok.Value.Role);
        Assert.NotEqual(Guid.Empty, ok.Value.Id);

        ctx.Detach();
        var salvato = await ctx.Db.Users.SingleAsync();
        Assert.Equal(ok.Value.Id, salvato.Id);
        Assert.Equal(Roles.Operator, salvato.Role);
        Assert.NotEqual(default, salvato.CreatedAtUtc);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_rispetta_IsActive_della_richiesta(bool isActive)
    {
        using var ctx = new TestDb();

        var result = await Create(ctx, isActive: isActive);

        var ok = Assert.IsType<Ok<CreateUserResult>>(result);
        Assert.Equal(isActive, ok.Value!.IsActive);

        ctx.Detach();
        Assert.Equal(isActive, (await ctx.Db.Users.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Create_genera_una_password_casuale_diversa_per_ogni_utente()
    {
        using var ctx = new TestDb();

        await Create(ctx, "a@example.com");
        await Create(ctx, "b@example.com");
        ctx.Detach();

        var utenti = await ctx.Db.Users.ToListAsync();
        Assert.All(utenti, u => Assert.StartsWith("$2", u.PasswordHash));
        Assert.NotEqual(utenti[0].PasswordHash, utenti[1].PasswordHash);
    }

    [Fact]
    public async Task Un_utente_creato_attivo_puo_loggarsi_una_volta_nota_la_password()
    {
        using var ctx = new TestDb();

        await Create(ctx, "attivo@example.com", isActive: true);
        ctx.Detach();
        await ImpostaPasswordNotaAsync(ctx, "Password123!");

        var result = await Askii.Features.Auth.Login.LoginEndpoint.Impl(
            new Askii.Features.Auth.Login.LoginRequest("attivo@example.com", "Password123!"),
            ctx.Db, TestFactory.TokenService(), CancellationToken.None);

        Assert.IsType<Ok<Askii.Features.Auth.Login.LoginResult>>(result);
    }

    [Fact]
    public async Task Create_salva_l_email_normalizzata()
    {
        using var ctx = new TestDb();

        await Create(ctx, "  Mario.Rossi@Example.COM ");

        ctx.Detach();
        Assert.Equal("mario.rossi@example.com", (await ctx.Db.Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task L_utente_creato_con_email_maiuscola_riesce_a_loggarsi()
    {
        using var ctx = new TestDb();

        await Create(ctx, "Mario.Rossi@Example.COM", isActive: true);
        ctx.Detach();
        await ImpostaPasswordNotaAsync(ctx, "Password123!");

        var result = await Askii.Features.Auth.Login.LoginEndpoint.Impl(
            new Askii.Features.Auth.Login.LoginRequest("Mario.Rossi@Example.COM", "Password123!"),
            ctx.Db, TestFactory.TokenService(), CancellationToken.None);

        Assert.IsType<Ok<Askii.Features.Auth.Login.LoginResult>>(result);
    }

    [Fact]
    public async Task Create_non_salva_la_password_in_chiaro()
    {
        using var ctx = new TestDb();

        await Create(ctx);

        ctx.Detach();
        var salvato = await ctx.Db.Users.SingleAsync();
        Assert.StartsWith("$2", salvato.PasswordHash);  // hash BCrypt
        Assert.Equal(60, salvato.PasswordHash.Length);
    }

    [Theory]
    [InlineData("senza-chiocciola")]
    [InlineData("@example.com")]
    [InlineData("mario@")]
    public async Task Create_con_email_non_valida_ritorna_400(string email)
    {
        using var ctx = new TestDb();

        var result = await Create(ctx, email);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("non è valida", problem.ProblemDetails.Detail!);
        Assert.Empty(await ctx.Db.Users.ToListAsync());
    }

    [Fact]
    public async Task Create_con_email_gia_presente_ritorna_409()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com");

        var result = await Create(ctx, "mario@example.com");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Contains("gia presente", problem.ProblemDetails.Detail!);
        Assert.Equal(1, await ctx.Db.Users.CountAsync());
    }

    [Fact]
    public async Task Il_controllo_duplicati_e_case_insensitive_perche_normalizza_l_input()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com");

        var result = await Create(ctx, "MARIO@EXAMPLE.COM");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task Create_con_ruolo_non_valido_propaga_una_DomainException()
    {
        using var ctx = new TestDb();

        // Non è gestita nell'endpoint: risale al GlobalExceptionHandler, che la mappa a 400.
        await Assert.ThrowsAnyAsync<DomainException>(() => Create(ctx, role: "Root"));
        Assert.Empty(await ctx.Db.Users.ToListAsync());
    }

    [Fact]
    public async Task Create_con_nome_e_cognome_null_non_fallisce()
    {
        using var ctx = new TestDb();

        var result = await Create(ctx, name: null, lastName: null);

        var ok = Assert.IsType<Ok<CreateUserResult>>(result);
        Assert.Equal(" ", ok.Value!.FullName); // FullName è sempre "Name LastName", quindi uno spazio
    }
}
