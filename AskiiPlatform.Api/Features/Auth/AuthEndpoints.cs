using Askii.Common.Validation;
using Askii.Features.Auth.Login;
using Askii.Features.Auth.Tfa;

namespace Askii.Features.Auth;

public static class AuthEndpoint
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", LoginEndpoint.Impl)
            .Validating<RouteHandlerBuilder, LoginRequest>()
            .AllowAnonymous()
            .MapToApiVersion(1);

        // Secondo passaggio del login: l'autorizzazione la porta il token di
        // sfida nel corpo, l'utente non ha ancora un token d'accesso.
        app.MapPost("/auth/tfa/send-otp", TfaSendOtpEndpoint.Impl)
            .Validating<RouteHandlerBuilder, TfaSendOtpRequest>()
            .AllowAnonymous()
            .MapToApiVersion(1);

        app.MapPost("/auth/tfa/verify", TfaVerifyEndpoint.Impl)
            .Validating<RouteHandlerBuilder, TfaVerifyRequest>()
            .AllowAnonymous()
            .MapToApiVersion(1);
    }
}

public enum AuthStatus
{
    /// <summary>
    /// Autorizzazione non concessa
    /// </summary>
    UNAUTHORIZED,
    /// <summary>
    /// Login effettuato
    /// </summary>
    OK,
    /// <summary>
    /// Autenticazione a 2 fattori richiesta
    /// </summary>
    TFA_REQUIRED
}
