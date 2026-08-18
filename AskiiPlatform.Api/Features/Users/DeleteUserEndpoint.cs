using System.Security.Claims;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Microsoft.EntityFrameworkCore;
using Askii.Common.Validation;
using FluentValidation;

namespace Askii.Features.Users.DeleteUser;

public static class DeleteUserEndpoint
{
    public static async Task<IResult> Impl(
        DeleteUserRequest req,
        AppDbContext db,
        CancellationToken ct,
        ClaimsPrincipal loggedUser
    )
    {
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Id == req.userId, ct);

        DeleteUserResponse result;
        if(user is null || user.Id == loggedUser.CurrentUserId()) result = DeleteUserResponse.Ko();
        else if (user.IsSuperAdmin) result = DeleteUserResponse.CannotDeleteSuperAdmin();
        else
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
            result = DeleteUserResponse.Ok();
        }

        if(result.result)
        {
            return Results.Ok(result);
        } else
        {
            return ResultsHelper.BadRequest(result.msg);
        }
    }
}

public record DeleteUserRequest(Guid userId);

public record DeleteUserResponse(bool result, string msg)
{
    public static DeleteUserResponse Ok() => new DeleteUserResponse(true, "Utente eliminato");
    public static DeleteUserResponse Ko() => new DeleteUserResponse(false, "Errore durante l'eliminazione dell'utente");
    public static DeleteUserResponse CannotDeleteSuperAdmin() => new DeleteUserResponse(false, "L'utente super amministratore non può essere eliminato");
}

// --- validazione ---

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
        => RuleFor(x => x.userId).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
}
