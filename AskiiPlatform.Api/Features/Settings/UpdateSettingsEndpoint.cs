using Askii.Common.Security;
using Askii.Database.Entities;

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
