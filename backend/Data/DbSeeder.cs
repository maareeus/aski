using Aski.Tickets.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace Aski.Tickets.Api.Data;

/// <summary>Crea i ruoli e l'utente Admin iniziale se mancanti.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration config)
    {
        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        var userMgr = sp.GetRequiredService<UserManager<AppUser>>();
        var email = config["Seed:AdminEmail"] ?? "admin@aski.local";
        var password = config["Seed:AdminPassword"] ?? "ChangeMe123!";

        if (await userMgr.FindByEmailAsync(email) is null)
        {
            var admin = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Administrator",
                IsActive = true
            };
            var result = await userMgr.CreateAsync(admin, password);
            if (result.Succeeded)
                await userMgr.AddToRoleAsync(admin, Roles.Admin);
            else
                throw new InvalidOperationException(
                    "Seed admin fallito: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
