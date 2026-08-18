using Askii.Authorization;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.DeleteUser;
using Askii.Features.Users.ListUsers;
using Askii.Features.Users.UpdateUser;

namespace Askii.Features.Users;

public static class UserEndpoint
{
    public static void MapUsers(this IEndpointRouteBuilder app)
    {
        // In GET, non POST: l'elenco è cacheabile e lo stato dei filtri
        // diventa una URL condivisibile e ricaricabile.
        app.MapGet("/user/admin/list", ListUsersEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/admin/create", CreateUserEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/activate", ActivateUserEndpoint.Impl)
            .AllowAnonymous()
            .MapToApiVersion(1);

        app.MapPost("/user/admin/delete", DeleteUserEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/changepassword", ChangePasswordEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/admin/update", UpdateUserEndpoint.AdminImpl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

            app.MapPost("/user/update", UpdateUserEndpoint.UserImpl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);
    }
}
