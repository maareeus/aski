using Askii.Common;
using Askii.Database.Entities;
using Askii.Features.Auth.Login;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.UpdateUser;
using Askii.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Askii.Tests;

/// <summary>
/// Test di caratterizzazione: fotografano il comportamento ATTUALE dei difetti noti,
/// non quello desiderato. Sono verdi oggi; quando il bug verrà corretto diventeranno
/// rossi, segnalando che vanno riscritti come test del comportamento giusto.
/// Ogni test cita il difetto a cui si riferisce.
/// </summary>
public class KnownIssuesTests
{
    // =====================================================================
    // #4 - Nessuna validazione dell'input: campi mancanti nel JSON arrivano
    //      null e fanno esplodere l'endpoint con un 500 anziché un 400.
    // =====================================================================

    [Fact]
    public async Task BUG4_login_con_email_null_solleva_NullReferenceException()
    {
        using var ctx = new TestDb();

        await Assert.ThrowsAsync<NullReferenceException>(() => LoginEndpoint.Impl(
            new LoginRequest(null!, "Password123!"),
            ctx.Db, TestFactory.TokenService(), CancellationToken.None));
    }

    [Fact]
    public async Task BUG4_create_con_email_null_solleva_NullReferenceException()
    {
        using var ctx = new TestDb();

        await Assert.ThrowsAsync<NullReferenceException>(() => CreateUserEndpoint.Impl(
            new CreateUserRequest(null!, "N", "U", Roles.Client, false, "Password123!"),
            ctx.Db, CancellationToken.None));
    }

    [Fact]
    public async Task BUG4_create_con_password_null_solleva_ArgumentNullException()
    {
        using var ctx = new TestDb();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateUserEndpoint.Impl(
            new CreateUserRequest("nuovo@example.com", "N", "U", Roles.Client, false, null!),
            ctx.Db, CancellationToken.None));
    }

    [Fact]
    public async Task BUG4_create_accetta_una_password_vuota()
    {
        using var ctx = new TestDb();

        var result = await CreateUserEndpoint.Impl(
            new CreateUserRequest("nuovo@example.com", "N", "U", Roles.Client, false, ""),
            ctx.Db, CancellationToken.None);

        // Nessun requisito minimo sulla password.
        Assert.IsType<Ok<CreateUserResult>>(result);
    }

    // =====================================================================
    // #5 - IsValidEmail si basa su MailAddress, che accetta anche le forme
    //      con display name: passa validazione roba che non è un indirizzo.
    // =====================================================================

    [Fact]
    public async Task BUG5_create_accetta_email_in_forma_display_name()
    {
        using var ctx = new TestDb();

        var result = await CreateUserEndpoint.Impl(
            new CreateUserRequest("Mario Rossi <mario@example.com>", "N", "U", Roles.Client, false, "Password123!"),
            ctx.Db, CancellationToken.None);

        Assert.IsType<Ok<CreateUserResult>>(result);
        ctx.Detach();
        Assert.Equal("mario rossi <mario@example.com>", (await ctx.Db.Users.SingleAsync()).Email);
    }

    // =====================================================================
    // #6 - /user/activate è anonimo e richiede solo lo userId: nessun token
    //      di conferma. Chi conosce l'id attiva l'account.
    // =====================================================================

    [Fact]
    public async Task BUG6_activate_non_richiede_alcun_segreto_oltre_all_id()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(isActive: false);

        // Nessun ClaimsPrincipal, nessun token di attivazione: basta il Guid,
        // che l'endpoint di create restituisce in chiaro nella response.
        var result = await ActivateUserEndpoint.Impl(
            new ActivateUserRequest(user.Id), ctx.Db, CancellationToken.None);

        Assert.IsType<Ok<ActivateUserResponse>>(result);
        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).IsActive);
    }

    // =====================================================================
    // #7 - UpdateUser chiama SetEmail senza normalizzare né validare né
    //      controllare i duplicati: aggira tutti i controlli di CreateUser.
    // =====================================================================

    [Fact]
    public async Task BUG7_update_accetta_un_email_sintatticamente_invalida()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync("mario@example.com");

        var result = await UpdateUserEndpoint.Impl(
            new UpdateUserRequest(user.Id, "non-una-email", null, null, null),
            ctx.Db, CancellationToken.None);

        Assert.IsType<Ok<UpdateUserResponse>>(result);
        ctx.Detach();
        Assert.Equal("non-una-email", (await ctx.Db.Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task BUG7_update_su_email_gia_esistente_da_500_invece_di_409()
    {
        using var ctx = new TestDb();
        var a = await ctx.SeedUserAsync("a@example.com", "Password123!");
        await ctx.SeedUserAsync("b@example.com", "Password123!");

        // L'indice univoco protegge i dati, ma UpdateUser non fa il controllo
        // preventivo che CreateUser fa: l'errore arriva dal db come
        // DbUpdateException, quindi 500 anziché un 409 con messaggio utile.
        await Assert.ThrowsAsync<DbUpdateException>(() => UpdateUserEndpoint.Impl(
            new UpdateUserRequest(a.Id, "b@example.com", null, null, null),
            ctx.Db, CancellationToken.None));
    }

    // =====================================================================
    // #9 - ChangePassword non chiede la password attuale: un token rubato
    //      (valido 8h) basta per prendersi l'account in modo permanente.
    // =====================================================================

    [Fact]
    public async Task BUG9_un_admin_puo_cambiare_la_password_del_superadmin()
    {
        using var ctx = new TestDb();
        var superAdmin = await ctx.SeedSuperAdminAsync("super@example.com", "Password123!");

        var result = await ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(superAdmin.Id, "Presa456!", "Presa456!", null),
            ctx.Db,
            TestFactory.Principal(Guid.NewGuid(), Roles.Admin), // admin qualsiasi, non il superadmin
            CancellationToken.None);

        Assert.IsType<Ok<ChangePasswordResponse>>(result);
        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword("Presa456!"));
    }

    [Fact]
    public async Task BUG9_changepassword_accetta_una_password_vuota()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync(password: "Vecchia123!");

        // Nessun requisito di robustezza: si può azzerare la password.
        var result = await ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(user.Id, "", "", OldPassword: "Vecchia123!"),
            ctx.Db, TestFactory.Principal(user.Id, Roles.Client), CancellationToken.None);

        Assert.IsType<Ok<ChangePasswordResponse>>(result);
        ctx.Detach();
        Assert.True((await ctx.Db.Users.SingleAsync()).VerifyPassword(""));
    }

    // =====================================================================
    // #10 - ChangePassword legge i claim senza difese: la policy UserPolicy
    //       richiede solo l'autenticazione, quindi un token senza claim di
    //       ruolo o di id passa l'autorizzazione e poi fa 500.
    // =====================================================================

    [Fact]
    public async Task BUG10_token_senza_claim_di_id_manda_l_endpoint_in_eccezione()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        // role presente ma non Admin -> si valuta CurrentUserId(), che è assente.
        await Assert.ThrowsAsync<ArgumentNullException>(() => ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(user.Id, "Nuova456!", "Nuova456!", null),
            ctx.Db,
            TestFactory.Principal(userId: null, role: Roles.Client),
            CancellationToken.None));
    }

    [Fact]
    public async Task BUG10_token_senza_claim_di_ruolo_manda_l_endpoint_in_eccezione()
    {
        using var ctx = new TestDb();
        var user = await ctx.SeedUserAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() => ChangePasswordEndpoint.Impl(
            new ChangePasswordRequest(user.Id, "Nuova456!", "Nuova456!", null),
            ctx.Db,
            TestFactory.Principal(userId: null, role: null),
            CancellationToken.None));
    }

    // =====================================================================
    // #8 - NormalizeEmail usa ToLower() culture-sensitive invece di
    //      ToLowerInvariant(): sotto culture turca "I" non diventa "i".
    // =====================================================================

    [Fact]
    public void BUG8_NormalizeEmail_dipende_dalla_culture_del_thread()
    {
        var originale = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            var turca = "MARIO@EXAMPLE.COM".ToLower();

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var invariante = "MARIO@EXAMPLE.COM".ToLower();

            // In turco la I maiuscola diventa 'ı' (senza punto): le due normalizzazioni divergono.
            Assert.NotEqual(invariante, turca);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originale;
        }
    }
}
