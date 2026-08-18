using System.Security.Claims;
using Askii.Common.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Askii.Common.Authorization;

public sealed class PermissionRequirement(string permesso) : IAuthorizationRequirement
{
    public string Permesso { get; } = permesso;
}

public sealed class PermissionHandler(IPermissionRegistry registry)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (registry.RuoloHa(context.User.CurrentUserRoleOrNull(), requirement.Permesso))
        {
            context.Succeed(requirement);
        }

        // Non si chiama Fail(): lasciando il requisito non soddisfatto, un altro
        // handler potrebbe concederlo. Fail() invece è definitivo.
        return Task.CompletedTask;
    }
}

public static class PermissionEndpointExtensions
{
    /// <summary>
    /// Richiede autenticazione più un permesso specifico. Le policy si
    /// costruiscono inline invece di registrarne una per nome: i permessi sono
    /// stringhe, e mantenere in parallelo un elenco di policy sarebbe un secondo
    /// posto da tenere allineato.
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permesso)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permesso)));

    /// <summary>
    /// Solo autenticazione: per gli endpoint che agiscono sul proprio account,
    /// dove il controllo è sull'identità e non su un permesso.
    /// </summary>
    public static TBuilder RequireAuthenticated<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(policy => policy.RequireAuthenticatedUser());
}

public static class PermissionPrincipalExtensions
{
    /// <summary>
    /// Verifica un permesso dentro un handler, per i casi in cui l'esito non è
    /// binario: cambiare la password è consentito sul proprio account senza
    /// alcun permesso, e su quello di altri solo con users.password.reset.
    /// </summary>
    public static bool HaPermesso(
        this ClaimsPrincipal utente, IPermissionRegistry registry, string permesso)
        => registry.RuoloHa(utente.CurrentUserRoleOrNull(), permesso);
}
