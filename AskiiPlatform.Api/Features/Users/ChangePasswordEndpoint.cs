using System.Security.Claims;
using Askii.Common;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.ChangePassword;


public static class ChangePasswordEndpoint
{
    public static async Task<IResult> Impl(
        ChangePasswordRequest req,
        AppDbContext db,
        ClaimsPrincipal loggedUser,
        CancellationToken ct
    )
    {
        if(loggedUser.CurrentUserRole() != Roles.Admin && loggedUser.CurrentUserId() != req.Id)
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
            return ResultsHelper.BadRequest(ChangePasswordResponse.UserNotFound().msg);
        }

        // Se non sono admin, verifico che la password attuale sia conosciuta
        if(loggedUser.CurrentUserRole() != Roles.Admin && (!user.VerifyPassword(req.OldPassword ?? string.Empty)))
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
    public static ChangePasswordResponse Ko() => new ChangePasswordResponse(false, "Errore durante la modifica della password");
    public static ChangePasswordResponse CheckFailed() => new ChangePasswordResponse(false, "Le password non corrispondono");
    public static ChangePasswordResponse CheckAuthFailed() => new ChangePasswordResponse(false, "La password non è corretta");
    public static ChangePasswordResponse Unauthorized() => new ChangePasswordResponse(false, "Non hai i permessi per modificare la risorsa");
    public static ChangePasswordResponse UserNotFound() => new ChangePasswordResponse(false, "Utente non trovato");
}
