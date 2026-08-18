using System.Security.Claims;
using Askii.Common;
using Askii.Common.Authorization;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Microsoft.EntityFrameworkCore;
using Askii.Common.Validation;
using FluentValidation;

namespace Askii.Features.Users.ChangePassword;


public static class ChangePasswordEndpoint
{
    public static async Task<IResult> Impl(
        ChangePasswordRequest req,
        AppDbContext db,
        ClaimsPrincipal loggedUser,
        IPermissionRegistry permessi,
        CancellationToken ct
    )
    {
        // Sul proprio account non serve alcun permesso: il controllo è
        // sull'identità. Su quello di altri serve users.password.reset.
        var puoResettareAltrui = loggedUser.HaPermesso(permessi, Permissions.Users.ResetPassword);

        if(!puoResettareAltrui && loggedUser.CurrentUserId() != req.Id)
        {
            // L'utente non è un admin e sta cercando di modificare un utente che è diverso da se stesso
            // Non può farlo
            return ResultsHelper.Unauthorized(ChangePasswordResponse.Unauthorized().msg);
        }

        // Se siamo qui allora l'utente può modificare la risorsa, bisogna distinguere due cose però:
        // Admin => Può modificare la password di altri
        // Utente => Può solo modificare la sua password
        if(req.Password != req.RePassword)
        {
            return ResultsHelper.BadRequest(ChangePasswordResponse.CheckFailed().msg);
        }

        // Posso modificare la password
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Id == req.Id, ct);
        
        if(user is null)
        {
            return ResultsHelper.NotFound(ChangePasswordResponse.UserNotFound().msg);
        }

        // Il super amministratore è protetto da declassamento e cancellazione: se
        // un Admin qualsiasi potesse cambiargli la password, quelle protezioni
        // sarebbero aggirabili prendendone il controllo. Solo lui può cambiare
        // la propria, dimostrando di conoscere quella attuale.
        if(user.IsSuperAdmin && loggedUser.CurrentUserId() != user.Id)
        {
            return ResultsHelper.Unauthorized(ChangePasswordResponse.SuperAdminProtetto().msg);
        }

        // Anche il superadmin su se stesso deve fornire la password attuale,
        // nonostante sia Admin: altrimenti un token rubato basterebbe.
        // Chi ha il permesso di reset ne è esente, tranne sul super
        // amministratore: lì la password attuale è sempre richiesta, altrimenti
        // un token rubato basterebbe a prenderne il controllo.
        var serveVecchia = !puoResettareAltrui || user.IsSuperAdmin;

        // Se non sono admin, verifico che la password attuale sia conosciuta
        if(serveVecchia && (!user.VerifyPassword(req.OldPassword ?? string.Empty)))
        {
            return ResultsHelper.Unauthorized(ChangePasswordResponse.CheckAuthFailed().msg);
        }

        user.SetPassword(req.Password);
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);

        return Results.Ok(
            ChangePasswordResponse.Ok()
        );
    }
}

public record ChangePasswordRequest(
    Guid Id,
    string Password,
    string RePassword,
    string? OldPassword
);

public record ChangePasswordResponse(bool result, string msg)
{
    public static ChangePasswordResponse Ok() => new ChangePasswordResponse(true, "Password modificata con successo");
    public static ChangePasswordResponse CheckFailed() => new ChangePasswordResponse(false, "Le password non corrispondono");
    public static ChangePasswordResponse CheckAuthFailed() => new ChangePasswordResponse(false, "La password non è corretta");
    public static ChangePasswordResponse Unauthorized() => new ChangePasswordResponse(false, "Non hai i permessi per modificare la risorsa");
    public static ChangePasswordResponse UserNotFound() => new ChangePasswordResponse(false, "Utente non trovato");
    public static ChangePasswordResponse SuperAdminProtetto() => new ChangePasswordResponse(false, "La password del super amministratore può essere cambiata solo da lui stesso");
}

// --- validazione ---

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.RePassword)
            .Equal(x => x.Password).WithMessage("Le due password non corrispondono.");
        // OldPassword resta opzionale: gli Admin ne sono esenti, e a decidere
        // se serve è l'endpoint che conosce il ruolo del chiamante.
    }
}
