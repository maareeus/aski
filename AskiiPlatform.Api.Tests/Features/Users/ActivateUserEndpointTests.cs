using Askii.Features.Users.ActivateUser;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Users;

public class ActivateUserEndpointTests
{
    private static Task<IResult> Activate(TestDb ctx, Guid userId)
        => ActivateUserEndpoint.Impl(new ActivateUserRequest(userId), ctx.Db, CancellationToken.None);

    [Fact]
    public async Task Activate_di_utente_disattivo_lo_attiva_e_ritorna_200()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(isActive: false);

        var result = await Activate(ctx, user.Id);

        var ok = Assert.IsType<Ok<ActivateUserResponse>>(result);
        Assert.True(ok.Value!.result);
        Assert.Equal("Utente attivato", ok.Value.msg);

        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync(u => u.Id == user.Id)).IsActive);
    }

    [Fact]
    public async Task Activate_di_utente_gia_attivo_e_idempotente()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(isActive: true);

        var result = await Activate(ctx, user.Id);

        var ok = Assert.IsType<Ok<ActivateUserResponse>>(result);
        Assert.True(ok.Value!.result);
        Assert.Equal("L'utente era gia stato attivato", ok.Value.msg);
    }

    [Fact]
    public async Task Activate_di_id_inesistente_ritorna_400()
    {
        using var ctx = new TestDb();

        var result = await Activate(ctx, Guid.NewGuid());

        var problem = Assert.IsType<ProblemHttpResult>(result);
        // Semanticamente sarebbe un 404: l'utente non esiste, non è la richiesta a essere malformata.
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Errore durante l'attivazione dell'utente", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Activate_di_id_vuoto_ritorna_400()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync(isActive: false);

        var result = await Activate(ctx, Guid.Empty);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Activate_non_tocca_gli_altri_utenti()
    {
        using var ctx = new TestDb();
        var target = await ctx.SeedUserAsync("a@example.com", isActive: false);
        var altro = await ctx.SeedUserAsync("b@example.com", isActive: false);

        await Activate(ctx, target.Id);
        ctx.Detach();

        Assert.True((await ctx.Db.Users.SingleAsync(u => u.Id == target.Id)).IsActive);
        Assert.False((await ctx.Db.Users.SingleAsync(u => u.Id == altro.Id)).IsActive);
    }
}
