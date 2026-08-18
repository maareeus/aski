using Askii.Common;
using Askii.Common.Exceptions;
using Askii.Features.Auth;
using Askii.Features.Users.UpdateUser;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Users;

public class UpdateUserEndpointTests
{
    private static Task<IResult> Update(
        TestDb ctx, Guid id, string? email = null, string? name = null,
        string? lastName = null, string? role = null, List<TFA_Available>? tfa = null)
        => UpdateUserEndpoint.AdminImpl(
            new UpdateUserRequest(id, email, name, lastName, role, tfa),
            ctx.Db, CancellationToken.None);

    [Fact]
    public async Task Update_modifica_i_campi_passati()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", name: "Mario", lastName: "Rossi");

        var result = await Update(ctx, user.Id, name: "Marco", lastName: "Bianchi", role: Roles.Operator);

        var ok = Assert.IsType<Ok<UpdateUserResponse>>(result);
        Assert.True(ok.Value!.result);
        Assert.Equal("Utente modificato", ok.Value.msg);

        ctx.Detach();
        var salvato = await ctx.Db.Users.SingleAsync();
        Assert.Equal("Marco", salvato.Name);
        Assert.Equal("Bianchi", salvato.LastName);
        Assert.Equal(Roles.Operator, salvato.Role);
    }

    [Fact]
    public async Task Update_lascia_invariati_i_campi_null()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", role: Roles.Client, name: "Mario", lastName: "Rossi");

        await Update(ctx, user.Id, name: "Marco");
        ctx.Detach();

        var salvato = await ctx.Db.Users.SingleAsync();
        Assert.Equal("Marco", salvato.Name);
        Assert.Equal("Rossi", salvato.LastName);         // non passato -> invariato
        Assert.Equal("mario@example.com", salvato.Email); // non passato -> invariato
        Assert.Equal(Roles.Client, salvato.Role);
    }

    [Fact]
    public async Task Update_normalizza_l_email_e_il_login_continua_a_funzionare()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", "Password123!", isActive: true);

        await Update(ctx, user.Id, email: "  NUOVA.Mail@Example.COM ");
        ctx.Detach();

        Assert.Equal("nuova.mail@example.com", (await ctx.Db.Users.SingleAsync()).Email);

        var result = await Askii.Features.Auth.Login.LoginEndpoint.Impl(
            new Askii.Features.Auth.Login.LoginRequest("NUOVA.Mail@Example.COM", "Password123!"),
            ctx.Db, TestFactory.TokenService(), CancellationToken.None);

        Assert.IsType<Ok<Askii.Features.Auth.Login.LoginResult>>(result);
    }

    [Fact]
    public async Task Update_valorizza_UpdatedAtUtc()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();
        Assert.Null(user.UpdatedAtUtc);

        await Update(ctx, user.Id, name: "Marco");
        ctx.Detach();

        Assert.NotNull((await ctx.Db.Users.SingleAsync()).UpdatedAtUtc);
    }

    [Fact]
    public async Task Update_di_id_inesistente_ritorna_404()
    {
        using var ctx = new TestDb();

        var result = await Update(ctx, Guid.NewGuid(), name: "Marco");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("Utente non trovato", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Un_email_non_valida_da_400_col_messaggio_dedicato()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com");

        var result = await Update(ctx, user.Id, email: "non-una-email");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(UpdateUserResponse.InvalidEmail().msg, problem.ProblemDetails.Detail);

        ctx.Detach();
        Assert.Equal("mario@example.com", (await ctx.Db.Users.SingleAsync(u => u.Id == user.Id)).Email);
    }

    [Fact]
    public async Task Un_email_gia_di_un_altro_utente_da_409_non_500()
    {
        using var ctx = new TestDb();
        var a = await ctx.SeedUserAsync("a@example.com");
        await ctx.SeedUserAsync("b@example.com");

        // Il controllo preventivo evita che l'indice univoco sollevi una
        // DbUpdateException, che il gestore globale tradurrebbe in 500.
        var result = await Update(ctx, a.Id, email: "b@example.com");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Contains("già assegnata", problem.ProblemDetails.Detail!);
    }

    [Fact]
    public async Task Reimpostare_la_propria_stessa_email_non_e_un_conflitto()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com");

        Assert.IsType<Ok<UpdateUserResponse>>(await Update(ctx, user.Id, email: "MARIO@example.com"));
    }

    [Fact]
    public async Task Update_con_ruolo_non_valido_propaga_una_DomainException()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        await Assert.ThrowsAnyAsync<DomainException>(() => Update(ctx, user.Id, role: "Root"));

        ctx.Detach();
        Assert.Equal(Roles.Client, (await ctx.Db.Users.SingleAsync()).Role);
    }

    [Fact]
    public async Task Update_non_puo_declassare_il_superadmin()
    {
        using var ctx = new TestDb();
        var admin = await ctx.SeedSuperAdminAsync();

        await Assert.ThrowsAnyAsync<DomainException>(() => Update(ctx, admin.Id, role: Roles.Client));

        ctx.Detach();
        Assert.Equal(Roles.Admin, (await ctx.Db.Users.SingleAsync()).Role);
    }

    [Fact]
    public async Task Update_non_modifica_la_password()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Password123!");
        var hashPrima = user.PasswordHash;

        await Update(ctx, user.Id, name: "Marco");
        ctx.Detach();

        var salvato = await ctx.Db.Users.SingleAsync();
        Assert.Equal(hashPrima, salvato.PasswordHash);
        Assert.True(salvato.VerifyPassword("Password123!"));
    }

    [Fact]
    public async Task Update_non_riattiva_ne_disattiva_l_utente()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(isActive: false);

        await Update(ctx, user.Id, name: "Marco");
        ctx.Detach();

        Assert.False((await ctx.Db.Users.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Update_non_tocca_gli_altri_utenti()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("a@example.com", name: "A");
        var altro = await ctx.SeedUserAsync("b@example.com", name: "B");

        await Update(ctx, target.Id, name: "Modificato");
        ctx.Detach();

        Assert.Equal("B", (await ctx.Db.Users.SingleAsync(u => u.Id == altro.Id)).Name);
    }
}
