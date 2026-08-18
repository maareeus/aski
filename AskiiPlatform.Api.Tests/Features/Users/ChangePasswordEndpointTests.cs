using Askii.Common;
using Askii.Features.Users.ChangePassword;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Users;

public class ChangePasswordEndpointTests
{
    private static Task<IResult> Change(
        TestDb ctx, Guid targetId, string password, string rePassword,
        Guid? callerId = null, string? callerRole = Roles.Client, string? oldPassword = null)
        => ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(targetId, password, rePassword, oldPassword),
            ctx.Db,
            TestFactory.Principal(callerId ?? targetId, callerRole),
            TestFactory.Permessi(),
            CancellationToken.None);

    [Fact]
    public async Task Un_utente_puo_cambiare_la_propria_password()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Vecchia123!");

        var result = await Change(ctx, user.Id, "Nuova456!", "Nuova456!",
            callerId: user.Id, oldPassword: "Vecchia123!");

        var ok = Assert.IsType<Ok<ChangePasswordResponse>>(result);
        Assert.True(ok.Value!.result);
        Assert.Equal("Password modificata con successo", ok.Value.msg);

        ctx.Detach();
        var salvato = await ctx.Db.Users.SingleAsync();
        Assert.True(salvato.VerifyPassword("Nuova456!"));
        Assert.False(salvato.VerifyPassword("Vecchia123!"));
    }

    [Fact]
    public async Task Un_admin_puo_cambiare_la_password_di_un_altro_utente()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("target@example.com", password: "Vecchia123!");

        var result = await Change(ctx, target.Id, "Nuova456!", "Nuova456!",
            callerId: Guid.NewGuid(), callerRole: Roles.Admin);

        Assert.IsType<Ok<ChangePasswordResponse>>(result);
        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword("Nuova456!"));
    }

    [Fact]
    public async Task Un_utente_non_admin_non_puo_cambiare_la_password_di_altri()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("target@example.com", password: "Vecchia123!");

        var result = await Change(ctx, target.Id, "Nuova456!", "Nuova456!",
            callerId: Guid.NewGuid(), callerRole: Roles.Client);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
        Assert.Equal("Non hai i permessi per modificare la risorsa", problem.ProblemDetails.Detail);

        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword("Vecchia123!")); // invariata
    }

    [Fact]
    public async Task Un_operator_non_puo_cambiare_la_password_di_altri()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("target@example.com", password: "Vecchia123!");

        var result = await Change(ctx, target.Id, "Nuova456!", "Nuova456!",
            callerId: Guid.NewGuid(), callerRole: Roles.Operator);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Se_le_due_password_non_coincidono_ritorna_400()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Vecchia123!");

        var result = await Change(ctx, user.Id, "Nuova456!", "Diversa789!", callerId: user.Id);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Le password non corrispondono", problem.ProblemDetails.Detail);

        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword("Vecchia123!"));
    }

    [Fact]
    public async Task Il_confronto_delle_password_e_case_sensitive()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        var result = await Change(ctx, user.Id, "Nuova456!", "nuova456!", callerId: user.Id);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Su_utente_inesistente_ritorna_404()
    {
        using var ctx = new TestDb();
        var id = Guid.NewGuid();

        var result = await Change(ctx, id, "Nuova456!", "Nuova456!", callerId: id);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        // La risorsa non esiste: 404, non 400.
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("Utente non trovato", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Il_controllo_permessi_precede_quello_di_esistenza()
    {
        using var ctx = new TestDb();

        // Utente inesistente, chiamante non admin e diverso dal target: vince il 401.
        var result = await Change(ctx, Guid.NewGuid(), "Nuova456!", "Nuova456!",
            callerId: Guid.NewGuid(), callerRole: Roles.Client);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Il_cambio_password_valorizza_UpdatedAtUtc()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Vecchia123!");

        await Change(ctx, user.Id, "Nuova456!", "Nuova456!",
            callerId: user.Id, oldPassword: "Vecchia123!");
        ctx.Detach();

        Assert.NotNull((await ctx.Db.Users.SingleAsync()).UpdatedAtUtc);
    }

    // --- verifica della password attuale ---

    [Fact]
    public async Task Un_utente_deve_fornire_la_password_attuale_corretta()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Vecchia123!");

        var result = await Change(ctx, user.Id, "Nuova456!", "Nuova456!",
            callerId: user.Id, oldPassword: "sbagliata");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
        Assert.Equal("La password non è corretta", problem.ProblemDetails.Detail);

        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword("Vecchia123!"));
    }

    [Fact]
    public async Task Un_utente_senza_password_attuale_riceve_401()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Vecchia123!");

        var result = await Change(ctx, user.Id, "Nuova456!", "Nuova456!",
            callerId: user.Id, oldPassword: null);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public async Task Un_admin_non_deve_fornire_la_password_attuale()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("target@example.com", password: "Vecchia123!");

        var result = await Change(ctx, target.Id, "Nuova456!", "Nuova456!",
            callerId: Guid.NewGuid(), callerRole: Roles.Admin, oldPassword: null);

        Assert.IsType<Ok<ChangePasswordResponse>>(result);
    }

    [Fact]
    public async Task Il_controllo_di_esistenza_precede_quello_della_password_attuale_e_da_404()
    {
        using var ctx = new TestDb();
        var id = Guid.NewGuid();

        var result = await Change(ctx, id, "Nuova456!", "Nuova456!",
            callerId: id, oldPassword: "qualsiasi");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("Utente non trovato", problem.ProblemDetails.Detail);
    }

    // --- protezione del super amministratore ---

    [Fact]
    public async Task Un_admin_non_puo_cambiare_la_password_del_superadmin()
    {
        using var ctx = new TestDb();
        var superAdmin = await ctx.SeedSuperAdminAsync("super@example.com", "Password123!");

        var result = await Change(ctx, superAdmin.Id, "Presa456!", "Presa456!",
            callerId: Guid.NewGuid(), callerRole: Roles.Admin);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
        Assert.Contains("super amministratore", problem.ProblemDetails.Detail!);

        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword("Password123!"));
    }

    [Fact]
    public async Task Il_superadmin_cambia_la_propria_password_fornendo_quella_attuale()
    {
        using var ctx = new TestDb();
        var superAdmin = await ctx.SeedSuperAdminAsync("super@example.com", "Password123!");

        // Pur essendo Admin, su se stesso deve dimostrare di conoscere la
        // password attuale: un token rubato non basta.
        Assert.IsType<ProblemHttpResult>(await Change(ctx, superAdmin.Id, "Nuova456!", "Nuova456!",
            callerId: superAdmin.Id, callerRole: Roles.Admin, oldPassword: null));

        ctx.Detach();
        Assert.IsType<Ok<ChangePasswordResponse>>(await Change(ctx, superAdmin.Id, "Nuova456!", "Nuova456!",
            callerId: superAdmin.Id, callerRole: Roles.Admin, oldPassword: "Password123!"));
    }
}
