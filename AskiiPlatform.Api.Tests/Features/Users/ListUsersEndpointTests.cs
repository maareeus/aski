using Askii.Common;
using Askii.Common.Paging;
using Askii.Features.Users.ListUsers;
using Askii.Tests.Infrastructure;

namespace Askii.Tests.Features.Users;

public class ListUsersEndpointTests
{
    private static Task<IResult> Lista(
        TestDb ctx,
        string? search = null, string? role = null, bool? isActive = null,
        int? page = null, int? pageSize = null, string? sort = null, string? dir = null)
        => ListUsersEndpoint.Impl(
            new ListUsersRequest(search, role, isActive, page, pageSize, sort, dir),
            ctx.Db, CancellationToken.None);

    private static PagedResult<UserListItem> Estrai(IResult result)
        => Assert.IsType<Ok<PagedResult<UserListItem>>>(result).Value!;

    private static async Task SeedAsync(TestDb ctx, int quanti)
    {
        for (var i = 1; i <= quanti; i++)
        {
            var ruolo = i % 3 == 0 ? Roles.Admin : i % 3 == 1 ? Roles.Operator : Roles.Client;
            await ctx.SeedUserAsync(
                email: $"utente{i:D2}@example.com",
                role: ruolo,
                isActive: i % 2 == 0,
                name: $"Nome{i}",
                lastName: $"Cognome{i}");
        }
    }

    [Fact]
    public async Task La_lista_vuota_non_e_un_errore()
    {
        using var ctx = new TestDb();

        var esito = Estrai(await Lista(ctx));

        Assert.Empty(esito.Items);
        Assert.Equal(0, esito.TotalCount);
        Assert.Equal(0, esito.TotalPages);
        Assert.False(esito.HasNext);
        Assert.False(esito.HasPrevious);
    }

    [Fact]
    public async Task Non_espone_la_password()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync();

        var esito = Estrai(await Lista(ctx));

        // Il DTO non ha alcuna proprietà che assomigli a una password: la
        // proiezione nella query impedisce che l'hash lasci il database.
        Assert.DoesNotContain("password", typeof(UserListItem)
            .GetProperties().Select(p => p.Name.ToLowerInvariant()));
        Assert.Single(esito.Items);
    }

    // --- paginazione ---

    [Fact]
    public async Task Pagina_e_conta_correttamente()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 12);

        var prima = Estrai(await Lista(ctx, pageSize: 5, page: 1));
        Assert.Equal(5, prima.Items.Count);
        Assert.Equal(12, prima.TotalCount);
        Assert.Equal(3, prima.TotalPages);
        Assert.True(prima.HasNext);
        Assert.False(prima.HasPrevious);

        var ultima = Estrai(await Lista(ctx, pageSize: 5, page: 3));
        Assert.Equal(2, ultima.Items.Count);
        Assert.False(ultima.HasNext);
        Assert.True(ultima.HasPrevious);
    }

    [Fact]
    public async Task Le_pagine_non_si_sovrappongono_ne_perdono_righe()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 12);

        var raccolte = new List<string>();
        for (var p = 1; p <= 3; p++)
        {
            raccolte.AddRange(Estrai(await Lista(ctx, pageSize: 5, page: p)).Items.Select(u => u.Email));
        }

        Assert.Equal(12, raccolte.Count);
        Assert.Equal(12, raccolte.Distinct().Count());
    }

    [Fact]
    public async Task Anche_ordinando_su_una_colonna_ripetitiva_le_pagine_restano_disgiunte()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 12);

        // Role ha 3 valori distinti su 12 righe: senza il tiebreaker su Id
        // l'ordine fra pagine non sarebbe deterministico.
        var raccolte = new List<string>();
        for (var p = 1; p <= 4; p++)
        {
            raccolte.AddRange(Estrai(await Lista(ctx, sort: "ruolo", pageSize: 3, page: p)).Items.Select(u => u.Email));
        }

        Assert.Equal(12, raccolte.Distinct().Count());
    }

    [Theory]
    [InlineData(null, PageRequest.DimensionePredefinita)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(9999, PageRequest.DimensioneMassima)]
    public async Task Il_pageSize_viene_riportato_nei_limiti(int? richiesto, int atteso)
    {
        using var ctx = new TestDb();

        var esito = Estrai(await Lista(ctx, pageSize: richiesto));

        Assert.Equal(atteso, esito.PageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Una_pagina_non_valida_diventa_la_prima(int? richiesta)
    {
        using var ctx = new TestDb();

        Assert.Equal(1, Estrai(await Lista(ctx, page: richiesta)).Page);
    }

    // --- ordinamento ---

    [Fact]
    public async Task Ordina_per_email_ascendente_per_default()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("carlo@example.com");
        await ctx.SeedUserAsync("anna@example.com");
        await ctx.SeedUserAsync("bruno@example.com");

        var email = Estrai(await Lista(ctx)).Items.Select(u => u.Email).ToList();

        Assert.Equal(new[] { "anna@example.com", "bruno@example.com", "carlo@example.com" }, email);
    }

    [Fact]
    public async Task La_direzione_desc_inverte_l_ordine()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("anna@example.com");
        await ctx.SeedUserAsync("bruno@example.com");

        var email = Estrai(await Lista(ctx, dir: "desc")).Items.Select(u => u.Email).ToList();

        Assert.Equal(new[] { "bruno@example.com", "anna@example.com" }, email);
    }

    [Theory]
    [InlineData("colonna-inventata")]
    [InlineData("; DROP TABLE Users")]
    [InlineData("")]
    public async Task Una_chiave_di_ordinamento_non_ammessa_ricade_sul_default(string sort)
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("bruno@example.com");
        await ctx.SeedUserAsync("anna@example.com");

        var email = Estrai(await Lista(ctx, sort: sort)).Items.Select(u => u.Email).ToList();

        Assert.Equal(new[] { "anna@example.com", "bruno@example.com" }, email);
    }

    // --- filtri ---

    [Fact]
    public async Task Filtra_per_ruolo()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 9);

        var esito = Estrai(await Lista(ctx, role: Roles.Admin, pageSize: 100));

        Assert.All(esito.Items, u => Assert.Equal(Roles.Admin, u.Role));
        Assert.Equal(3, esito.TotalCount);
    }

    [Fact]
    public async Task Filtra_per_stato()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 10);

        var attivi = Estrai(await Lista(ctx, isActive: true, pageSize: 100));
        var inattivi = Estrai(await Lista(ctx, isActive: false, pageSize: 100));

        Assert.All(attivi.Items, u => Assert.True(u.IsActive));
        Assert.All(inattivi.Items, u => Assert.False(u.IsActive));
        Assert.Equal(10, attivi.TotalCount + inattivi.TotalCount);
    }

    [Fact]
    public async Task I_filtri_si_combinano_in_and()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 12);

        var esito = Estrai(await Lista(ctx, role: Roles.Admin, isActive: true, pageSize: 100));

        Assert.All(esito.Items, u =>
        {
            Assert.Equal(Roles.Admin, u.Role);
            Assert.True(u.IsActive);
        });
    }

    [Fact]
    public async Task Il_conteggio_totale_rispetta_i_filtri_non_la_pagina()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 12);

        var esito = Estrai(await Lista(ctx, role: Roles.Admin, pageSize: 1));

        Assert.Single(esito.Items);
        Assert.Equal(4, esito.TotalCount); // 12/3 sono Admin
    }

    // --- ricerca ---

    [Theory]
    [InlineData("cognome3")]
    [InlineData("COGNOME3")]
    [InlineData("Cognome3")]
    public async Task La_ricerca_e_case_insensitive_sul_cognome(string testo)
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com", name: "Mario", lastName: "Cognome3");
        await ctx.SeedUserAsync("altro@example.com", name: "Altro", lastName: "Bianchi");

        var esito = Estrai(await Lista(ctx, search: testo));

        Assert.Single(esito.Items);
        Assert.Equal("mario@example.com", esito.Items[0].Email);
    }

    [Theory]
    [InlineData("MARIO@")]
    [InlineData("mario@")]
    public async Task La_ricerca_e_case_insensitive_sull_email(string testo)
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com");
        await ctx.SeedUserAsync("altro@example.com");

        Assert.Single(Estrai(await Lista(ctx, search: testo)).Items);
    }

    [Fact]
    public async Task La_ricerca_guarda_email_nome_e_cognome()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("a@example.com", name: "Cercami", lastName: "Uno");
        await ctx.SeedUserAsync("cercami@example.com", name: "Due", lastName: "Due");
        await ctx.SeedUserAsync("c@example.com", name: "Tre", lastName: "Cercami");
        await ctx.SeedUserAsync("d@example.com", name: "Quattro", lastName: "Quattro");

        Assert.Equal(3, Estrai(await Lista(ctx, search: "cercami")).TotalCount);
    }

    [Fact]
    public async Task I_metacaratteri_di_LIKE_non_agiscono_da_jolly()
    {
        using var ctx = new TestDb();
        await ctx.SeedUserAsync("mario@example.com");
        await ctx.SeedUserAsync("anna@example.com");

        // Se "%" non fosse neutralizzato, restituirebbe tutte le righe.
        Assert.Empty(Estrai(await Lista(ctx, search: "%")).Items);
        Assert.Empty(Estrai(await Lista(ctx, search: "_")).Items);
    }

    [Fact]
    public async Task Una_ricerca_di_soli_spazi_non_filtra()
    {
        using var ctx = new TestDb();
        await SeedAsync(ctx, 3);

        Assert.Equal(3, Estrai(await Lista(ctx, search: "   ")).TotalCount);
    }
}
