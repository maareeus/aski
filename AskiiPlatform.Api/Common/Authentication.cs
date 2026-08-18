namespace Askii.Common;

/// <summary>
/// Metodi di secondo fattore che un utente può avere attivi.
///
/// Sta in Common e non fra le feature perché è un valore persistito
/// sull'entità User: se vivesse in Features/Auth, il livello del dominio
/// dipenderebbe da quello degli endpoint.
/// </summary>
public enum TFA_Available
{
    /// <summary>Invio di codice OTP via mail valido 5min</summary>
    EMAIL_OTP,

    /// <summary>Uso di una app di authenticator registrata</summary>
    AUTHENTICATOR_APP
}

/// <summary>
/// Nomi dei claim che l'applicazione mette nei propri token, oltre a quelli
/// standard. Qui e non nel TokenService perché li leggono anche i livelli
/// inferiori, come il controllo di revoca nella pipeline di autenticazione.
/// </summary>
public static class AskiiClaims
{
    /// <summary>
    /// Impronta dello stato di autorizzazione al momento dell'emissione: se non
    /// coincide con quella dell'utente, il token è stato revocato.
    /// </summary>
    public const string Stamp = "stamp";
}
