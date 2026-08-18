using Askii.Features.Auth.Login;

namespace Askii.Features.Auth;

public static class AuthEndpoint
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", LoginEndpoint.Impl)
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
/// <summary>
/// Metodi di 2FA disponibili per l'utente corrente
/// </summary>
public enum TFA_Available
{
    /// <summary>
    /// Invio di codice OTP via mail valido 5min
    /// </summary>
    EMAIL_OTP,
    /// <summary>
    /// Uso di una app di authenticator registrata
    /// </summary>
    AUTHENTICATOR_APP
}
