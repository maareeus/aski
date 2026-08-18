using Askii.Database.Entities;
using Askii.Features.Settings.GetSettings;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests.Features.Settings;

public class GetSettingsEndpointTests
{
    private static Task<IResult> Leggi(TestDb ctx)
        => GetSettingsEndpoint.Impl(ctx.Db, CancellationToken.None);

    private static SettingsResult Estrai(IResult result)
        => Assert.IsType<Ok<SettingsResult>>(result).Value!;

    private static async Task SeedAsync(TestDb ctx, params (string nome, string valore)[] opzioni)
    {
        foreach (var (nome, valore) in opzioni)
        {
            ctx.Db.Options.Add(new Option(nome, valore));
        }
        await ctx.Db.SaveChangesAsync();
        ctx.Detach();
    }

    [Fact]
    public async Task Senza_opzioni_restituisce_un_elenco_vuoto()
    {
        using var ctx = new TestDb();

        Assert.Empty(Estrai(await Leggi(ctx)).Items);
    }

    [Fact]
    public async Task Restituisce_le_opzioni_ordinate_per_nome()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx,
            (Option.Email.SMTP_PORT, "587"),
            (Option.Email.SMTP_HOST, "smtp.example.com"));

        var nomi = Estrai(await Leggi(ctx)).Items.Select(i => i.Name).ToList();

        Assert.Equal(new[] { Option.Email.SMTP_HOST, Option.Email.SMTP_PORT }, nomi);
    }

    [Fact]
    public async Task Il_valore_delle_opzioni_normali_viene_restituito()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, (Option.Email.SMTP_HOST, "smtp.example.com"));

        var voce = Estrai(await Leggi(ctx)).Items.Single();

        Assert.Equal("smtp.example.com", voce.Value);
        Assert.False(voce.IsSecret);
        Assert.True(voce.HasValue);
    }

    [Fact]
    public async Task La_password_non_viene_mai_restituita()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, (Option.Email.SMTP_PASS, "segretissima"));

        var voce = Estrai(await Leggi(ctx)).Items.Single();

        Assert.Null(voce.Value);
        Assert.True(voce.IsSecret);
        // Il client sa che è configurata, senza conoscerla.
        Assert.True(voce.HasValue);
    }

    [Fact]
    public async Task Una_password_non_impostata_ha_HasValue_falso()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, (Option.Email.SMTP_PASS, ""));

        var voce = Estrai(await Leggi(ctx)).Items.Single();

        Assert.Null(voce.Value);
        Assert.False(voce.HasValue);
    }

    [Fact]
    public async Task Un_valore_vuoto_non_segreto_ha_HasValue_falso()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, (Option.Email.SMTP_HOST, ""));

        var voce = Estrai(await Leggi(ctx)).Items.Single();

        Assert.Equal(string.Empty, voce.Value);
        Assert.False(voce.HasValue);
    }

    [Fact]
    public async Task La_risposta_non_contiene_il_valore_segreto_in_nessun_campo()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx,
            (Option.Email.SMTP_PASS, "segretissima"),
            (Option.Email.SMTP_HOST, "smtp.example.com"));

        var serializzata = System.Text.Json.JsonSerializer.Serialize(Estrai(await Leggi(ctx)));

        Assert.DoesNotContain("segretissima", serializzata);
        Assert.Contains("smtp.example.com", serializzata);
    }

    [Fact]
    public async Task Riporta_l_istante_di_ultimo_aggiornamento()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, (Option.Email.SMTP_HOST, "smtp.example.com"));

        Assert.NotEqual(default, Estrai(await Leggi(ctx)).Items.Single().LastUpdateUtc);
    }
}
