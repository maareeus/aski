using Askii.Authorization;
using Askii.Common.Validation;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.DeleteUser;
using Askii.Features.Users.GetUser;
using Askii.Features.Users.ListUsers;
using Askii.Features.Users.TfaSettings;
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

        app.MapGet("/user/admin/{id:guid}", GetUserEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/admin/create", CreateUserEndpoint.Impl)
            .Validating<RouteHandlerBuilder, CreateUserRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/activate", ActivateUserEndpoint.Impl)
            .Validating<RouteHandlerBuilder, ActivateUserRequest>()
            .AllowAnonymous()
            .MapToApiVersion(1);

        app.MapPost("/user/admin/activation/resend", ResendActivationEndpoint.Impl)
            .Validating<RouteHandlerBuilder, ResendActivationRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/admin/delete", DeleteUserEndpoint.Impl)
            .Validating<RouteHandlerBuilder, DeleteUserRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/changepassword", ChangePasswordEndpoint.Impl)
            .Validating<RouteHandlerBuilder, ChangePasswordRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/admin/update", UpdateUserEndpoint.AdminImpl)
            .Validating<RouteHandlerBuilder, UpdateUserRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/update", UpdateUserEndpoint.UserImpl)
            .Validating<RouteHandlerBuilder, UpdateUserRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        // --- configurazione della 2FA sul proprio account ---

        app.MapGet("/user/tfa", TfaSettingsEndpoints.Stato)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/tfa/authenticator/start", TfaSettingsEndpoints.AvviaAuthenticator)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/tfa/authenticator/confirm", TfaSettingsEndpoints.ConfermaAuthenticator)
            .Validating<RouteHandlerBuilder, TfaCodeRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/tfa/authenticator/disable", TfaSettingsEndpoints.DisattivaAuthenticator)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/tfa/email/enable", TfaSettingsEndpoints.AttivaEmail)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        app.MapPost("/user/tfa/email/disable", TfaSettingsEndpoints.DisattivaEmail)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.UserPolicy)
            .MapToApiVersion(1);

        // Recupero: azzera la 2FA di un utente che ha perso il secondo fattore.
        app.MapPost("/user/admin/tfa/reset", TfaSettingsEndpoints.ResetAdmin)
            .Validating<RouteHandlerBuilder, TfaResetRequest>()
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);
    }
}
