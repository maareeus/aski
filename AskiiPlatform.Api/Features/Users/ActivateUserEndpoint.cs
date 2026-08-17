using Askii.Common.Helpers;
using Askii.Database;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.ActivateUser;

public static class ActivateUserEndpoint
{
    public static async Task<IResult> Impl(
            ActivateUserRequest req,
            AppDbContext db,
            CancellationToken ct
        )
    {
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Id == req.userId, ct);

        ActivateUserResponse result;
        if(user is null) result = ActivateUserResponse.Ko();
        else if(user.IsActive) result = ActivateUserResponse.AlreadyActivated();
        else
        {
            user.IsActive = true;
            await db.SaveChangesAsync(ct);
            result = ActivateUserResponse.Ok();
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

public record ActivateUserRequest(Guid userId);

public record ActivateUserResponse(bool result, string msg)
{
    public static ActivateUserResponse Ok() => new ActivateUserResponse(true, "Utente attivato");
    public static ActivateUserResponse Ko() => new ActivateUserResponse(false, "Errore durante l'attivazione dell'utente");
    public static ActivateUserResponse AlreadyActivated() => new ActivateUserResponse(true, "L'utente era gia stato attivato");
}
