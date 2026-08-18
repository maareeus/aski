using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Askii.Database.Entities;
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

        // Password corretta ma 2FA attiva: non si emette il token d'accesso, solo
        // una sfida a breve scadenza che autorizza il secondo passaggio.
        if(user.TfaEnabled)
        {
            return Results.Ok(LoginResult.TfaRichiesta(
                tokenService.GenerateTfaChallenge(user),
                user.TFA_Availables));
        }

        user.RecordLogin();
        await db.SaveChangesAsync(ct);

        return Results.Ok(LoginResult.Completato(tokenService.GenerateToken(user), user));
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResult(
    AuthStatus Status,
    string? Token,
    Guid? UserId,
    string? Email,
    string? FullName,
    string? Role,
    /// <summary>Valorizzato solo con Status = TFA_REQUIRED.</summary>
    string? ChallengeToken,
    List<TFA_Available>? TfaMethods)
{
    public static LoginResult Completato(string token, User user) => new(
        AuthStatus.OK, token, user.Id, user.Email, user.FullName, user.Role, null, null);

    public static LoginResult TfaRichiesta(string challengeToken, List<TFA_Available> metodi) => new(
        AuthStatus.TFA_REQUIRED, null, null, null, null, null, challengeToken, metodi);
}
