using Askii.Common;
using Askii.Features.Auth;
using Askii.Features.Users.Me;
using Askii.Features.Users.Stats;
using Askii.Tests.Infrastructure;

namespace Askii.Tests.Features.Users;

public class MeEndpointTests
{
    private static Task<IResult> Me(TestDb ctx, Guid? id)
        => MeEndpoint.Impl(ctx.Db, TestFactory.Principal(id, Roles.Client), CancellationToken.None);

    [Fact]
    public async Task Restituisce_il_profilo_del_chiamante()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com", role: Roles.Operator,
            name: "Mario", lastName: "Rossi");

        var ok = Assert.IsType<Ok<MeResult>>(await Me(ctx, user.Id));

        Assert.Equal(user.Id, ok.Value!.Id);
        Assert.Equal("mario@example.com", ok.Value.Email);
        Assert.Equal("Mario Rossi", ok.Value.FullName);
        Assert.Equal(Roles.Operator, ok.Value.Role);
        Assert.False(ok.Value.TfaEnabled);
    }

    [Fact]
    public async Task Riflette_una_modifica_fatta_da_un_amministratore()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(role: Roles.Client);

        // È il motivo per cui l'endpoint esiste: il token conservato in locale
        // porta il ruolo vecchio, il database quello nuovo.
        user.UpdateRole(Roles.Operator);
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var ok = Assert.IsType<Ok<MeResult>>(await Me(ctx, user.Id));

        Assert.Equal(Roles.Operator, ok.Value!.Role);
    }

    [Fact]
    public async Task Riporta_i_metodi_2FA_attivi()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();
        user.EnableEmailOtp();
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var ok = Assert.IsType<Ok<MeResult>>(await Me(ctx, user.Id));

        Assert.True(ok.Value!.TfaEnabled);
        Assert.Equal(new[] { TFA_Available.EMAIL_OTP }, ok.Value.TfaMethods);
    }

    [Fact]
    public async Task Non_espone_la_password()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        Assert.IsType<Ok<MeResult>>(await Me(ctx, user.Id));
        Assert.DoesNotContain("password", typeof(MeResult)
            .GetProperties().Select(p => p.Name.ToLowerInvariant()));
    }

    [Fact]
    public async Task Un_utente_non_piu_esistente_da_404()
    {
        using var ctx = new TestDb();

        var problem = Assert.IsType<ProblemHttpResult>(await Me(ctx, Guid.NewGuid()));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }
}

public class UserStatsEndpointTests
{
    private static async Task<UserStatsResult> Stats(TestDb ctx)
        => Assert.IsType<Ok<UserStatsResult>>(
            await UserStatsEndpoint.Impl(ctx.Db, CancellationToken.None)).Value!;

    [Fact]
    public async Task Su_database_vuoto_tutti_i_conteggi_sono_zero()
    {
        using var ctx = new TestDb();

        var s = await Stats(ctx);

        Assert.Equal(0, s.Total);
        Assert.Equal(0, s.Active);
        Assert.Equal(0, s.PendingActivation);
        Assert.Equal(0, s.WithTfa);
        Assert.Null(s.LastLoginUtc);
    }

    [Fact]
    public async Task Conta_totale_attivi_e_da_attivare()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("a@example.com", isActive: true);
        await ctx.SeedUserAsync("b@example.com", isActive: true);
        await ctx.SeedUserAsync("c@example.com", isActive: false);

        var s = await Stats(ctx);

        Assert.Equal(3, s.Total);
        Assert.Equal(2, s.Active);
        Assert.Equal(1, s.PendingActivation);
        // Le due partizioni coprono il totale.
        Assert.Equal(s.Total, s.Active + s.PendingActivation);
    }

    [Fact]
    public async Task Conta_per_ruolo()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("a@example.com", role: Roles.Admin);
        await ctx.SeedUserAsync("b@example.com", role: Roles.Operator);
        await ctx.SeedUserAsync("c@example.com", role: Roles.Client);
        await ctx.SeedUserAsync("d@example.com", role: Roles.Client);

        var s = await Stats(ctx);

        Assert.Equal(1, s.ByRole[Roles.Admin]);
        Assert.Equal(1, s.ByRole[Roles.Operator]);
        Assert.Equal(2, s.ByRole[Roles.Client]);
        Assert.Equal(s.Total, s.ByRole.Values.Sum());
    }

    [Fact]
    public async Task Conta_quanti_hanno_la_2FA()
    {
        using var ctx = new TestDb();
        var conTfa = await ctx.SeedUserAsync("a@example.com");
        conTfa.EnableEmailOtp();
        await ctx.SeedUserAsync("b@example.com");
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        Assert.Equal(1, (await Stats(ctx)).WithTfa);
    }

    [Fact]
    public async Task Riporta_l_ultimo_accesso_piu_recente()
    {
        using var ctx = new TestDb();
        var vecchio = await ctx.SeedUserAsync("a@example.com");
        var recente = await ctx.SeedUserAsync("b@example.com");

        vecchio.RecordLogin();
        await ctx.Db.SaveChangesAsync();
        await Task.Delay(10);
        recente.RecordLogin();
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var s = await Stats(ctx);

        Assert.NotNull(s.LastLoginUtc);
        Assert.Equal(recente.LastLoginUtc, s.LastLoginUtc);
    }

    [Fact]
    public async Task Con_utenti_ma_nessun_accesso_l_ultimo_accesso_e_nullo()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync();

        var s = await Stats(ctx);

        Assert.Equal(1, s.Total);
        Assert.Null(s.LastLoginUtc);
    }
}
