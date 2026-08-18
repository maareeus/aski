using Askii.Common.Extensions;
using Askii.Features.Auth.Login;
using Askii.Features.Auth.Tfa;
using Askii.Features.Settings.UpdateSettings;
using Askii.Features.Users.ActivateUser;
using Askii.Features.Users.ChangePassword;
using Askii.Features.Users.CreateUser;
using Askii.Features.Users.DeleteUser;
using Askii.Features.Users.TfaSettings;
using Askii.Features.Users.UpdateUser;
using FluentValidation;

namespace Askii.Common.Validation;

/// <summary>
/// Requisiti minimi sulle password, applicati in ogni punto in cui una password
/// viene scelta.
///
/// La regola è sulla lunghezza e non sulla composizione: NIST SP 800-63B
/// raccomanda esplicitamente di non imporre classi di caratteri, che spingono a
/// password prevedibili tipo "Password1!", e di puntare invece sulla lunghezza.
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

// --- autenticazione ---

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Sul login non si applicano i requisiti di lunghezza: una password
        // vecchia potrebbe non rispettarli, e rifiutarla qui impedirebbe di
        // accedere per poi cambiarla.
        RuleFor(x => x.Email).NotEmpty().WithMessage("L'email è obbligatoria.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La password è obbligatoria.");
    }
}

public class TfaSendOtpRequestValidator : AbstractValidator<TfaSendOtpRequest>
{
    public TfaSendOtpRequestValidator()
        => RuleFor(x => x.ChallengeToken).NotEmpty().WithMessage("Sessione di verifica assente.");
}

public class TfaVerifyRequestValidator : AbstractValidator<TfaVerifyRequest>
{
    public TfaVerifyRequestValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty().WithMessage("Sessione di verifica assente.");
        RuleFor(x => x.Code).CodiceSeiCifre();
        RuleFor(x => x.Method).IsInEnum().WithMessage("Metodo di verifica non riconosciuto.");
    }
}

// --- utenti ---

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Role).Ruolo();
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");

        // I campi opzionali si validano solo se valorizzati: null significa
        // "non modificare".
        RuleFor(x => x.Email!).Email().When(x => x.Email is not null);
        RuleFor(x => x.Role!).Ruolo().When(x => x.Role is not null);
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
        => RuleFor(x => x.userId).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
}

public class ActivateUserRequestValidator : AbstractValidator<ActivateUserRequest>
{
    public ActivateUserRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Il codice di attivazione è obbligatorio.");
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.RePassword)
            .Equal(x => x.Password).WithMessage("Le due password non corrispondono.");
    }
}

public class ResendActivationRequestValidator : AbstractValidator<ResendActivationRequest>
{
    public ResendActivationRequestValidator()
        => RuleFor(x => x.UserId).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.RePassword)
            .Equal(x => x.Password).WithMessage("Le due password non corrispondono.");
        // OldPassword resta opzionale: gli Admin ne sono esenti, e a decidere
        // se serve è l'endpoint che conosce il ruolo del chiamante.
    }
}

// --- 2FA ---

public class TfaCodeRequestValidator : AbstractValidator<TfaCodeRequest>
{
    public TfaCodeRequestValidator() => RuleFor(x => x.Code).CodiceSeiCifre();
}

public class TfaResetRequestValidator : AbstractValidator<TfaResetRequest>
{
    public TfaResetRequestValidator()
        => RuleFor(x => x.UserId).NotEmpty().WithMessage("L'identificativo dell'utente è obbligatorio.");
}

// --- impostazioni ---

public class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.Options)
            .NotNull().WithMessage("Nessuna opzione da salvare.")
            .Must(o => o.Count > 0).WithMessage("Nessuna opzione da salvare.");

        RuleForEach(x => x.Options)
            .Must(kv => !string.IsNullOrWhiteSpace(kv.Key))
                .WithMessage("Il nome dell'opzione non può essere vuoto.")
            .Must(kv => kv.Value.Length <= 100)
                .WithMessage("Il valore dell'opzione non può superare 100 caratteri.");
    }
}
