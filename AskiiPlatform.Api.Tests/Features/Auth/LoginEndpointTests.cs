using Askii.Common;
using Askii.Features.Auth.Login;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Auth;

public class LoginEndpointTests
{
    private static Task<IResult> Login(TestDb ctx, string email, string password)
        => LoginEndpoint.Impl(
            new LoginRequest(email, password),
            ctx.Db,
            TestFactory.TokenService(),
            CancellationToken.None);

    [Fact]
    public async Task Login_con_credenziali_valide_ritorna_200_con_token()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!", Roles.Operator);

        var result = await Login(ctx, "mario@example.com", "Password123!");

        var ok = Assert.IsType<Ok<LoginResult>>(result);
        Assert.NotNull(ok.Value);
        Assert.NotEmpty(ok.Value!.Token);
        Assert.Equal(user.Id, ok.Value.UserId);
        Assert.Equal("mario@example.com", ok.Value.Email);
        Assert.Equal("Mario Rossi", ok.Value.FullName);
        Assert.Equal(Roles.Operator, ok.Value.Role);
    }

    [Fact]
    public async Task Login_normalizza_l_email_in_ingresso()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com", "Password123!");

        var result = await Login(ctx, "  MARIO@EXAMPLE.COM  ", "Password123!");

        Assert.IsType<Ok<LoginResult>>(result);
    }

    [Fact]
    public async Task Login_con_password_errata_ritorna_401()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com", "Password123!");

        var result = await Login(ctx, "mario@example.com", "sbagliata");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
        Assert.Equal("Errore di autenticazione", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Login_con_email_inesistente_ritorna_401()
    {
        using var ctx = new TestDb();

        var result = await Login(ctx, "nessuno@example.com", "Password123!");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Login_di_utente_non_attivo_ritorna_401()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com", "Password123!", isActive: false);

        var result = await Login(ctx, "mario@example.com", "Password123!");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Login_fallito_non_espone_il_motivo_specifico()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com", "Password123!", isActive: false);

        var inesistente = Assert.IsType<ProblemHttpResult>(await Login(ctx, "nessuno@example.com", "x"));
        var disattivo = Assert.IsType<ProblemHttpResult>(await Login(ctx, "mario@example.com", "Password123!"));
        var pswErrata = Assert.IsType<ProblemHttpResult>(await Login(ctx, "mario@example.com", "sbagliata"));

        // Buona pratica: lo stesso messaggio per non fare user-enumeration.
        Assert.Equal(inesistente.ProblemDetails.Detail, disattivo.ProblemDetails.Detail);
        Assert.Equal(inesistente.ProblemDetails.Detail, pswErrata.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Login_riuscito_registra_LastLoginUtc_sul_db()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!");
        Assert.Null(user.LastLoginUtc);

        await Login(ctx, "mario@example.com", "Password123!");
        ctx.Detach();

        var salvato = await ctx.Db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.NotNull(salvato.LastLoginUtc);
        Assert.NotNull(salvato.UpdatedAtUtc); // l'audit di SaveChangesAsync ha agito
    }

    [Fact]
    public async Task Login_fallito_non_registra_LastLoginUtc()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!");

        await Login(ctx, "mario@example.com", "sbagliata");
        ctx.Detach();

        var salvato = await ctx.Db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Null(salvato.LastLoginUtc);
    }
}
