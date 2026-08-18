using Askii.Common.Extensions;
using FluentValidation;

namespace Askii.Common.Validation;

/// <summary>
/// Regole riusabili sui tipi primitivi, condivise dai validatori delle feature.
///
/// Qui stanno solo le regole: i validatori dei singoli DTO vivono accanto ai
/// rispettivi endpoint, perché tenerli in Common obbligava questo livello a
/// conoscere ogni feature, invertendo la direzione delle dipendenze.
/// </summary>
public static class RegolePassword
{
    public const int LunghezzaMinima = 12;
    public const int LunghezzaMassima = 128;

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> regola)
        => regola
            .NotEmpty().WithMessage("La password è obbligatoria.")
            .MinimumLength(LunghezzaMinima)
                .WithMessage($"La password deve essere di almeno {LunghezzaMinima} caratteri.")
            .MaximumLength(LunghezzaMassima)
                .WithMessage($"La password non può superare {LunghezzaMassima} caratteri.");

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> regola)
        => regola
            .NotEmpty().WithMessage("L'email è obbligatoria.")
            .MaximumLength(320).WithMessage("L'email è troppo lunga.")
            // La guardia su null serve perché FluentValidation esegue tutte le
            // regole della catena anche dopo il fallimento di NotEmpty.
            .Must(e => e is not null && e.NormalizeEmail().IsValidEmail())
                .WithMessage("L'email non è in un formato valido.");

    public static IRuleBuilderOptions<T, string> Ruolo<T>(this IRuleBuilder<T, string> regola)
        => regola
            .NotEmpty().WithMessage("Il ruolo è obbligatorio.")
            .Must(r => Roles.All.Contains(r))
                .WithMessage($"Il ruolo deve essere uno fra: {string.Join(", ", Roles.All)}.");

    public static IRuleBuilderOptions<T, string> CodiceSeiCifre<T>(this IRuleBuilder<T, string> regola)
        => regola
            .NotEmpty().WithMessage("Il codice è obbligatorio.")
            .Matches(@"^\d{6}$").WithMessage("Il codice è composto da 6 cifre.");
}
