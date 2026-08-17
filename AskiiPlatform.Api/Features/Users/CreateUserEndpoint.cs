using System.Security.Cryptography;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.CreateUser;

public static class CreateUserEndpoint
{
    public static async Task<IResult> Impl(
        CreateUserRequest req,
        AppDbContext db,
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

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Ok(new CreateUserResult(
            user.Email,
            user.FullName,
            user.Role,
            user.IsActive,
            true,
            user.Id
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
    Guid Id
);
