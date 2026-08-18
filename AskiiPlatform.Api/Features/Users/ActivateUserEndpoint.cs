using Askii.Common.Helpers;
using Askii.Database;
using Askii.ExternalServices;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.ActivateUser;

/// <summary>
/// Attivazione tramite codice monouso. Sostituisce la versione che accettava il
/// solo identificativo: quel valore non è un segreto — lo restituisce la
/// creazione e compare nel claim `sub` di ogni token — quindi chiunque lo
/// conoscesse poteva attivare un account.
/// </summary>
public static class ActivateUserEndpoint
{
    public static async Task<IResult> Impl(
            ActivateUserRequest req,
            AppDbContext db,
            CancellationToken ct
        )
    {
        if (req.Password != req.RePassword)
        {
            return ResultsHelper.BadRequest(ActivateUserResponse.PasswordDiverse().msg);
        }

        // Il codice si cerca fra gli utenti con attivazione pendente: sono pochi,
        // e confrontare l'hash uno per uno evita di dover indicizzare un segreto.
        var candidati = await db.Users
            .Where(u => u.ActivationCodeHash != null)
            .ToListAsync(ct);

        var user = candidati.FirstOrDefault(u => u.TryActivate(req.Code, req.Password));

        if (user is null)
        {
            // Messaggio unico per codice inesistente, scaduto o già usato: dire
            // quale dei tre permetterebbe di sondare i codici validi.
            return ResultsHelper.BadRequest(ActivateUserResponse.CodiceNonValido().msg);
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(ActivateUserResponse.Ok(user.Email));
    }
}

/// <summary>
/// Rigenera il codice e lo rimanda. Endpoint di amministrazione: il codice viene
/// restituito anche nella risposta, così il flusso funziona pure senza SMTP
/// configurato. Non è un'esposizione aggiuntiva, dato che un Admin può già
/// reimpostare direttamente la password di chiunque.
/// </summary>
public static class ResendActivationEndpoint
{
    public static async Task<IResult> Impl(
        ResendActivationRequest req,
        AppDbContext db,
        IEmailSender emailSender,
        CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null) return ResultsHelper.NotFound($"Nessun utente con identificativo {req.UserId}");

        if (user.IsActive)
        {
            return ResultsHelper.Conflict("L'utente è già attivo: non serve un codice di attivazione.");
        }

        var codice = user.IssueActivationCode();
        await db.SaveChangesAsync(ct);

        var esito = await emailSender.InviaAsync(
            user.Email,
            "Attiva il tuo account Askii Platform",
            $"""
             Per attivare l'account e scegliere la tua password usa questo codice:

             {codice}

             È valido 7 giorni e può essere usato una sola volta.
             """,
            ct);

        return Results.Ok(new ResendActivationResponse(
            result: true,
            msg: esito.Inviata
                ? $"Codice inviato a {user.Email}"
                : $"Codice generato, ma l'invio non è riuscito: {esito.Errore} Comunicalo manualmente.",
            code: codice,
            emailSent: esito.Inviata));
    }
}

public record ActivateUserRequest(string Code, string Password, string RePassword);

public record ActivateUserResponse(bool result, string msg)
{
    public static ActivateUserResponse Ok(string email) =>
        new(true, $"Account {email} attivato. Ora puoi accedere con la password scelta.");

    public static ActivateUserResponse CodiceNonValido() =>
        new(false, "Codice di attivazione non valido, scaduto o già utilizzato");

    public static ActivateUserResponse PasswordDiverse() =>
        new(false, "Le due password non corrispondono");
}

public record ResendActivationRequest(Guid UserId);

/// <summary>`code` è presente perché l'endpoint è riservato agli Admin.</summary>
public record ResendActivationResponse(bool result, string msg, string code, bool emailSent);
