using Askii.Common.Extensions;
using System.Security.Claims;
using Askii.Common;
using Askii.Common.Authorization;
using Askii.Tests.Infrastructure;

namespace Askii.Tests.Common;

public class PermissionRegistryTests
{
    private static readonly IPermissionRegistry Registro = new PermissionRegistry();

    // --- assegnazione predefinita ---

    [Fact]
    public void L_admin_ha_tutti_i_permessi()
    {
        Assert.Equal(
            Permissions.Tutti.OrderBy(p => p),
            Registro.PermessiDi(Roles.Admin).OrderBy(p => p));
    }

    [Theory]
    [InlineData(Permissions.Users.Read)]
    [InlineData(Permissions.Settings.Read)]
    public void L_operator_puo_leggere(string permesso)
        => Assert.True(Registro.RuoloHa(Roles.Operator, permesso));

    [Theory]
    [InlineData(Permissions.Users.Create)]
    [InlineData(Permissions.Users.Update)]
    [InlineData(Permissions.Users.Delete)]
    [InlineData(Permissions.Users.ResetPassword)]
    [InlineData(Permissions.Users.ResetTfa)]
    [InlineData(Permissions.Settings.Update)]
    public void L_operator_non_puo_scrivere(string permesso)
        => Assert.False(Registro.RuoloHa(Roles.Operator, permesso));

    [Fact]
    public void Il_client_non_ha_permessi_amministrativi()
    {
        // Sul proprio account agisce comunque: quei controlli sono sull'identità,
        // non su un permesso.
        Assert.Empty(Registro.PermessiDi(Roles.Client));
        Assert.All(Permissions.Tutti, p => Assert.False(Registro.RuoloHa(Roles.Client, p)));
    }

    // --- casi limite ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("RuoloInventato")]
    [InlineData("admin")] // case-sensitive: non è Roles.Admin
    public void Un_ruolo_sconosciuto_non_ha_permessi(string? ruolo)
    {
        Assert.Empty(Registro.PermessiDi(ruolo));
        Assert.False(Registro.RuoloHa(ruolo, Permissions.Users.Read));
    }

    [Fact]
    public void Un_permesso_sconosciuto_e_sempre_negato()
        => Assert.False(Registro.RuoloHa(Roles.Admin, "permesso.inventato"));

    [Fact]
    public void Ogni_ruolo_dichiarato_e_presente_nella_mappa()
    {
        // Un ruolo assegnabile ma assente dalla mappa non avrebbe alcun permesso,
        // e il difetto emergerebbe solo in esecuzione.
        foreach (var ruolo in Roles.All)
        {
            Assert.False(Registro.PermessiDi(ruolo) is null);
        }
    }

    // --- coerenza della dichiarazione ---

    [Fact]
    public void Una_mappa_con_permessi_non_dichiarati_non_parte()
    {
        // Un nome scritto male negherebbe l'accesso in silenzio: meglio fallire
        // all'avvio che scoprirlo quando un utente prova a usare la funzione.
        var ex = Assert.Throws<InvalidOperationException>(() => new PermissionRegistry(
            new Dictionary<string, IEnumerable<string>>
            {
                [Roles.Admin] = ["users.raed"], // refuso voluto
            }));

        Assert.Contains("users.raed", ex.Message);
    }

    [Fact]
    public void Una_mappa_personalizzata_sostituisce_quella_predefinita()
    {
        var registro = new PermissionRegistry(new Dictionary<string, IEnumerable<string>>
        {
            [Roles.Client] = [Permissions.Users.Read],
        });

        Assert.True(registro.RuoloHa(Roles.Client, Permissions.Users.Read));
        Assert.False(registro.RuoloHa(Roles.Admin, Permissions.Users.Read));
    }

    [Fact]
    public void Permissions_Tutti_non_ha_duplicati()
        => Assert.Equal(Permissions.Tutti.Count, Permissions.Tutti.Distinct().Count());
}

public class PermissionPrincipalTests
{
    private static readonly IPermissionRegistry Registro = new PermissionRegistry();

    [Fact]
    public void HaPermesso_legge_il_ruolo_dal_principal()
    {
        var admin = TestFactory.Principal(Guid.NewGuid(), Roles.Admin);
        var client = TestFactory.Principal(Guid.NewGuid(), Roles.Client);

        Assert.True(admin.HaPermesso(Registro, Permissions.Users.Delete));
        Assert.False(client.HaPermesso(Registro, Permissions.Users.Delete));
    }

    [Fact]
    public void Un_principal_senza_ruolo_non_ha_permessi()
    {
        var senzaRuolo = TestFactory.Principal(Guid.NewGuid(), role: null);

        // Non solleva: il controllo dei permessi deve negare, non esplodere.
        Assert.False(senzaRuolo.HaPermesso(Registro, Permissions.Users.Read));
    }

    [Fact]
    public void Un_principal_vuoto_non_ha_permessi()
        => Assert.False(new ClaimsPrincipal(new ClaimsIdentity())
            .HaPermesso(Registro, Permissions.Users.Read));

    [Fact]
    public void CurrentUserRoleOrNull_e_CurrentUserIdOrNull_non_sollevano()
    {
        var vuoto = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Null(vuoto.CurrentUserRoleOrNull());
        Assert.Null(vuoto.CurrentUserIdOrNull());
    }
}
