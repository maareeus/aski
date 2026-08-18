using Askii.Common;
using Askii.Common.Helpers;
using Askii.Database;
using Askii.ExternalServices;
using Askii.Features.Auth.Login;
using Microsoft.EntityFrameworkCore;
using Askii.Common.Validation;
using FluentValidation;

namespace Askii.Features.Auth.Tfa;

/// <summary>
/// Secondo passaggio del login. Sono endpoint anonimi: l'autorizzazione la porta
/// il token di sfida nel corpo, non un bearer, perché a questo punto l'utente non
/// ha ancora un token d'accesso.
/// </summary>
public static class TfaSendOtpEndpoint
{
    public static async Task<IResult> Impl(
        TfaSendOtpRequest req,
        AppDbContext db,
        TokenService tokenService,
        IEmailSender emailSender,
        CancellationToken ct)
    {
        var userId = tokenService.ReadTfaChallenge(req.ChallengeToken);
        if (userId is null) return ResultsHelper.Unauthorized("Sessione di verifica non valida o scaduta");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !user.IsActive) return ResultsHelper.Unauthorized("Errore di autenticazione");

        if (!user.TFA_Availables.Contains(TFA_Available.EMAIL_OTP))
        {
            return ResultsHelper.BadRequest("Il codice via email non è fra i metodi attivi per questo utente");
        }

        var codice = user.IssueEmailOtp();
        await db.SaveChangesAsync(ct);

        var esito = await emailSender.InviaAsync(
            user.Email,
            "Codice di verifica Askii Platform",
            $"""
             Il codice per completare l'accesso è: {codice}

             È valido 5 minuti e può essere usato una sola volta.
             Se non hai richiesto tu questo accesso, cambia la password.
             """,
            ct);

        if (!esito.Inviata)
        {
            // Il codice resta valido: se l'invio fallisce per configurazione, in
            // sviluppo lo si legge dal log e il flusso è comunque provabile.
            return ResultsHelper.BadRequest(esito.Errore ?? "Invio del codice non riuscito");
        }

        return Results.Ok(new TfaSendOtpResponse(true, $"Codice inviato a {Mascherata(user.Email)}"));
    }

    /// <summary>
    /// Nasconde parte dell'indirizzo: la schermata di verifica è raggiungibile
    /// con la sola sfida, quindi mostrare l'email intera darebbe un'informazione
    /// in più a chi ha rubato la password.
    /// </summary>
    private static string Mascherata(string email)
    {
        var chiocciola = email.IndexOf('@');
        if (chiocciola <= 1) return new string('*', Math.Max(email.Length, 3));

        var locale = email[..chiocciola];
        var visibili = Math.Min(2, locale.Length);
        return locale[..visibili] + new string('*', Math.Max(locale.Length - visibili, 1)) + email[chiocciola..];
    }
}

public static class TfaVerifyEndpoint
{
    public static async Task<IResult> Impl(
        TfaVerifyRequest req,
        AppDbContext db,
        TokenService tokenService,
        CancellationToken ct)
    {
        var userId = tokenService.ReadTfaChallenge(req.ChallengeToken);
        if (userId is null) return ResultsHelper.Unauthorized("Sessione di verifica non valida o scaduta");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !user.IsActive) return ResultsHelper.Unauthorized("Errore di autenticazione");

        var valido = req.Method switch
        {
            TFA_Available.AUTHENTICATOR_APP => user.VerifyTotp(req.Code),
            TFA_Available.EMAIL_OTP => user.VerifyEmailOtp(req.Code),
            _ => false,
        };

        // I tentativi sull'OTP vanno persistiti anche quando la verifica
        // fallisce, altrimenti il limite non conterebbe nulla.
        await db.SaveChangesAsync(ct);

        if (!valido) return ResultsHelper.Unauthorized("Codice non valido o scaduto");

        user.RecordLogin();
        await db.SaveChangesAsync(ct);

        return Results.Ok(LoginResult.Completato(tokenService.GenerateToken(user), user));
    }
}

public record TfaSendOtpRequest(string ChallengeToken);

public record TfaSendOtpResponse(bool result, string msg);

public record TfaVerifyRequest(string ChallengeToken, TFA_Available Method, string Code);

// --- validazione ---

public class TfaSendOtpRequestValidator : AbstractValidator<TfaSendOtpRequest>
{
    public TfaSendOtpRequestValidator()
        => RuleFor(x => x.ChallengeToken).NotEmpty().WithMessage("Sessione di verifica assente.");
}

public class TfaVerifyRequestValidator : AbstractValidator<TfaVerifyRequest>
{
    public TfaVerifyRequestValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty().WithMessage("Sessione di verifica assente.");
        RuleFor(x => x.Code).CodiceSeiCifre();
        RuleFor(x => x.Method).IsInEnum().WithMessage("Metodo di verifica non riconosciuto.");
    }
}
