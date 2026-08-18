using Askii.Common.Security;
using Askii.Database.Entities;
using Askii.Common.Validation;
using FluentValidation;

namespace Askii.Features.Settings.UpdateSettings;

public static class UpdateSettingsEndpoint
{
    public static async Task<IResult> Impl(
        UpdateSettingsRequest req,
        CancellationToken ct,
        Options options,
        ISecretProtector protector
    )
    {
        foreach(var (k,v) in req.Options)
        {
            // I segreti vanno a database cifrati: un dump della tabella non
            // consegna la password SMTP in chiaro.
            var valore = OpzioniSegrete.Contiene(k) ? protector.Protect(v) : v;
            await options.UpdateOption(k, valore);
        }

        return Results.Ok();
    }
}

public record UpdateSettingsRequest(Dictionary<string, string> Options);

// --- validazione ---

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
