using Askii.Common;
using Askii.Features.Auth;
using Askii.Features.Users.GetUser;
using Askii.Tests.Infrastructure;

namespace Askii.Tests.Features.Users;

public class GetUserEndpointTests
{
    private static Task<IResult> Dettaglio(TestDb ctx, Guid id)
        => GetUserEndpoint.Impl(id, ctx.Db, CancellationToken.None);

    [Fact]
    public async Task Restituisce_il_dettaglio_dell_utente()
    {
        using var ctx = new TestDb();
        var utente = await ctx.SeedUserAsync(
            "mario@example.com", role: Roles.Operator, isActive: true,
            name: "Mario", lastName: "Rossi");

        var ok = Assert.IsType<Ok<UserDetail>>(await Dettaglio(ctx, utente.Id));

        Assert.Equal(utente.Id, ok.Value!.Id);
        Assert.Equal("mario@example.com", ok.Value.Email);
        Assert.Equal("Mario", ok.Value.Name);
        Assert.Equal("Rossi", ok.Value.LastName);
        Assert.Equal("Mario Rossi", ok.Value.FullName);
        Assert.Equal(Roles.Operator, ok.Value.Role);
        Assert.True(ok.Value.IsActive);
        Assert.False(ok.Value.IsSuperAdmin);
        Assert.NotEqual(default, ok.Value.CreatedAtUtc);
    }

    [Fact]
    public async Task Un_id_inesistente_da_404_non_400()
    {
        using var ctx = new TestDb();

        var problem = Assert.IsType<ProblemHttpResult>(await Dettaglio(ctx, Guid.NewGuid()));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task Non_espone_la_password()
    {
        using var ctx = new TestDb();
        var utente = await ctx.SeedUserAsync();

        Assert.IsType<Ok<UserDetail>>(await Dettaglio(ctx, utente.Id));
        Assert.DoesNotContain("password", typeof(UserDetail)
            .GetProperties().Select(p => p.Name.ToLowerInvariant()));
    }

    [Fact]
    public async Task Riporta_i_metodi_2FA_configurati()
    {
        using var ctx = new TestDb();
        var utente = await ctx.SeedUserAsync();
        utente.TFA_Availables = [TFA_Available.EMAIL_OTP, TFA_Available.AUTHENTICATOR_APP];
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var ok = Assert.IsType<Ok<UserDetail>>(await Dettaglio(ctx, utente.Id));

        Assert.Equal(2, ok.Value!.TFA_Availables.Count);
        Assert.Contains(TFA_Available.EMAIL_OTP, ok.Value.TFA_Availables);
    }

    [Fact]
    public async Task Segnala_il_superadmin()
    {
        using var ctx = new TestDb();
        var admin = await ctx.SeedSuperAdminAsync();

        var ok = Assert.IsType<Ok<UserDetail>>(await Dettaglio(ctx, admin.Id));

        Assert.True(ok.Value!.IsSuperAdmin);
    }

    [Fact]
    public async Task Il_dettaglio_riflette_una_modifica_appena_salvata()
    {
        using var ctx = new TestDb();
        var utente = await ctx.SeedUserAsync(name: "Mario");

        utente.Name = "Marco";
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();

        var ok = Assert.IsType<Ok<UserDetail>>(await Dettaglio(ctx, utente.Id));

        Assert.Equal("Marco", ok.Value!.Name);
        Assert.NotNull(ok.Value.UpdatedAtUtc);
    }
}
