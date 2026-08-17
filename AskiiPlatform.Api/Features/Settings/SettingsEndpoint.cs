using Askii.Authorization;
using Askii.Features.Settings.UpdateSettings;

namespace Askii.Features.Auth;

public static class SettingsEndpoint
{
    public static void MapSettings(this IEndpointRouteBuilder app)
    {
        app.MapPost("/settings/update", UpdateSettingsEndpoint.Impl)
            .RequireAuthorization(JwtAuthorization.PolicyLevel.AdminPolicy)
            .MapToApiVersion(1);
    }
}
