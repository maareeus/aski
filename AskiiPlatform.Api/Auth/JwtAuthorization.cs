using System.Security.Claims;
using System.Text;
using Askii.Common;
using Askii.Common.Authorization;
using Askii.Common.Extensions;
using Askii.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Askii.Authorization;

public static class JwtAuthorization
{
    public static void Init(
        this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
             o.TokenValidationParameters = new TokenValidationParameters
             {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"], 
                ValidAudience = config["Jwt:Audience"], 
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(config["Jwt:Key"]!))  
             };

             // La firma valida dice solo che il token è nostro e non scaduto, non
             // che è ancora buono: qui si controlla che l'utente esista, sia
             // ancora attivo e non abbia cambiato password o ruolo dopo
             // l'emissione. Costa una query per richiesta autenticata, prezzo
             // accettabile per avere una revoca immediata su un token da 8 ore.
             o.Events = new JwtBearerEvents
             {
                OnTokenValidated = async contesto =>
                {
                    var principal = contesto.Principal;
                    if (principal is null) { contesto.Fail("Token senza identità"); return; }

                    var stamp = principal.FindFirst(AskiiClaims.Stamp)?.Value;
                    if (string.IsNullOrEmpty(stamp)) { contesto.Fail("Token privo di impronta"); return; }

                    Guid userId;
                    try { userId = principal.CurrentUserId(); }
                    catch (Exception) { contesto.Fail("Token senza identificativo utente"); return; }

                    var db = contesto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                    var stato = await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.SecurityStamp, u.IsActive })
                        .SingleOrDefaultAsync();

                    if (stato is null) { contesto.Fail("Utente non più esistente"); return; }
                    if (!stato.IsActive) { contesto.Fail("Utente disattivato"); return; }
                    if (stato.SecurityStamp != stamp) { contesto.Fail("Credenziali cambiate dopo l'emissione del token"); return; }
                }
             };
        });
        builder.Services.AddAuthorization();

        // L'autorizzazione è per permesso, non per ruolo: gli endpoint dichiarano
        // l'azione che consentono e il registro decide se il ruolo la concede.
        builder.Services.AddSingleton<IPermissionRegistry>(_ => new PermissionRegistry());
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
    }
}
