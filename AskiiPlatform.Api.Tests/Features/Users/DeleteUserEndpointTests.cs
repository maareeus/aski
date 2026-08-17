using Askii.Common;
using Askii.Features.Users.DeleteUser;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Users;

public class DeleteUserEndpointTests
{
    /// <summary>Il chiamante di default è un admin diverso dal bersaglio.</summary>
    private static Task<IResult> Delete(TestDb ctx, Guid userId, Guid? callerId = null)
        => DeleteUserEndpoint.Impl(
            new DeleteUserRequest(userId),
            ctx.Db,
            CancellationToken.None,
            TestFactory.Principal(callerId ?? Guid.NewGuid(), Roles.Admin));

    [Fact]
    public async Task Delete_di_utente_normale_lo_rimuove_e_ritorna_200()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        var result = await Delete(ctx, user.Id);

        var ok = Assert.IsType<Ok<DeleteUserResponse>>(result);
        Assert.True(ok.Value!.result);
        Assert.Equal("Utente eliminato", ok.Value.msg);

        ctx.Detach();
        Assert.Empty(await ctx.Db.Users.ToListAsync());
    }

    [Fact]
    public async Task Delete_del_superadmin_e_bloccato()
    {
        using var ctx = new TestDb();
        var admin = await ctx.SeedSuperAdminAsync();

        var result = await Delete(ctx, admin.Id);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("super amministratore", problem.ProblemDetails.Detail!);

        ctx.Detach();
        Assert.Equal(1, await ctx.Db.Users.CountAsync());
    }

    [Fact]
    public async Task Un_utente_non_puo_cancellare_se_stesso()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(role: Roles.Admin);

        var result = await Delete(ctx, user.Id, callerId: user.Id);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Errore durante l'eliminazione dell'utente", problem.ProblemDetails.Detail);

        ctx.Detach();
        Assert.Equal(1, await ctx.Db.Users.CountAsync());
    }

    [Fact]
    public async Task Delete_di_id_inesistente_ritorna_400()
    {
        using var ctx = new TestDb();

        var result = await Delete(ctx, Guid.NewGuid());

        var problem = Assert.IsType<ProblemHttpResult>(result);
        // Anche qui il caso "non trovato" viene reso come 400 anziché 404,
        // e con lo stesso messaggio dell'autocancellazione: i due casi sono indistinguibili.
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Errore durante l'eliminazione dell'utente", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Delete_e_una_cancellazione_fisica_non_logica()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        await Delete(ctx, user.Id);
        ctx.Detach();

        // Nessun soft-delete: la riga non è più recuperabile, storico incluso.
        Assert.Null(await ctx.Db.Users.SingleOrDefaultAsync(u => u.Id == user.Id));
    }

    [Fact]
    public async Task Delete_non_tocca_gli_altri_utenti()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("a@example.com");
        var altro = await ctx.SeedUserAsync("b@example.com");

        await Delete(ctx, target.Id);
        ctx.Detach();

        var rimasti = await ctx.Db.Users.ToListAsync();
        Assert.Single(rimasti);
        Assert.Equal(altro.Id, rimasti[0].Id);
    }

    [Fact]
    public async Task Un_token_senza_claim_di_id_manda_l_endpoint_in_eccezione()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        // Stesso difetto di ChangePassword (#10): CurrentUserId() non regge il claim assente.
        await Assert.ThrowsAsync<ArgumentNullException>(() => DeleteUserEndpoint.Impl(
            new DeleteUserRequest(user.Id),
            ctx.Db,
            CancellationToken.None,
            TestFactory.Principal(userId: null, role: Roles.Admin)));
    }
}
