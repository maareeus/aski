using System.Security.Cryptography;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Askii.Database.Entities;
using Askii.ExternalServices;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.CreateUser;

public static class CreateUserEndpoint
{
    public static async Task<IResult> Impl(
        CreateUserRequest req,
        AppDbContext db,
        IEmailSender emailSender,
        CancellationToken ct
    )
    {
        var normalizedEmail = req.Email.NormalizeEmail();
        if(!normalizedEmail.IsValidEmail())
        {
            return ResultsHelper.BadRequest($"La mail {req.Email} non è valida");
        }

        var user = await db.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if(user is not null)
        {
            return ResultsHelper.Conflict($"Un utente con email {req.Email} è gia presente");
        }

        user = User.Create(
            email: normalizedEmail,
            password: SecretGenerator.TemporaryPassword(),
            name: req.Name,
            lastName: req.LastName,
            role: req.Role
        );

        user.IsActive = req.IsActive;

        // La password generata non è nota a nessuno: serve solo a impedire
        // l'accesso prima dell'attivazione, durante la quale sarà l'utente a
        // scegliere la propria. Un utente creato già attivo non ha bisogno del
        // codice, e la password gliela imposta l'admin dal dettaglio.
        string? codiceAttivazione = null;
        if (!user.IsActive)
        {
            codiceAttivazione = user.IssueActivationCode();
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var emailInviata = false;
        if (codiceAttivazione is not null)
        {
            var esito = await emailSender.InviaAsync(
                user.Email,
                "Attiva il tuo account Askii Platform",
                $"""
                 Per attivare l'account e scegliere la tua password usa questo codice:

                 {codiceAttivazione}

                 È valido 7 giorni e può essere usato una sola volta.
                 """,
                ct);

            emailInviata = esito.Inviata;
        }

        return Results.Ok(new CreateUserResult(
            user.Email,
            user.FullName,
            user.Role,
            user.IsActive,
            true,
            user.Id,
            codiceAttivazione,
            emailInviata
        ));
    }
}

public record CreateUserRequest(
    string Email,
    string? Name,
    string? LastName,
    string Role,
    bool IsActive
);

public record CreateUserResult(
    string? Email,
    string? FullName,
    string? Role,
    bool IsActive,
    bool Result,
    Guid Id,
    /// <summary>
    /// Codice di attivazione in chiaro, presente solo alla creazione di un
    /// utente non attivo. L'endpoint è riservato agli Admin, che possono già
    /// reimpostare la password di chiunque: restituirlo permette di completare
    /// il flusso anche senza SMTP configurato.
    /// </summary>
    string? ActivationCode,
    bool ActivationEmailSent
);
