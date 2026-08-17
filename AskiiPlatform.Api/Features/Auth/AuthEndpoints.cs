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
