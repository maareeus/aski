using Askii.Database.Entities;

namespace Askii.Features.Settings.UpdateSettings;

public static class UpdateSettingsEndpoint
{
    public static async Task<IResult> Impl(
        UpdateSettingsRequest req,
        CancellationToken ct,
        Options options
    )
    {
        foreach(var (k,v) in req.Options)
        {
            await options.UpdateOption(k,v);
        }
        
        return Results.Ok();
    }
}

public record UpdateSettingsRequest(Dictionary<string, string> Options);
