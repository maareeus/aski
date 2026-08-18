using Askii.Authorization;
using Askii.Features.Settings.GetSettings;
using Askii.Features.Settings.UpdateSettings;

namespace Askii.Features.Settings;

public static class SettingsEndpoint
{
    public static void MapSettings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/settings", GetSettingsEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);

        app.MapPost("/settings/update", UpdateSettingsEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);
    }
}
