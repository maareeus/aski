using System.Security.Claims;
using Askii.Common;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Askii.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.UpdateUser;


public static class UpdateUserEndpoint
{
    public static async Task<IResult> AdminImpl(
        UpdateUserRequest req,
        AppDbContext db,
        CancellationToken ct
    )
    {
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Id == req.Id, ct);
        
        if(user is null)
        {
            return ResultsHelper.BadRequest(UpdateUserResponse.UserNotFound().msg);
        }

        if(req.Email is not null)
        {
            user.SetEmail(req.Email);
        }
        if(req.Name is not null) user.Name = req.Name;
        if(req.LastName is not null) user.LastName = req.LastName;
        if(req.Role is not null) user.UpdateRole(req.Role);

        // Un admin può azzerare la 2FA (lista vuota) come percorso di recupero,
        // ma non attivare metodi al posto dell'utente: l'app di authenticator
        // richiede un segreto che solo l'utente può confermare.
        if(req.TFA_Availables is not null)
        {
            if(req.TFA_Availables.Count > 0)
            {
                return ResultsHelper.BadRequest(
                    "I metodi 2FA non si attivano da qui. Per azzerarli usa /user/admin/tfa/reset.");
            }
            user.DisableAllTfa();
        }

        db.Users.Update(user);
        await db.SaveChangesAsync(ct);

        return Results.Ok(UpdateUserResponse.Ok());
    }

    public static async Task<IResult> UserImpl(
        UpdateUserRequest req,
        AppDbContext db,
        CancellationToken ct,
        ClaimsPrincipal loggedUser
    )
    {
        if(loggedUser.CurrentUserId() != req.Id)
        {
            return ResultsHelper.Unauthorized(UpdateUserResponse.Unauthorized().msg);
        }
        
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Id == req.Id, ct);
        
        if(user is null)
        {
            return ResultsHelper.BadRequest(UpdateUserResponse.UserNotFound().msg);
        }

        // I metodi 2FA NON si impostano da qui: usare /user/tfa/*. Assegnare la
        // lista direttamente permetterebbe di attivare l'app di authenticator
        // senza un segreto confermato, rendendo l'account inaccessibile.
        if(req.TFA_Availables is not null)
        {
            return ResultsHelper.BadRequest(
                "I metodi di autenticazione a due fattori si configurano dagli endpoint /user/tfa.");
        }

        db.Users.Update(user);
        await db.SaveChangesAsync(ct);

        return Results.Ok(UpdateUserResponse.Ok());
    }
}

public record UpdateUserRequest(
    Guid Id,
    string? Email,
    string? Name,
    string? LastName,
    string? Role,
    List<TFA_Available>? TFA_Availables
);

public record UpdateUserResponse(bool result, string msg)
{
    public static UpdateUserResponse Ok() => new UpdateUserResponse(true, "Utente modificato");
    public static UpdateUserResponse Ko() => new UpdateUserResponse(false, "Errore durante la modifica dell'utente");
    public static UpdateUserResponse Unauthorized() => new UpdateUserResponse(false, "Non hai i permessi per modificare la risorsa");
    public static UpdateUserResponse UserNotFound() => new UpdateUserResponse(false, "Utente non trovato");
    public static UpdateUserResponse InvalidEmail() => new UpdateUserResponse(false, "La mail inserita non è valida");
}
