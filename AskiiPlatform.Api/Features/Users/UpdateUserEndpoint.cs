using System.Security.Claims;
using Askii.Common;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.UpdateUser;


public static class UpdateUserEndpoint
{
    public static async Task<IResult> Impl(
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

        if(req.Email is not null) user.SetEmail(req.Email);
        if(req.Name is not null) user.Name = req.Name;
        if(req.LastName is not null) user.LastName = req.LastName;
        if(req.Role is not null) user.UpdateRole(req.Role);

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
    string? Role
);

public record UpdateUserResponse(bool result, string msg)
{
    public static UpdateUserResponse Ok() => new UpdateUserResponse(true, "Utente modificato");
    public static UpdateUserResponse Ko() => new UpdateUserResponse(false, "Errore durante la modifica dell'utente");
    public static UpdateUserResponse Unauthorized() => new UpdateUserResponse(false, "Non hai i permessi per modificare la risorsa");
    public static UpdateUserResponse UserNotFound() => new UpdateUserResponse(false, "Utente non trovato");
}
