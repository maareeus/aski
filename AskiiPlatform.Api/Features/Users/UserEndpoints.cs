using Askii.Authorization;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.DeleteUser;
using Askii.Features.Users.UpdateUser;

namespace Askii.Features.Users;

public static class UserEndpoint
{
    public static void MapUsers(this IEndpointRouteBuilder app)
    {
        app.MapPost("/user/create", CreateUserEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/activate", ActivateUserEndpoint.Impl)
            .AllowAnonymous()
            .MapToApiVersion(1);

        app.MapPost("/user/delete", DeleteUserEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/changepassword", ChangePasswordEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/update", UpdateUserEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);
    }
}
