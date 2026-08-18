using Askii.Common;
using Askii.Common.Extensions;
using Askii.Common.Exceptions;
using Askii.Database.Entities;
using Askii.Features.Auth.Login;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.UpdateUser;
using Askii.Tests.Features.Auth;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests;

/// <summary>
/// Precondizioni degli handler, documentate.
///
/// Gli handler non si difendono da input nulli né da claim mancanti: contano
/// sulla pipeline, cioè sul ValidationFilter per il corpo della richiesta e su
/// OnTokenValidated per l'identità. Questi test fissano quella dipendenza, così
/// se qualcuno stacca un filtro da un endpoint si sa cosa succede all'handler
/// rimasto scoperto: non un 400, ma un 500.
///
/// Nascono come test di caratterizzazione dei difetti noti; quelli corretti sono
/// stati rimossi o riscritti come verifica del comportamento giusto nei rispettivi
/// file.
/// </summary>
public class KnownIssuesTests
{
    // =====================================================================
    // Gli handler assumono il corpo già validato: senza il ValidationFilter
    // davanti, un campo mancante li fa esplodere invece di produrre un 400.
    // =====================================================================

    [Fact]
    public async Task Handler_login_con_email_null_solleva_NullReferenceException()
    {
        using var ctx = new TestDb();

        await Assert.ThrowsAsync<NullReferenceException>(() => LoginEndpoint.Impl(
            new LoginRequest(null!, "Password123!"),
            ctx.Db, TestFactory.TokenService(), CancellationToken.None));
    }

    [Fact]
    public async Task Handler_create_con_email_null_solleva_NullReferenceException()
    {
        using var ctx = new TestDb();

        await Assert.ThrowsAsync<NullReferenceException>(() => CreateUserEndpoint.Impl(
            new CreateUserRequest(null!, "N", "U", Roles.Client, false),
            ctx.Db, new EmailSenderFinto(), CancellationToken.None));
    }

    // =====================================================================
    // Gli handler assumono l'identità già verificata: le extension sui claim
    // usano `!` e Guid.Parse, quindi un principal incompleto solleva
    // un'eccezione. Oggi non può arrivarci, perché OnTokenValidated rifiuta i
    // token senza `sub` e senza impronta.
    // =====================================================================

    [Fact]
    public async Task Handler_token_senza_claim_di_id_manda_l_endpoint_in_eccezione()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        // role presente ma non Admin -> si valuta CurrentUserId(), che è assente.
        await Assert.ThrowsAsync<ArgumentNullException>(() => ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(user.Id, "Nuova456!", "Nuova456!", null),
            ctx.Db,
            TestFactory.Principal(userId: null, role: Roles.Client),
            TestFactory.Permessi(),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handler_token_senza_claim_di_ruolo_manda_l_endpoint_in_eccezione()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() => ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(user.Id, "Nuova456!", "Nuova456!", null),
            ctx.Db,
            TestFactory.Principal(userId: null, role: null),
            TestFactory.Permessi(),
            CancellationToken.None));
    }

    // =====================================================================
    // Non più un difetto: NormalizeEmail usa ToLowerInvariant. Il test resta
    // come guardia, perché tornare a ToLower non darebbe errori evidenti.
    // =====================================================================

    [Fact]
    public void NormalizeEmail_non_dipende_dalla_culture_del_thread()
    {
        var originale = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            var turca = "MARIO@EXAMPLE.COM".NormalizeEmail();

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var invariante = "MARIO@EXAMPLE.COM".NormalizeEmail();

            // In turco `ToLower` trasformerebbe la I in 'ı' (senza punto):
            // ToLowerInvariant no, quindi le due normalizzazioni coincidono.
            Assert.Equal(invariante, turca);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originale;
        }
    }
}
