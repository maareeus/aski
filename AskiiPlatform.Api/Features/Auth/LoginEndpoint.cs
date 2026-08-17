using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Auth.Login;

public static class LoginEndpoint
{
    public static async Task<IResult> Impl(
            LoginRequest req,
            AppDbContext db,
            TokenService tokenService,
            CancellationToken ct
        )
    {
        var normalizedEmail = req.Email.NormalizeEmail();
        var user = await db.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if(user is null || !user.VerifyPassword(req.Password) || !user.IsActive)
        {
            return ResultsHelper.Unauthorized("Errore di autenticazione");
        }

        user.RecordLogin();
        await db.SaveChangesAsync(ct);

        var token = tokenService.GenerateToken(user);

        return Results.Ok(new LoginResult(
            token,
            user.Id,
            user.Email,
            user.FullName,
            user.Role
        ));
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResult(
    string Token,
    Guid UserId,
    string Email,
    string FullName,
    string Role
);
