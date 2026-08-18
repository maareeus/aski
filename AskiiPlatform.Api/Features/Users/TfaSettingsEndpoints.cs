using Askii.Common;
using System.Security.Claims;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Common.Security;
using Askii.Database;
using Askii.Database.Entities;
using Askii.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Askii.Common.Validation;
using FluentValidation;

namespace Askii.Features.Users.TfaSettings;

/// <summary>
/// Configurazione della 2FA da parte dell'utente sul proprio account.
///
/// Sono endpoint dedicati e non campi di /user/update perché attivare
/// l'app di authenticator richiede di dimostrare che l'app è configurata: senza
/// quella conferma si otterrebbe un account inaccessibile.
/// </summary>
public static class TfaSettingsEndpoints
{
    private const string Emittente = "Askii Platform";

    private static async Task<User?> UtenteCorrente(AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
        => await db.Users.SingleOrDefaultAsync(u => u.Id == loggedUser.CurrentUserId(), ct);

    /// <summary>Stato corrente, per popolare la schermata del profilo.</summary>
    public static async Task<IResult> Stato(
        AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var user = await UtenteCorrente(db, loggedUser, ct);
        if (user is null) return ResultsHelper.NotFound("Utente non trovato");

        return Results.Ok(new TfaStatusResponse(
            Enabled: user.TfaEnabled,
            Methods: user.TFA_Availables,
            AuthenticatorPending: user.HasPendingTotp));
    }

    /// <summary>
    /// Avvia l'associazione dell'app: genera il segreto e restituisce l'URI
    /// otpauth da cui il client disegna il QR. Il metodo non è ancora attivo.
    /// </summary>
    public static async Task<IResult> AvviaAuthenticator(
        AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var user = await UtenteCorrente(db, loggedUser, ct);
        if (user is null) return ResultsHelper.NotFound("Utente non trovato");

        if (user.TFA_Availables.Contains(TFA_Available.AUTHENTICATOR_APP))
        {
            return ResultsHelper.Conflict(
                "L'app di authenticator è già attiva. Disattivala prima di associarne una nuova.");
        }

        var segreto = user.StartTotpEnrollment();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new AuthenticatorSetupResponse(
            Secret: segreto,
            OtpauthUri: Totp.UriOtpauth(segreto, Emittente, user.Email),
            Digits: Totp.CifrePredefinite,
            PeriodSeconds: Totp.PeriodoSecondi));
    }

    /// <summary>Conferma l'associazione verificando un codice prodotto dall'app.</summary>
    public static async Task<IResult> ConfermaAuthenticator(
        TfaCodeRequest req, AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var user = await UtenteCorrente(db, loggedUser, ct);
        if (user is null) return ResultsHelper.NotFound("Utente non trovato");

        if (user.TotpSecret is null)
        {
            return ResultsHelper.BadRequest("Nessuna associazione in corso: avviala prima di confermare.");
        }

        if (!user.ConfirmTotp(req.Code))
        {
            return ResultsHelper.BadRequest("Codice non valido. Controlla l'orario del dispositivo e riprova.");
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new TfaOperationResponse(true, "App di authenticator attivata"));
    }

    public static async Task<IResult> DisattivaAuthenticator(
        AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var user = await UtenteCorrente(db, loggedUser, ct);
        if (user is null) return ResultsHelper.NotFound("Utente non trovato");

        user.DisableTotp();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new TfaOperationResponse(true, "App di authenticator disattivata"));
    }

    public static async Task<IResult> AttivaEmail(
        AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var user = await UtenteCorrente(db, loggedUser, ct);
        if (user is null) return ResultsHelper.NotFound("Utente non trovato");

        user.EnableEmailOtp();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new TfaOperationResponse(true, "Codice via email attivato"));
    }

    public static async Task<IResult> DisattivaEmail(
        AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var user = await UtenteCorrente(db, loggedUser, ct);
        if (user is null) return ResultsHelper.NotFound("Utente non trovato");

        user.DisableEmailOtp();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new TfaOperationResponse(true, "Codice via email disattivato"));
    }

    /// <summary>
    /// Percorso di recupero: un Admin azzera la 2FA di un utente che ha perso
    /// l'accesso al secondo fattore. Non richiede codici, quindi è un potere
    /// forte, coerente con il fatto che l'Admin può già reimpostare la password.
    /// </summary>
    public static async Task<IResult> ResetAdmin(
        TfaResetRequest req, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null) return ResultsHelper.NotFound($"Nessun utente con identificativo {req.UserId}");

        user.DisableAllTfa();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new TfaOperationResponse(true, "Autenticazione a due fattori azzerata"));
    }
}

public record TfaStatusResponse(bool Enabled, List<TFA_Available> Methods, bool AuthenticatorPending);

public record AuthenticatorSetupResponse(string Secret, string OtpauthUri, int Digits, int PeriodSeconds);

public record TfaCodeRequest(string Code);

public record TfaResetRequest(Guid UserId);

public record TfaOperationResponse(bool result, string msg);

// --- validazione ---

public class TfaCodeRequestValidator : AbstractValidator<TfaCodeRequest>
{
    public TfaCodeRequestValidator() => RuleFor(x => x.Code).CodiceSeiCifre();
}

public class TfaResetRequestValidator : AbstractValidator<TfaResetRequest>
{
    public TfaResetRequestValidator()
        => RuleFor(x => x.UserId).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
}
